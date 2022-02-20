namespace XAMLTools.MSBuild
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System.Collections.Generic;
    using System.Linq;
    using XAMLTools.Helpers;
    using XAMLTools.XAMLCombine;

    public class XAMLCombineTask : Task
    {
        public const string TargetFileMetadataName = "TargetFile";

        [Required]
        public ITaskItem[] Items { get; set; } = null!;

        [Output]
        public ITaskItem[]? GeneratedFiles { get; set; }

        public bool ImportMergedResourceDictionaryReferences { get; set; }

        public override bool Execute()
        {
            var generatedFiles = new List<ITaskItem>();

            var grouped = Items.GroupBy(x => x.GetMetadata(TargetFileMetadataName));

            var importMergedResourceDictionaryReferences = ImportMergedResourceDictionaryReferences;

            if (importMergedResourceDictionaryReferences)
            {
                BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Import for merged ResourceDictionary elements enabled for this generated content", string.Empty, nameof(XAMLCombine), MessageImportance.Low));
            }

            foreach (var item in grouped)
            {
                var sourceFiles = item.Select(x => x.ItemSpec);
                var targetFile = item.Key;

                BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Generating combined XAML file \"{targetFile}\".", string.Empty, nameof(XAMLCombineTask), MessageImportance.High));

                var combiner = new XAMLCombiner
                {
                    Logger = new Logger(this.BuildEngine, nameof(XAMLCombineTask))
                };
                targetFile = MutexHelper.ExecuteLocked(() => combiner.Combine(sourceFiles, targetFile, importMergedResourceDictionaryReferences), targetFile);

                generatedFiles.Add(new TaskItem(targetFile));
            }

            GeneratedFiles = generatedFiles.ToArray();

            return true;
        }
    }
}
