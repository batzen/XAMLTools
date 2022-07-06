namespace XAMLTools.XAMLCombine;

public interface IXamlCombinerOptions
{
    bool ImportMergedResourceDictionaryReferences { get; }

    bool WriteFileHeader { get; }

    string FileHeader { get; }

    bool IncludeSourceFilesInFileHeader { get; }
}