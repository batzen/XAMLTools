namespace XAMLTools.ResourceDump;

using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

public class ResourceDumper
{
    public void DumpResources(string assemblyFile, string outputPath)
    {
        assemblyFile = Path.GetFullPath(assemblyFile);
        var assembly = Assembly.LoadFile(assemblyFile);

        var resourceNames = assembly.GetManifestResourceNames();

        {
            var resourceNamesFile = Path.Combine(outputPath, "ResourceNames");
            File.WriteAllLines(resourceNamesFile, resourceNames, Encoding.UTF8);
        }

        var xamlResourceName = resourceNames.FirstOrDefault(x => x.EndsWith(".g.resources"));

        if (string.IsNullOrEmpty(xamlResourceName) == false)
        {
            using var xamlResourceStream = assembly.GetManifestResourceStream(xamlResourceName)!;
            using var reader = new System.Resources.ResourceReader(xamlResourceStream);
            var xamlResourceNames = reader.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToArray();

            {
                var xamlResourceNamesFile = Path.Combine(outputPath, "XAMLResourceNames");
                File.WriteAllLines(xamlResourceNamesFile, xamlResourceNames, Encoding.UTF8);
            }
        }
    }
}