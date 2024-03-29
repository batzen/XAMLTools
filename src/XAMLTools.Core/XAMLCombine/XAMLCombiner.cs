namespace XAMLTools.XAMLCombine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using XAMLTools.Helpers;

    public class XAMLCombiner : IXamlCombinerOptions
    {
        private const int BufferSize = 32768; // 32 Kilobytes

        private static readonly Regex resourceUsageRegex = new(@"({(DynamicResource|StaticResource)\s(?<ResourceKey>.*?)})", RegexOptions.IgnorePatternWhitespace);

        private const string MergedDictionariesString = "ResourceDictionary.MergedDictionaries";

        // WinUI / UWP
        private const string ThemeDictionariesString = "ResourceDictionary.ThemeDictionaries";

        private const string ResourceDictionaryString = "ResourceDictionary";

        private const string WinfxXAMLNamespaceUri = "http://schemas.microsoft.com/winfx/2006/xaml";
        private const string WinfxXAMLPresentationNamespaceUri = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        public const bool ImportMergedResourceDictionaryReferencesDefault = false;
        public const bool WriteFileHeaderDefault = true;
        public const string FileHeaderDefault = @"
    This code was generated by a tool.
    Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
";
        public const bool IncludeSourceFilesInFileHeaderDefault = true;

        public bool ImportMergedResourceDictionaryReferences { get; set; } = ImportMergedResourceDictionaryReferencesDefault;

        public bool WriteFileHeader { get; set; } = WriteFileHeaderDefault;

        public string FileHeader { get; set; } = FileHeaderDefault;

        public bool IncludeSourceFilesInFileHeader { get; set; } = IncludeSourceFilesInFileHeaderDefault;

        public ILogger? Logger { get; set; } = new TraceLogger();

        /// <summary>
        /// Combines multiple XAML resource dictionaries in one.
        /// </summary>
        /// <param name="sourceFile">Filename of list of XAML's.</param>
        /// <param name="targetFile">Result XAML filename.</param>
        public void Combine(string sourceFile, string targetFile)
        {
            this.Logger?.Debug($"Loading resources list from \"{sourceFile}\"");

            sourceFile = this.GetFullFilePath(sourceFile);

            // Load resource file list
            var resourceFileLines = File.ReadAllLines(sourceFile);

            this.Combine(resourceFileLines, targetFile);
        }

        /// <summary>
        /// Combines multiple XAML resource dictionaries in one.
        /// </summary>
        /// <param name="sourceFiles">Source files.</param>
        /// <param name="targetFile">Result XAML filename.</param>
        public string Combine(IReadOnlyCollection<string> sourceFiles, string targetFile)
        {
            // Create result XML document
            var finalDocument = new XDocument();
            var finalRootElement = new XElement(XName.Get(ResourceDictionaryString, WinfxXAMLPresentationNamespaceUri));
            finalDocument.Add(finalRootElement);

            XElement? mergedDictionariesListElement = default;

            // Associate key with ResourceElement
            var resourceElements = new Dictionary<string, ResourceElement>();

            // List of read resources
            var resourcesList = new List<ResourceElement>();

            var themeDictionaries = new Dictionary<string, XElement>();

            // For each resource file
            var orderedSourceFiles = sourceFiles.OrderBy(x => x)
                                                .ToArray();

            var seenNamespacesToFilesMapping = new Dictionary<XAttribute, string>();

            foreach (var resourceFile in orderedSourceFiles)
            {
                // ignore empty and lines that start with '#'
                if (string.IsNullOrEmpty(resourceFile)
                    || resourceFile.StartsWith("#"))
                {
                    continue;
                }

                var current = XDocument.Load(this.GetFullFilePath(resourceFile), LoadOptions.SetLineInfo | LoadOptions.SetBaseUri);

                this.Logger?.Debug($"Loading resource \"{resourceFile}\"");

                // Set and fix resource dictionary attributes
                var currentDocRoot = current.Root;
                if (currentDocRoot is null)
                {
                    this.Logger?.Warn($"\"{resourceFile}\" did not contain a document element.");
                    continue;
                }

                foreach (var attribute in currentDocRoot.Attributes())
                {
                    var duplicate = seenNamespacesToFilesMapping.Keys.FirstOrDefault(x => x.Name.LocalName == attribute.Name.LocalName
                                                                                        && x.Value != attribute.Value);

                    if (duplicate is not null)
                    {
                        var message = $"Namespace name \"{duplicate.Name.LocalName}\" with different values was seen in \"{seenNamespacesToFilesMapping[duplicate]}\" and \"{resourceFile}\".\nPlease use unique prefixes when their namespace values differs.";
                        this.Logger?.Error(message);
                        throw new Exception(message);
                    }

                    seenNamespacesToFilesMapping.Add(attribute, resourceFile);

                    if (finalRootElement.Attribute(attribute.Name.ToString()) is null)
                    {
                        // Add namespace to result resource dictionary
                        finalRootElement.SetAttributeValue(attribute.Name, attribute.Value);
                    }
                }

                // Extract resources
                foreach (var element in currentDocRoot.Elements())
                {
                    // Merged resource dictionaries (at the top)
                    if (element.Name.LocalName == MergedDictionariesString)
                    {
                        if (this.ImportMergedResourceDictionaryReferences)
                        {
                            if (finalRootElement.Elements().Any() == false)
                            {
                                mergedDictionariesListElement = new XElement(XName.Get(MergedDictionariesString, WinfxXAMLPresentationNamespaceUri));
                                finalRootElement.Add(mergedDictionariesListElement);
                            }
                            else if (element.Name.LocalName != MergedDictionariesString)
                            {
                                mergedDictionariesListElement = new XElement(XName.Get(MergedDictionariesString, WinfxXAMLPresentationNamespaceUri));
                                finalRootElement.AddFirst(mergedDictionariesListElement);
                            }

                            if (mergedDictionariesListElement is null)
                            {
                                continue;
                            }

                            var currentMergedSources = mergedDictionariesListElement.Elements()
                                                                                    .Select(mergedDictionaryElement => mergedDictionaryElement.Attribute("Source"))
                                                                                    .Where(source => source is not null && string.IsNullOrEmpty(source.Value) == false)
                                                                                    .ToList();

                            foreach (var mergedDictionaryReference in element.Elements())
                            {
                                // #65 => Import everything as is if it's not a regular ResourceDictionary
                                if (mergedDictionaryReference.Name.LocalName != ResourceDictionaryString)
                                {
                                    // Import non ResourceDictionary reference element from processed XML document to final XML document
                                    var importedNonResourceDictionaryReference = new XElement(mergedDictionaryReference);
                                    mergedDictionariesListElement.Add(importedNonResourceDictionaryReference);
                                    continue;
                                }

                                var sourceAttribute = mergedDictionaryReference.Attribute("Source");
                                if (sourceAttribute?.Value is { Length: > 0 } sourceValue)
                                {
                                    // Check if it's processed by combine
                                    // Not ideal but should be enough for most cases
                                    const string COMPONENT_MARKER = ";component/";

                                    var componentMarkerIndex = sourceValue.IndexOf(COMPONENT_MARKER, StringComparison.OrdinalIgnoreCase);
                                    if (componentMarkerIndex is -1)
                                    {
                                        this.Logger?.Warn(string.Format($"Ignored merged ResourceDictionary inside \"{resourceFile}\" because it's source has no 'component' path.{Environment.NewLine}{GetDebugInfo(mergedDictionaryReference)}"));
                                        continue;
                                    }

                                    var sourceRelativeFilePath = sourceValue.Remove(0, componentMarkerIndex + COMPONENT_MARKER.Length);
                                    sourceRelativeFilePath = sourceRelativeFilePath.Replace("/", "\\");
                                    if (orderedSourceFiles.Contains(sourceRelativeFilePath))
                                    {
                                        continue;
                                    }

                                    if (string.IsNullOrEmpty(sourceValue))
                                    {
                                        this.Logger?.Warn(string.Format($"Ignored merged ResourceDictionary inside \"{resourceFile}\".{Environment.NewLine}{GetDebugInfo(mergedDictionaryReference)}"));
                                        continue;
                                    }

                                    // Check if it was already added
                                    if (currentMergedSources.Any(x => x.Equals(sourceValue)))
                                    {
                                        continue;
                                    }
                                }

                                // Import ResourceDictionary reference element from processed XML document to final XML document
                                var importedResourceDictionaryReference = new XElement(mergedDictionaryReference);
                                mergedDictionariesListElement.Add(importedResourceDictionaryReference);
                            }
                        }

                        // Always continue after merged dictionary elements
                        continue;
                    }

                    // WinUI / UWP
                    if (element.Name.LocalName == ThemeDictionariesString)
                    {
                        foreach (var childElement in element.Elements())
                        {
                            var importedElement = new XElement(childElement);
                            var key = GetKey(importedElement, WinfxXAMLNamespaceUri);

                            if (string.IsNullOrEmpty(key))
                            {
                                continue;
                            }

                            if (themeDictionaries.TryGetValue(key, out var mergedThemeDictionary) == false)
                            {
                                mergedThemeDictionary = importedElement;
                                themeDictionaries.Add(key, mergedThemeDictionary);
                            }
                            else
                            {
                                foreach (var resourceDictionaryChild in childElement.Elements())
                                {
                                    mergedThemeDictionary.Add(new XElement(resourceDictionaryChild));
                                }
                            }
                        }
                    }

                    // Resources
                    {
                        // Import XML element from one XML document to result XML document
                        var importedElement = new XElement(element);

                        // Find resource key
                        var key = GetKey(importedElement, WinfxXAMLNamespaceUri);

                        if (string.IsNullOrEmpty(key))
                        {
                            this.Logger?.Warn($"Element had no key and was skipped.{Environment.NewLine}{GetDebugInfo(element)}");
                            continue;
                        }

                        // Check key unique
                        if (resourceElements.TryGetValue(key, out var existingElement) == false)
                        {
                            // Create ResourceElement for key and XML element
                            var res = new ResourceElement(key, importedElement, GetUsedKeys(importedElement))
                                {
                                    ElementDebugInfo = GetDebugInfo(element)
                                };
                            resourceElements.Add(key, res);
                            resourcesList.Add(res);
                        }
                        else if (importedElement.ToString() != existingElement.Element.ToString())
                        {
                            this.Logger?.Warn($"Key \"{key}\" was found in multiple imported files, with differing content, and was skipped.{Environment.NewLine}Existing: {existingElement.GetElementDebugInfo()}{Environment.NewLine}Current: {GetDebugInfo(element)}");
                            continue;
                        }
                    }
                }
            }

            // Result list
            var finalOrderList = new List<ResourceElement>();

            // Add all items with empty UsedKeys
            for (var i = 0; i < resourcesList.Count; i++)
            {
                if (resourcesList[i].UsedKeys.Length == 0)
                {
                    finalOrderList.Add(resourcesList[i]);

                    this.Logger?.Debug($"Adding resource \"{resourcesList[i].Key}\"");

                    resourcesList.RemoveAt(i);
                    i--;
                }
            }

            // Add other resources in correct order
            while (resourcesList.Count > 0)
            {
                for (var i = 0; i < resourcesList.Count; i++)
                {
                    // Check used keys is in result list
                    var containsAll = true;
                    for (var j = 0; j < resourcesList[i].UsedKeys.Length; j++)
                    {
                        if (resourceElements.ContainsKey(resourcesList[i].UsedKeys[j])
                            && finalOrderList.Contains(resourceElements[resourcesList[i].UsedKeys[j]]) == false)
                        {
                            containsAll = false;
                            break;
                        }
                    }

                    // If all used keys are in the result list add this resource to result list
                    if (containsAll)
                    {
                        finalOrderList.Add(resourcesList[i]);

                        this.Logger?.Debug($"Adding resource \"{resourcesList[i].Key}\"");

                        resourcesList.RemoveAt(i);
                        i--;
                    }
                }

                // TODO: Limit iterations count.
            }

            // WinUI / UWP
            if (themeDictionaries.Any())
            {
                var themeDictionariesElement = new XElement(XName.Get(ThemeDictionariesString, WinfxXAMLPresentationNamespaceUri));

                finalRootElement.Add(themeDictionariesElement);

                foreach (var themeDictionary in themeDictionaries)
                {
                    themeDictionariesElement.Add(themeDictionary.Value);
                }
            }

            // Add elements to XML document
            for (var i = 0; i < finalOrderList.Count; i++)
            {
                finalRootElement.Add(finalOrderList[i].Element);
            }

            if (this.WriteFileHeader)
            {
                this.AddFileHeader(finalDocument, orderedSourceFiles);
            }

            this.CleanUpEmptyElements(finalDocument);

            // Save result file
            return this.WriteResultFile(targetFile, finalDocument);
        }

        private void CleanUpEmptyElements(XDocument finalDocument)
        {
            foreach (var descendantNode in finalDocument.DescendantNodes())
            {
                if (descendantNode is not XElement element)
                {
                    continue;
                }

                if (element.HasElements == false
                    && element.Value == string.Empty)
                {
                    element.RemoveNodes();
                }
            }
        }

        private static string GetKey(XElement importedElement, string winfxXamlNamespaceAttributeName)
        {
            // TODO: Are there any other variants???
            var key = importedElement.Attribute("Key")?.Value;

            if (string.IsNullOrEmpty(key))
            {
                key = importedElement.Attribute(XName.Get("Key", winfxXamlNamespaceAttributeName))?.Value;
            }

            if (string.IsNullOrEmpty(key))
            {
                key = importedElement.Attribute(XName.Get("Name", winfxXamlNamespaceAttributeName))?.Value;
            }

            if (string.IsNullOrEmpty(key))
            {
                key = importedElement.Attribute("TargetType")?.Value;
            }

            // WinUI / UWP
            if (string.IsNullOrEmpty(key) == false)
            {
                // If this element has a key and a conditional-inclusion namespace, we'll attach a prefix
                // to the key corresponding to the condition we checked in order to allow multiple such elements
                // with the same key to exist.
                var indexOfContractPresent = importedElement.Name.Namespace.NamespaceName.IndexOf("IsApiContract", StringComparison.Ordinal);

                if (indexOfContractPresent >= 0)
                {
                    key = importedElement.Name.Namespace.NamespaceName.Substring(indexOfContractPresent) + ":" + key;
                }
            }

            return key ?? string.Empty;
        }

        private void AddFileHeader(XDocument finalDocument, IReadOnlyCollection<string> sourceFiles)
        {
            var root = finalDocument.Root;

            var fileHeaderComment = new XComment(this.FileHeader);

            root.AddBeforeSelf(fileHeaderComment);

            if (this.IncludeSourceFilesInFileHeader)
            {
                var sourceFilesCommentContent = $@"
Source files:
{string.Join(Environment.NewLine, sourceFiles)}
";
                var sourceFilesComment = new XComment(sourceFilesCommentContent);
                fileHeaderComment.AddAfterSelf(sourceFilesComment);
            }
        }

        private string GetFullFilePath(string file)
        {
            if (File.Exists(file) == false)
            {
                throw new FileNotFoundException("Unable to find file.", file);
            }

            return Path.GetFullPath(file);
        }

        /// <summary>
        /// Find all used keys for resource.
        /// </summary>
        /// <param name="element">Xml element which contains resource.</param>
        /// <returns>Array of keys used by resource.</returns>
        private static string[] GetUsedKeys(XElement element)
        {
            // Result list
            var result = new List<string>();

            // Check all attributes
            foreach (var attr in element.Attributes())
            {
                if (attr is null)
                {
                    continue;
                }

                if (attr.Value.StartsWith("{")
                    && resourceUsageRegex.Matches(attr.Value) is { Count: > 0 } matches)
                {
                    foreach (Match match in matches)
                    {
                        var resourceKey = match.Groups["ResourceKey"].Value;

                        // compensate regex cutting of trailing "}" if key starts with "{".
                        if (resourceKey.StartsWith("{", StringComparison.Ordinal)
                            && resourceKey.EndsWith("}", StringComparison.Ordinal) is false)
                        {
                            resourceKey += "}";
                        }

                        // Add key to result
                        if (result.Contains(resourceKey) == false)
                        {
                            result.Add(resourceKey);
                        }
                    }
                }
            }

            // Check child elements
            foreach (var childElement in element.Elements())
            {
                result.AddRange(GetUsedKeys(childElement));
            }

            return result.ToArray();
        }

        private string WriteResultFile(string resultFile, XDocument finalDocument)
        {
            try
            {
                resultFile = resultFile.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                var stringWriter = new StringWriter();
                var xmlWriterSettings = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        Indent = true,
                        IndentChars = "  ",
                        NewLineHandling = NewLineHandling.None,
                        NamespaceHandling = NamespaceHandling.OmitDuplicates
                    };
                var xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);

                using (stringWriter)
                {
                    using (xmlWriter)
                    {
                        finalDocument.WriteTo(xmlWriter);
                    }
                }

                var tempFileContent = stringWriter.ToString();
                var tempFileContent2 = finalDocument.ToString(SaveOptions.OmitDuplicateNamespaces);

                this.Logger?.Debug($"Checking \"{resultFile}\"...");

                var fileHasToBeWritten = File.Exists(resultFile) == false
                                         || FileHelper.ReadAllTextSharedWithRetry(resultFile) != tempFileContent;

                if (fileHasToBeWritten)
                {
                    var directory = Path.GetDirectoryName(resultFile);

                    if (string.IsNullOrEmpty(directory) == false)
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var sw = new StreamWriter(resultFile, false, Encoding.UTF8, BufferSize))
                    {
                        sw.Write(tempFileContent);
                    }

                    this.Logger?.Debug($"Resource Dictionary saved to \"{resultFile}\".");
                }
                else
                {
                    this.Logger?.Debug("New Resource Dictionary did not differ from existing file. No new file written.");
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error during Resource Dictionary saving: {0}", e);
            }

            return resultFile;
        }

        private static string GetDebugInfo(XElement element)
        {
            if (element is IXmlLineInfo lineInfo
                && lineInfo.HasLineInfo())
            {
                return $"At: {lineInfo.LineNumber}:{lineInfo.LinePosition} ({element.Document.BaseUri}){Environment.NewLine}{element}";
            }

            return element.ToString();
        }
    }
}
