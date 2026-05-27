using System;
using System.Reflection;

namespace KomplexHeat
{
    internal static class ReflectionHelper
    {
        /// <summary>
        ///     Searches for a field with the specified name in the given type or its base types (excluding <see cref="object" />),
        ///     considering only non-public instance fields defined at each level of inheritance.
        /// </summary>
        /// <param name="type">The type to start the search from.</param>
        /// <param name="name">The name of the field to locate.</param>
        /// <returns>A <see cref="FieldInfo" /> object representing the field if found; otherwise, null.</returns>
        public static FieldInfo FindField(Type type, string name)
        {
            while (type != null && type != typeof(object))
            {
                var field = type.GetField(name,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null) return field;
                type = type.BaseType;
            }

            return null;
        }
    }
}