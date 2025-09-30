# KomplexHeat

KomplexHeat is a mod for Jundroo's [Juno: New Origins](https://simplerockets.com) (JNO) that expands the game's heat system.

## Features

- Add heat generation to
    - Rocket engines
    - Jet engines
    - Electric motors
    - Servos
    - Parts running Vizzy
- Add Heat dissipation to
    - (New) radiators
- Reuse existing Juno systems for
    - Heat conduction
    - Heat radiation

## License

This project’s source code is licensed under the [MIT License](LICENSE).

> [!important]
> 
> This software is provided "as is", without warranty of any kind.
>
> This license applies only to the original source code written for KomplexHeat. Unity and Juno: New Origins remain proprietary software, and their engine libraries, assemblies, and any automatically generated skeleton code are not covered by this license. Any third-party plugins and libraries included and referenced are distributed under their original licenses, which are provided in their respective source and documentation.
>
> To build or use this mod, you must have a licensed copy of Unity, Juno: New Origins, and any applicable plugins.

## Installation

#TODO

## Usage

#TODO See wiki

## Development

1. Follow the "Downloading Unity" section of [JNO modding guide](https://www.simplerockets.com/Forums/View/31506/).
    1. Install Unity 2022.3.20f1 with macOS and Windows build components for mono.
    2. Install the C# IDE of your choice. (Unity defaults to Visual Studio; I use Rider.)
2. [Clone](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository) the KomplexHeat repository.
3. Open the repository as a Unity Project.
4. Follow the JNO modding guide to import the JNO mod tools.
    1. In Unity, Assets -> Import Package -> Custom Package, and import the mod tools at `JNO installation directory/ModTools/SimpleRockets2_ModTools.unitypackage`. The default location for this on Windows is `C:\Program Files (x86)\Steam\steamapps\common\SimpleRockets2\ModTools\SimpleRockets2_ModTools.unitypackage`.
    2. Initialize the mod in Unity via SimpleRockets 2 -> Mod Builder Window -> Start Creating Mod.
5. Install the Harmony Unity plugin.
    1. Download the newest "Fat" version of [Harmony plugin](https://github.com/pardeike/Harmony/releases). 
    2. Copy the file `net472/0Harmony.dll` into `Assets/Plugins/Harmony`.
    3. Optionally, you may also include `net472/0Harmony.xml` in `Assets/Plugins/Harmony`.
    4. Focus on Unity. Unity should start importing the plugin automatically.