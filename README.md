# XAMLTools

[![Build status](https://img.shields.io/appveyor/ci/batzen/XAMLTools.svg?style=flat-square)](https://ci.appveyor.com/project/batzen/XAMLTools)
[![Release](https://img.shields.io/github/release/batzen/XAMLTools.svg?style=flat-square)](https://github.com/batzen/XAMLTools/releases/latest)
[![Issues](https://img.shields.io/github/issues/batzen/XAMLTools.svg?style=flat-square)](https://github.com/batzen/XAMLTools/issues)
[![Downloads](https://img.shields.io/nuget/dt/XAMLTools.svg?style=flat-square)](http://www.nuget.org/packages/XAMLTools/)
[![Nuget](https://img.shields.io/nuget/vpre/XAMLTools.svg?style=flat-square)](http://nuget.org/packages/XAMLTools)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://github.com/batzen/XAMLTools/blob/master/License.txt)

Generates color scheme xaml files while replacing certain parts of a template file.

For an example on how this tool works see the [generator input](src/GeneratorParameters.json) and [template](src/Theme.Template.xaml) files.

## Using the tool

### Usage with commandline parameters

`XAMLTools` accepts the following commandline parameters:

- `-g "Path_To_Your_GeneratorParameters.json"`
- `-t "Path_To_Your_Theme.Template.xaml"`
- `-o "Path_To_Your_Output_Folder"`
- `-v` = enables verbose console output

### Usage without commandline parameters

Just set the working directory to a directory containing `GeneratorParameters.json` and `Theme.Template.xaml` and call `XAMLTools.exe`.
The tool then also uses the current working dir as the output folder.

### Usage during build

```xml
    <ItemGroup>
      <PackageReference Include="XAMLTools" version="4-*" privateAssets="All" includeAssets="build" />
    </ItemGroup>

    <Target Name="GenerateXamlFiles" BeforeTargets="DispatchToInnerBuilds">
      <!-- Generate theme files -->
      <Message Text="$(XAMLToolsExecutable)" />
      <Exec Command="&quot;$(XAMLToolsExecutable)&quot;" WorkingDirectory="$(MSBuildProjectDirectory)/Themes/Themes" />
    </Target>
```
