namespace XAMLTools.XAMLCombine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

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
        public string Combine(IEnumerable<string> sourceFiles, string targetFile)
        {
            // Create result XML document
            var finalDocument = new XmlDocument();
            var finalRootNode = finalDocument.CreateElement("ResourceDictionary", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            finalDocument.AppendChild(finalRootNode);

            // List of existing keys, to avoid duplicates
            var keys = new List<string>();

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
                            ChangeNamespacePrefix(currentDocRoot, currentDocAttribute.LocalName, name);

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
                                    this.Logger?.Warn($"Normalizing namespace prefix from \"{currentDocAttribute.Name}\" found in \"{resourceFile}\" \"{attributeFromFinalRoot.Name}\".");

                                    currentDocRoot.SetAttribute(currentDocAttribute.Name, currentDocAttribute.Value);
                                    ChangeNamespacePrefix(currentDocRoot, currentDocAttribute.LocalName, attributeFromFinalRoot.LocalName);
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
                    if (node is XmlElement
                        && node.Name != "ResourceDictionary.MergedDictionaries")
                    {
                        // Import XML node from one XML document to result XML document
                        var importedElement = (XmlElement)finalDocument.ImportNode(node, true);

                        // Find resource key
                        // TODO: Is any other variants???
                        var key = string.Empty;
                        if (importedElement.HasAttribute("Key"))
                        {
                            key = importedElement.Attributes["Key"].Value;
                        }
                        else if (importedElement.HasAttribute("x:Key"))
                        {
                            key = importedElement.Attributes["x:Key"].Value;
                        }
                        else if (importedElement.HasAttribute("TargetType"))
                        {
                            key = importedElement.Attributes["TargetType"].Value;
                        }

                        if (string.IsNullOrEmpty(key) == false)
                        {
                            // Check key unique
                            if (keys.Contains(key))
                            {
                                this.Logger?.Warn($"Duplicate key \"{key}\" found in \"{resourceFile}\".");
                                continue;
                            }

                            keys.Add(key);

                            // Create ResourceElement for key and XML node
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
        private static void ChangeNamespacePrefix(XmlElement element, string oldPrefix, string newPrefix)
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

                    // Check {x:Type {x:Static in attributes values
                    // TODO: Is any other???
                    if ((attr.Value.Contains("{x:Type") || attr.Value.Contains("{x:Static"))
                        && attr.Value.Contains(oldStringSpaced))
                    {
                        attr.Value = attr.Value.Replace(oldStringSpaced, newStringSpaced);
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
                ChangeNamespacePrefix(childElement, oldPrefix, newPrefix);
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
                                         || ReadAllTextShared(resultFile) != tempFileContent;

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

        private static string ReadAllTextShared(string file)
        {
            Stream? stream = null;
            try
            {
                stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);

                using (var textReader = new StreamReader(stream))
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
    }
}