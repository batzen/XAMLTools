namespace XAMLTools.XAMLColorSchemeGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class ColorSchemeGenerator
    {
        private const int BufferSize = 32768; // 32 Kilobytes

        public IEnumerable<string> GenerateColorSchemeFiles(string generatorParametersFile, string templateFile, string? outputPath = null)
        {
            var parameters = GetParametersFromFile(generatorParametersFile);

            outputPath ??= Path.GetDirectoryName(Path.GetFullPath(templateFile));

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new Exception("OutputPath could not be determined.");
            }

            Directory.CreateDirectory(outputPath);

            var templateContent = File.ReadAllText(templateFile, Encoding.UTF8);

            var colorSchemesWithoutVariantName = parameters.ColorSchemes
                .Where(x => string.IsNullOrEmpty(x.ForColorSchemeVariant) || x.ForColorSchemeVariant == "None")
                .ToList();
            var colorSchemesWithVariantName = parameters.ColorSchemes
                .Where(x => string.IsNullOrEmpty(x.ForColorSchemeVariant) == false && x.ForColorSchemeVariant != "None")
                .ToList();

            foreach (var baseColorScheme in parameters.BaseColorSchemes)
            {
                if (colorSchemesWithoutVariantName.Count == 0
                    && colorSchemesWithVariantName.Count == 0)
                {
                    var themeName = baseColorScheme.Name;
                    var colorSchemeName = string.Empty;
                    var alternativeColorSchemeName = string.Empty;
                    var themeDisplayName = baseColorScheme.Name;

                    yield return this.GenerateColorSchemeFile(outputPath, templateContent, themeName, themeDisplayName, baseColorScheme.Name, colorSchemeName, alternativeColorSchemeName, false, baseColorScheme.Values, parameters.DefaultValues);
                }

                foreach (var colorScheme in colorSchemesWithoutVariantName)
                {
                    if (string.IsNullOrEmpty(colorScheme.ForBaseColor) == false
                        && colorScheme.ForBaseColor != baseColorScheme.Name)
                    {
                        continue;
                    }

                    var themeName = $"{baseColorScheme.Name}.{colorScheme.Name}";
                    var colorSchemeName = colorScheme.Name;
                    var alternativeColorSchemeName = colorScheme.Name;
                    var themeDisplayName = $"{colorSchemeName} ({baseColorScheme.Name})";

                    yield return this.GenerateColorSchemeFile(outputPath, templateContent, themeName, themeDisplayName, baseColorScheme.Name, colorSchemeName, alternativeColorSchemeName, colorScheme.IsHighContrast, colorScheme.Values, baseColorScheme.Values, parameters.DefaultValues);
                }

                foreach (var colorSchemeVariant in parameters.AdditionalColorSchemeVariants)
                {
                    foreach (var colorScheme in parameters.ColorSchemes)
                    {
                        if (string.IsNullOrEmpty(colorScheme.ForBaseColor) == false
                            && colorScheme.ForBaseColor != baseColorScheme.Name)
                        {
                            continue;
                        }

                        if (colorScheme.ForColorSchemeVariant == "None"
                            // if there is a color scheme specific for the current variant skip the unspecific one
                            || parameters.ColorSchemes.Any(x => x != colorScheme && colorScheme.Name == x.Name && colorScheme.IsHighContrast == x.IsHighContrast && x.ForColorSchemeVariant == colorSchemeVariant.Name))
                        {
                            continue;
                        }

                        var themeName = $"{baseColorScheme.Name}.{colorScheme.Name}.{colorSchemeVariant.Name}";
                        var colorSchemeName = $"{colorScheme.Name}.{colorSchemeVariant.Name}";
                        var alternativeColorSchemeName = colorScheme.Name;
                        var themeDisplayName = $"{colorSchemeName} ({baseColorScheme.Name})";

                        yield return this.GenerateColorSchemeFile(outputPath, templateContent, themeName, themeDisplayName, baseColorScheme.Name, colorSchemeName, alternativeColorSchemeName, colorScheme.IsHighContrast, colorScheme.Values, colorSchemeVariant.Values, baseColorScheme.Values, parameters.DefaultValues);
                    }
                }
            }
        }

        public static ThemeGenerator.ThemeGeneratorParameters GetParametersFromFile(string inputFile)
        {
            return ThemeGenerator.Current.GetParametersFromString(ReadAllTextShared(inputFile));
        }

        private static string ReadAllTextShared(string file)
        {
            Stream? stream = null;
            try
            {
                stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);

                using (var textReader = new StreamReader(stream, Encoding.UTF8))
                {
                    stream = null;
                    return textReader.ReadToEnd();
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }

        public string GenerateColorSchemeFile(string outputPath, string templateContent, string themeName, string themeDisplayName, string baseColorScheme, string colorScheme, string alternativeColorScheme, bool isHighContrast, params Dictionary<string, string>[] valueSources)
        {
            if (isHighContrast)
            {
                themeDisplayName += " HighContrast";
            }

            var themeTempFileContent = ThemeGenerator.Current.GenerateColorSchemeFileContent(templateContent, themeName, themeDisplayName, baseColorScheme, colorScheme, alternativeColorScheme, isHighContrast, valueSources);

            var themeFilename = $"{themeName}";

            if (isHighContrast)
            {
                themeFilename += ".HighContrast";
            }

            //Debugger.Launch();
            themeFilename = themeFilename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var themeFile = Path.Combine(outputPath, $"{themeFilename}.xaml");
            var isNewFile = File.Exists(themeFile) == false;

            Trace.WriteLine($"Checking \"{themeFile}\"...");

            var fileHasToBeWritten = isNewFile
                                     || ReadAllTextShared(themeFile) != themeTempFileContent;

            if (fileHasToBeWritten)
            {
                using (var sw = new StreamWriter(themeFile, false, Encoding.UTF8, BufferSize))
                {
                    sw.Write(themeTempFileContent);
                }

                Trace.WriteLine($"Resource Dictionary saved to \"{themeFile}\".");
            }
            else
            {
                Trace.WriteLine("New Resource Dictionary did not differ from existing file. No new file written.");
            }

            return themeFile;
        }
    }
}