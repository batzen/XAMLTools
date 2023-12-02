namespace XAMLTools.XAMLCombine
{
    using System.Xml.Linq;

    /// <summary>
    /// Represents a XAML resource.
    /// </summary>
    public class ResourceElement
    {
        public ResourceElement(string key, XElement element, string[] usedKeys)
        {
            this.Key = key;
            this.Element = element;
            this.UsedKeys = usedKeys;
        }

        /// <summary>
        /// Resource key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Resource XML node.
        /// </summary>
        public XElement Element { get; }

        /// <summary>
        /// XAML keys used in this resource.
        /// </summary>
        public string[] UsedKeys { get; }

        public string? ElementDebugInfo { get; set; }

        public string GetElementDebugInfo()
        {
            if (string.IsNullOrEmpty(this.ElementDebugInfo) is false)
            {
                return this.ElementDebugInfo!;
            }

            return this.Element.ToString();
        }
    }
}