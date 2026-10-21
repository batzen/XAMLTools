namespace XAMLTools.XAMLCombine;

using System.Collections.Generic;

public interface IXamlCombinerOptions
{
    bool ImportMergedResourceDictionaryReferences { get; }

    bool WriteFileHeader { get; }

    string FileHeader { get; }

    bool IncludeSourceFilesInFileHeader { get; }

    IEnumerable<string> ExcludedNameSpaces { get; }
}