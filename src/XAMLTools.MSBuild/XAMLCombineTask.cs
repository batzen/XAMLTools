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

            var grouped = this.Items.GroupBy(x => x.GetMetadata(TargetFileMetadataName));

            if (this.ImportMergedResourceDictionaryReferences)
            {
                this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Import for merged ResourceDictionary elements enabled for this generated content", string.Empty, nameof(XAMLCombine), MessageImportance.Low));
            }

            foreach (var item in grouped)
            {
                var sourceFiles = item.Select(x => x.ItemSpec).ToList();
                var targetFile = item.Key;

                this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Generating combined XAML file \"{targetFile}\".", string.Empty, nameof(XAMLCombineTask), MessageImportance.High));

                var combiner = new XAMLCombiner
                {
                    ImportMergedResourceDictionaryReferences = this.ImportMergedResourceDictionaryReferences,
                    Logger = new Logger(this.BuildEngine, nameof(XAMLCombineTask))
                };
                targetFile = MutexHelper.ExecuteLocked(() => combiner.Combine(sourceFiles, targetFile), targetFile);

                generatedFiles.Add(new TaskItem(targetFile));
            }

            this.GeneratedFiles = generatedFiles.ToArray();

            return true;
        }
    }
}
