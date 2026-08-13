using System.Reflection;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using UnityEngine;

namespace KomplexHeat
{
    public class ElectricMotorHeatSource : HeatSourceBase
    {
        private const string MotorTorqueFieldName = "_motorTorqueAbs";
        private const string BrakeTorqueFieldName = "_brakeTorque";
        private FieldInfo _motorTorqueField;
        private FieldInfo _brakeTorqueField;

        private ElectricMotorScript _electricMotorScript;
        private float _scale;

        /// <summary>
        /// Fraction of the motor's dry mass attributed to its thermally active stator and rotor.
        /// </summary>
        /// <remarks>
        /// According to
        /// <see href="https://www.researchgate.net/figure/Distribution-of-weight-of-an-electric-motor-2_fig1_343919871">
        /// this source</see>, an electric motor's stator and rotor together account for about 60%
        /// of its thermal mass.
        /// </remarks>
        private const float CoreMassRatio = 0.6f;

        /// <summary>
        /// Minimum thermal core mass, in kilograms, used to prevent a zero or negative thermal
        /// mass.
        /// </summary>
        private const float MinThermalMass = 0.01f;

        /// <summary>
        /// Conversion factor from the motor's unitless scale to its body length in meters.
        /// </summary>
        /// <remarks>
        /// Electric motors in Juno have a length in meters 0.6x the unit-less scale parameter in
        /// meters. This excludes the protruding end shaft and the tapering portion of the
        /// "Base Color" end bell.
        /// </remarks>
        private const float ScaleToLengthScale = 0.6f;

        /// <summary>
        /// Conversion factor from the motor's unitless scale to its body diameter in meters.
        /// </summary>
        /// <remarks>
        /// Electric motors in Juno have a diameter in meters 0.4x the unit-less scale parameter in
        /// meters. This excludes the radiator fins and only measures the default "Detail Color"
        /// portion.
        /// </remarks>
        private const float ScaleToDiameterScale = 0.4f;

        /// <summary>
        /// Specific heat of the motor mass, in J/(kg K).
        /// </summary>
        /// <remarks>
        /// <para>
        /// A motor's stator and rotor are made from copper and steel. Copper has a specific heat of
        /// 385 J/(kg K), and steel has a specific heat of 466 J/(kg K). This value is a compromise
        /// between the two.
        /// </para>
        /// <para>
        /// See <see href="https://en.wikipedia.org/wiki/Table_of_specific_heat_capacities">Wikipedia</see>
        /// for more information.
        /// </para>
        /// </remarks>
        protected override float HeatCoreSpecificHeat => 426f; // Between copper and steel.

        /// <summary>
        /// The area of the interface between the motor's core and its casing in m^2, taken as the
        /// lateral surface of a cylinder with the motor's body length and diameter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The stator's outer diameter is in contact with the casing, and that portion of the
        /// casing is where the radiator fins attach, so the end bells are excluded as a
        /// comparatively weak conduction path.
        /// </para>
        /// <para>
        /// <paramref name="partMassDry" /> is unused because the motor's dimensions are known
        /// directly, unlike the hollow-shell approximation the base class falls back on.
        /// </para>
        /// </remarks>
        /// <param name="partMassDry">The part's dry mass in kg. Unused.</param>
        /// <returns>The conduction interface area in m^2</returns>
        protected override float GetSurfaceArea(float partMassDry)
        {
            var length = ScaleToLengthScale * _scale;
            var diameter = ScaleToDiameterScale * _scale;
            // The stator's outer diameter is in contact with the casing, and that portion of the
            // casing is where the radiator fins attach to.
            return length * Mathf.PI * diameter;
        }

