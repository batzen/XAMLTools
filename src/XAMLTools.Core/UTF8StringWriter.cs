namespace XAMLTools;

using System.IO;
using System.Text;

public sealed class UTF8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}