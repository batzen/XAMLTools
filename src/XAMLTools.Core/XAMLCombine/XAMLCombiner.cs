﻿namespace XAMLTools.XAMLCombine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using XAMLTools.Helpers;

    public class XAMLCombiner
    {
        private const int BufferSize = 32768; // 32 Kilobytes

        /// <summary>
        /// Dynamic resource string.
        /// </summary>
        private const string DynamicResourceString = "{DynamicResource ";

        /// <summary>
        /// Static resource string.
        /// </summary>
        private const string StaticResourceString = "{StaticResource ";

        private const string MergedDictionariesString = "ResourceDictionary.MergedDictionaries";

        private const string ResourceDictionaryString = "ResourceDictionary";

        private const string WinfxXAMLPresentationNamespaceUri = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        public bool ImportMergedResourceDictionaryReferences { get; set; }

        public ILogger? Logger { get; set; } = new TraceLogger();

        /// <summary>
        /// Expression matches substring which placed inside brackets, following '=' symbol
        /// </summary>
        private static readonly Regex markupExtensionSearch = new(@"(?<=\=\{)([^\{\}])+", RegexOptions.Compiled);

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
            var finalDocument = new XmlDocument();
            var finalRootNode = finalDocument.CreateElement(ResourceDictionaryString, WinfxXAMLPresentationNamespaceUri);
            finalDocument.AppendChild(finalRootNode);

            XmlElement? mergedDictionariesListNode = default;

            // List of existing keys, to avoid duplicates
            var keys = new HashSet<string>();

            // Associate key with ResourceElement
            var resourceElements = new Dictionary<string, ResourceElement>();

            // List of read resources
            var resourcesList = new List<ResourceElement>();

            // For each resource file
            foreach (var resourceFile in sourceFiles.OrderBy(x => x))
            {
                // ignore empty and lines that start with '#'
                if (string.IsNullOrEmpty(resourceFile)
                    || resourceFile.StartsWith("#"))
                {
                    continue;
                }

                var current = new XmlDocument();
                current.Load(this.GetFullFilePath(resourceFile));

                this.Logger?.Debug($"Loading resource \"{resourceFile}\"");

                // Set and fix resource dictionary attributes
                var currentDocRoot = current.DocumentElement;
                if (currentDocRoot == null)
                {
                    continue;
                }

                var winfxXamlNamespaceAttributeName = "x";

                // Find http://schemas.microsoft.com/winfx/2006/xaml namespace mapping
                foreach (XmlAttribute attribute in currentDocRoot.Attributes)
                {
                    var namespaceUri = attribute.Value;
                    if (string.Equals(namespaceUri, "http://schemas.microsoft.com/winfx/2006/xaml"))
                    {
                        winfxXamlNamespaceAttributeName = attribute.LocalName;
                    }
                }

                for (var j = 0; j < currentDocRoot.Attributes.Count; j++)
                {
                    var currentDocAttribute = currentDocRoot.Attributes[j];
                    if (finalRootNode.HasAttribute(currentDocAttribute.Name))
                    {
                        // If namespace with this name exists and not equal
                        if (currentDocAttribute.Value != finalRootNode.Attributes[currentDocAttribute.Name].Value
                            && currentDocAttribute.Prefix == "xmlns")
                        {
                            // Create new namespace name
                            var index = -1;
                            string name;
                            do
                            {
                                index++;
                                name = currentDocAttribute.LocalName + "_" + index.ToString(CultureInfo.InvariantCulture);
                            }
                            while (finalRootNode.HasAttribute("xmlns:" + name));

                            currentDocRoot.SetAttribute("xmlns:" + name, currentDocAttribute.Value);

                            // Change namespace prefixes in resource dictionary
                            ChangeNamespacePrefix(currentDocRoot, currentDocAttribute.LocalName, name, winfxXamlNamespaceAttributeName);

                            // Add renamed namespace
                            var a = finalDocument.CreateAttribute("xmlns", name, currentDocAttribute.NamespaceURI);
                            a.Value = currentDocAttribute.Value;
                            finalRootNode.Attributes.Append(a);
                        }
                    }
                    else
                    {
                        var exists = false;
                        if (currentDocAttribute.Prefix == "xmlns")
                        {
                            // Try to find equal namespace with different name
                            foreach (XmlAttribute? attributeFromFinalRoot in finalRootNode.Attributes)
                            {
                                if (attributeFromFinalRoot is null)
                                {
                                    continue;
                                }

                                if (currentDocAttribute.Value == attributeFromFinalRoot.Value)
                                {
                                    this.Logger?.Warn($"Normalizing namespace prefix from \"{currentDocAttribute.Name}\" to \"{attributeFromFinalRoot.Name}\" found in \"{resourceFile}\".");

                                    currentDocRoot.SetAttribute(currentDocAttribute.Name, currentDocAttribute.Value);
                                    ChangeNamespacePrefix(currentDocRoot, currentDocAttribute.LocalName, attributeFromFinalRoot.LocalName, winfxXamlNamespaceAttributeName);
                                    exists = true;
                                    break;
                                }
                            }
                        }

                        if (exists == false)
                        {
                            // Add namespace to result resource dictionary
                            var a = finalDocument.CreateAttribute(currentDocAttribute.Prefix, currentDocAttribute.LocalName, currentDocAttribute.NamespaceURI);
                            a.Value = currentDocAttribute.Value;
                            finalRootNode.Attributes.Append(a);
                        }
                    }
                }

                // Extract resources
                foreach (XmlNode? node in currentDocRoot.ChildNodes)
                {
                    if (node is not XmlElement xmlElement)
                    {
                        continue;
                    }

                    // Merged resource dictionaries (at the top)
                    if (node.Name == MergedDictionariesString)
                    {
                        if (this.ImportMergedResourceDictionaryReferences)
                        {
                            if (finalRootNode.ChildNodes.Count == 0)
                            {
                                mergedDictionariesListNode = finalDocument.CreateElement(MergedDictionariesString, WinfxXAMLPresentationNamespaceUri);
                                finalRootNode.AppendChild(mergedDictionariesListNode);
                            }
                            else if (finalRootNode.FirstChild.Name != MergedDictionariesString)
                            {
                                mergedDictionariesListNode = finalDocument.CreateElement(MergedDictionariesString, WinfxXAMLPresentationNamespaceUri);
                                finalRootNode.InsertBefore(mergedDictionariesListNode, finalRootNode.FirstChild);
                            }

                            if (mergedDictionariesListNode == null)
                            {
                                continue;
                            }

                            var currentMergedSources = mergedDictionariesListNode.ChildNodes.OfType<XmlElement>()
                                .Select(nodeElement => nodeElement.GetAttribute("Source"))
                                .Where(source => !string.IsNullOrEmpty(source))
                                .ToList();

                            foreach (var mergedDictionaryReference in xmlElement.ChildNodes)
                            {
                                if (mergedDictionaryReference is not XmlElement mergedDictionaryReferenceElement || mergedDictionaryReferenceElement.Name != ResourceDictionaryString)
                                {
                                    continue;
                                }

                                var sourceValue = mergedDictionaryReferenceElement.GetAttribute("Source");
                                // Check if it's processed by combine
                                // Not ideal but should be enough for most cases
                                var sourceRelativeFilePath = sourceValue.Remove(0, sourceValue.IndexOf(";component/", StringComparison.Ordinal) + ";component/".Length);
                                sourceRelativeFilePath = sourceRelativeFilePath.Replace("/", "\\");
                                if (sourceFiles.Contains(sourceRelativeFilePath))
                                {
                                    continue;
                                }

                                if (string.IsNullOrEmpty(sourceValue))
                                {
                                    this.Logger?.Warn(string.Format($"Ignore merged ResourceDictionary inside resource \"{resourceFile}\""));
                                    continue;
                                }

                                // Check if it was already added
                                if (currentMergedSources.Any(x => x.Equals(sourceValue)))
                                {
                                    continue;
                                }

                                // Import ResourceDictionary reference node from processed XML document to final XML document                        
                                var importedResourceDictionaryReference = (XmlElement)finalDocument.ImportNode(mergedDictionaryReferenceElement, false);
                                mergedDictionariesListNode.AppendChild(importedResourceDictionaryReference);
                            }
                        }

                        // Always continue after merged dictionary nodes
                        continue;
                    }

                    // Resources

                    // Import XML node from one XML document to result XML document
                    var importedElement = (XmlElement)finalDocument.ImportNode(xmlElement, true);

                    // Find resource key
                    // TODO: Is any other variants???
                    var key = importedElement.GetAttribute("Key");
                    if (string.IsNullOrEmpty(key))
                    {
                        key = importedElement.GetAttribute("x:Key");
                    }

                    if (string.IsNullOrEmpty(key))
                    {
                        key = importedElement.GetAttribute("TargetType");
                    }

                    if (string.IsNullOrEmpty(key) == false)
                    {
                        // Check key unique
                        if (keys.Contains(key))
                        {
                            continue;
                        }

                        if (keys.Add(key))
                        {
                            // Create ResourceElement for key and XML  node
                            var res = new ResourceElement(key, importedElement, FillKeys(importedElement));
                            resourceElements.Add(key, res);
                            resourcesList.Add(res);
                        }
                    }

                    // TODO: Add output information.
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

            // Add nodes to XML document
            for (var i = 0; i < finalOrderList.Count; i++)
            {
                finalRootNode.AppendChild(finalOrderList[i].Element);
            }

            // Save result file
            return this.WriteResultFile(targetFile, finalDocument);
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
        /// Changes namespace prefix for XML node.
        /// </summary>
        /// <param name="element">XML node.</param>
        /// <param name="oldPrefix">Old namespace prefix.</param>
        /// <param name="newPrefix">New namespace prefix.</param>
        /// <param name="winfxXamlNamespaceMapping">The local name for winfx xaml namespace</param>
        private static void ChangeNamespacePrefix(XmlElement element, string oldPrefix, string newPrefix, string winfxXamlNamespaceMapping)
        {
            // String for search
            var oldString = oldPrefix + ":";
            var newString = newPrefix + ":";
            var oldStringSpaced = " " + oldString;
            var newStringSpaced = " " + newString;

            foreach (XmlNode? child in element.ChildNodes)
            {
                if (child is not XmlElement childElement)
                {
                    continue;
                }


                if (child.Prefix == oldPrefix)
                {
                    child.Prefix = newPrefix;
                }

                foreach (XmlAttribute? attr in childElement.Attributes)
                {
                    if (attr is null)
                    {
                        continue;
                    }

                    // Check all attributes prefix
                    if (attr.Prefix == oldPrefix)
                    {
                        attr.Prefix = newPrefix;
                    }

                    if (attr.Value.Contains(oldStringSpaced))
                    {
                        // Check {x:Type {x:Static in attributes values
                        // TODO: Is any other???
                        if (attr.Value.Contains($"{{{winfxXamlNamespaceMapping}:Type") || attr.Value.Contains($"{{{winfxXamlNamespaceMapping}:Static"))
                        {
                            attr.Value = attr.Value.Replace(oldStringSpaced, newStringSpaced);
                        }
                    }
                    else
                    {
                        if (attr.Value.Contains(oldString))
                        {
                            // Check MarkdownExtension
                            var match = markupExtensionSearch.Match(attr.Value);
                            if (match.Success && match.Value.StartsWith(oldString))
                            {
                                attr.Value = attr.Value.Replace(oldString, newString);
                            }
                        }
                    }

                    // Check Property attribute
                    // TODO: Is any other???
                    if (attr.Name == "Property"
                        && attr.Value.StartsWith(oldString))
                    {
                        attr.Value = attr.Value.Replace(oldString, newString);
                    }
                }

                // Change namespaces for child node
                ChangeNamespacePrefix(childElement, oldPrefix, newPrefix, winfxXamlNamespaceMapping);
            }
        }

        /// <summary>
        /// Find all used keys for resource.
        /// </summary>
        /// <param name="element">Xml element which contains resource.</param>
        /// <returns>Array of keys used by resource.</returns>
        private static string[] FillKeys(XmlElement element)
        {
            // Result list
            var result = new List<string>();

            // Check all attributes
            foreach (XmlAttribute? attr in element.Attributes)
            {
                if (attr is null)
                {
                    continue;
                }

                if (attr.Value.StartsWith(DynamicResourceString))
                {
                    // Find key
                    var key = attr.Value.Substring(DynamicResourceString.Length, attr.Value.Length - DynamicResourceString.Length - 1).Trim();

                    // Add key to result
                    if (result.Contains(key) == false)
                    {
                        result.Add(key);
                    }
                }
                else if (attr.Value.StartsWith(StaticResourceString))
                {
                    // Find key
                    var key = attr.Value.Substring(StaticResourceString.Length, attr.Value.Length - StaticResourceString.Length - 1).Trim();

                    // Add key to result
                    if (result.Contains(key) == false)
                    {
                        result.Add(key);
                    }
                }
            }

            // Check child nodes
            foreach (XmlNode? node in element.ChildNodes)
            {
                if (node is XmlElement nodeElement)
                {
                    result.AddRange(FillKeys(nodeElement));
                }
            }

            return result.ToArray();
        }

        private string WriteResultFile(string resultFile, XmlDocument finalDocument)
        {
            try
            {
                resultFile = resultFile.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                var stringWriter = new UTF8StringWriter();

                using (stringWriter)
                {
                    finalDocument.Save(stringWriter);
                }

                var tempFileContent = stringWriter.ToString();

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
    }
}