        /// <summary>
        /// Conductance between the motor's core and its casing in W/(m^2 K), chosen so the
        /// resulting thermal time constant falls within the published range for every scale and
        /// torque setting. Because a motor's core mass scales with the cube of its size while its
        /// heat transfer area scales with the square, τ is proportional to scale, and a single h
        /// should satisfy every frame size.
        /// </summary>
        /// <remarks>
        /// <para>
        /// An electric motor's "overheating time" is correlated with its size. According to
        /// <see href="https://industrialmonitordirect.com/blogs/knowledgebase/motor-overload-duration-thermal-time-constant-calculation">
        /// Industrial Monitor Direct</see>, the heat transfer coefficient can be estimated
        /// based on the motor's diameter.
        /// </para>
        /// <para>
        /// Specifically, first map the motor diameter to the IEC frame size, according to
        /// <see href="https://www.baldor.com/~/media/files/brands/baldor-reliance/resources%20and%20support/backcover.ashx">
        /// ABB</see>.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Motor diameter (mm)</term>
        ///         <description>IEC frame</description>
        ///     </listheader>
        ///     <item>
        ///         <term>115</term>
        ///         <description>63</description>
        ///     </item>
        ///     <item>
        ///         <term>265</term>
        ///         <description>132</description>
        ///     </item>
        ///     <item>
        ///         <term>500</term>
        ///         <description>280</description>
        ///     </item>
        ///     <item>
        ///         <term>740</term>
        ///         <description>355</description>
        ///     </item>
        /// </list>
        /// </para>
        /// <para>
        /// Then, map IEC frame size to the thermal time constant.
        /// </para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>IEC frame</term>
        ///         <description>Thermal time constant (s)</description>
        ///     </listheader>
        ///     <item>
        ///         <term>63</term>
        ///         <description>50</description>
        ///     </item>
        ///     <item>
        ///         <term>90</term>
        ///         <description>200</description>
        ///     </item>
        ///     <item>
        ///         <term>100</term>
        ///         <description>200</description>
        ///     </item>
        ///     <item>
        ///         <term>200</term>
        ///         <description>600</description>
        ///     </item>
        ///     <item>
        ///         <term>225</term>
        ///         <description>600-1500</description>
        ///     </item>
        /// </list>
        /// <para>
        /// Using <see href="https://en.wikipedia.org/wiki/Newton%27s_law_of_cooling#First-order_transient_response_of_lumped-capacitance_objects">
        /// Newton's Law of Cooling</see> formula τ = mc / (hA), where τ is the time constant,
        /// m is the mass, c is the <see cref="HeatCoreSpecificHeat"/>,
        /// h is heat transfer coefficient, and A is the heat transfer surface area, it can be
        /// derived that h = mc / (A * τ).
        /// </para>
        /// <para>
        /// Substituting m = partMassDry * <see cref="CoreMassRatio"/>,
        /// c = <see cref="HeatCoreSpecificHeat"/>, A = <see cref="GetSurfaceArea"/> into
        /// h = mc / (A * τ) yields a permissible range of h for each motor configuration. The
        /// six cases below span the corners of the full 0.25–4.0 scale and 0–3000 torque design
        /// space. Part masses are measured in game; mc is in J/K, A is in m^2, and τ is in s.
        /// </para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Scale, torque</term>
        ///         <description>mass, mc, A, mc/A, τ band, permissible h</description>
        ///     </listheader>
        ///     <item>
        ///         <term>0.25, 0</term>
        ///         <description>1.56 kg, 399, 0.0471, 8461, 50–200, h = 42–169</description>
        ///     </item>
        ///     <item>
        ///         <term>0.25, 3000</term>
        ///         <description>2.03 kg, 519, 0.0471, 11011, 50–200, h = 55–220</description>
        ///     </item>
        ///     <item>
        ///         <term>1.0, 0</term>
        ///         <description>101 kg, 25816, 0.754, 34239, 200–600, h = 57–171</description>
        ///     </item>
        ///     <item>
        ///         <term>1.0, 3000</term>
        ///         <description>131 kg, 33484, 0.754, 44410, 200–600, h = 74–222</description>
        ///     </item>
        ///     <item>
        ///         <term>4.0, 0</term>
        ///         <description>7420 kg, 1896552, 12.064, 157211, 600–1500, h = 105–262</description>
        ///     </item>
        ///     <item>
        ///         <term>4.0, 3000</term>
        ///         <description>9340 kg, 2387304, 12.064, 197891, 600–1500, h = 132–330</description>
        ///     </item>
        /// </list>
        /// <para>
        /// The intersection of all six permissible ranges is h = 132–169, bounded below by the
        /// largest, highest-torque motor and above by the smallest, lowest-torque one. The midpoint
        /// 150 is chosen for maximum margin at both extremes, giving a τ of 56–73 s at scale 0.25,
        /// 228–296 s at scale 1.0, and 1048–1319 s at scale 4.0, all within their respective bands.
        /// </para>
        /// </remarks>
        protected override float HeatTransferCoefficient => 150f;

        protected override float GetHeatCoreThermalMass(float partMassDry) =>
            Mathf.Max(partMassDry * CoreMassRatio, MinThermalMass);

        protected override bool OnFlightStart(PartScript partScript)
        {
            _electricMotorScript = partScript.GetModifier<ElectricMotorScript>();
            if (_electricMotorScript == null) return false;

            _scale = _electricMotorScript.Data.Scale;
            _motorTorqueField = FindFloatField(MotorTorqueFieldName);
            _brakeTorqueField = FindFloatField(BrakeTorqueFieldName);

            return _motorTorqueField is not null && _brakeTorqueField is not null;
        }

        /// <summary>
        /// Calculates and applies the heat produced by motor losses and braking.
        /// </summary>
        protected override void ApplyHeat()
        {
            var electricalPower = _electricMotorScript.PowerConsumption;
            var angularVelocity = Mathf.Abs(_electricMotorScript.CurrentRpm) * Mathf.PI / 30f;

            var motorTorque = Mathf.Abs((float)_motorTorqueField.GetValue(_electricMotorScript));
            var motorPower = motorTorque * angularVelocity;
            var brakeTorque = Mathf.Abs((float)_brakeTorqueField.GetValue(_electricMotorScript));
            var brakePower = brakeTorque * angularVelocity;

            var motorLoss = Mathf.Max(electricalPower - motorPower, 0f);

            GenerateHeat(motorLoss + brakePower);
        }

        /// <summary>
        /// Finds and validates a private floating-point field on the electric motor script.
        /// </summary>
        /// <param name="fieldName">The name of the field to locate.</param>
        /// <returns>
        /// The matching <see cref="FieldInfo"/> if the field exists and has type
        /// <see cref="float"/>; otherwise, <see langword="null"/>.
        /// </returns>
        private FieldInfo FindFloatField(string fieldName)
        {
            var type = _electricMotorScript.GetType();
            var field = ReflectionHelper.FindField(type, fieldName);

            if (field is null)
            {
                Mod.Instance.LogError(
                    $"Could not find {fieldName} field in {type.FullName} on part {PartDescription}; " +
                    "motor heat disabled");
                return null;
            }

            if (field.FieldType == typeof(float)) return field;

            Mod.Instance.LogError(
                $"Expected {fieldName} to be a float but found {field.FieldType.FullName} in {type.FullName} " +
                $"on part {PartDescription}; motor heat disabled");
            return null;
        }
    }
}