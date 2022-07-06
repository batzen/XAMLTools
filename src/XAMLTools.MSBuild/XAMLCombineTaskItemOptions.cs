namespace XAMLTools.MSBuild;

using System;
using Microsoft.Build.Framework;
using XAMLTools.XAMLCombine;

public class XAMLCombineTaskItemOptions : IXamlCombinerOptions, IEquatable<XAMLCombineTaskItemOptions>
{
    public string TargetFile { get; init; } = string.Empty;

    public bool ImportMergedResourceDictionaryReferences { get; init; } = XAMLCombiner.ImportMergedResourceDictionaryReferencesDefault;

    public bool WriteFileHeader { get; init; } = XAMLCombiner.WriteFileHeaderDefault;

    public string FileHeader { get; init; } = XAMLCombiner.FileHeaderDefault;

    public bool IncludeSourceFilesInFileHeader { get; init; } = XAMLCombiner.IncludeSourceFilesInFileHeaderDefault;

    public static XAMLCombineTaskItemOptions From(ITaskItem taskItem)
    {
        var result = new XAMLCombineTaskItemOptions
        {
            TargetFile = taskItem.GetMetadata(nameof(TargetFile)),
            ImportMergedResourceDictionaryReferences = GetBool(taskItem, nameof(ImportMergedResourceDictionaryReferences), XAMLCombiner.ImportMergedResourceDictionaryReferencesDefault),
            WriteFileHeader = GetBool(taskItem, nameof(WriteFileHeader), XAMLCombiner.WriteFileHeaderDefault),
            FileHeader = GetString(taskItem, nameof(FileHeader), XAMLCombiner.FileHeaderDefault),
            IncludeSourceFilesInFileHeader = GetBool(taskItem, nameof(IncludeSourceFilesInFileHeader), XAMLCombiner.IncludeSourceFilesInFileHeaderDefault),
        };

        return result;
    }

    public override bool Equals(object obj)
    {
        if (obj is not XAMLCombineTaskItemOptions other)
        {
            return false;
        }

        return this.Equals(other);
    }

    public bool Equals(XAMLCombineTaskItemOptions? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return this.TargetFile == other.TargetFile
               && this.ImportMergedResourceDictionaryReferences == other.ImportMergedResourceDictionaryReferences
               && this.WriteFileHeader == other.WriteFileHeader
               && this.FileHeader == other.FileHeader
               && this.IncludeSourceFilesInFileHeader == other.IncludeSourceFilesInFileHeader;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (this.TargetFile != null
                ? this.TargetFile.GetHashCode()
                : 0);
            hashCode = (hashCode * 397) ^ this.ImportMergedResourceDictionaryReferences.GetHashCode();
            hashCode = (hashCode * 397) ^ this.WriteFileHeader.GetHashCode();
            hashCode = (hashCode * 397) ^ this.FileHeader.GetHashCode();
            hashCode = (hashCode * 397) ^ this.IncludeSourceFilesInFileHeader.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(XAMLCombineTaskItemOptions? left, XAMLCombineTaskItemOptions? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(XAMLCombineTaskItemOptions? left, XAMLCombineTaskItemOptions? right)
    {
        return !Equals(left, right);
    }

    private static bool GetBool(ITaskItem taskItem, string metadataName, bool defaultValue)
    {
        var metaData = taskItem.GetMetadata(metadataName);

        return bool.TryParse(metaData, out var value)
            ? value
            : defaultValue;
    }

    private static string GetString(ITaskItem taskItem, string metadataName, string defaultValue)
    {
        var metaData = taskItem.GetMetadata(metadataName);

        if (string.IsNullOrEmpty(metaData))
        {
            return defaultValue;
        }

        return metaData;
    }
}