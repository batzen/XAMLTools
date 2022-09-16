namespace XAMLTools.MSBuild
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System.Collections.Generic;
    using System.Linq;
    using XAMLTools.Helpers;
    using XAMLTools.XAMLCombine;

    public class XAMLCombineTask : Task
    {
        [Required]
        public ITaskItem[] Items { get; set; } = null!;

        [Output]
        public ITaskItem[]? GeneratedFiles { get; set; }

        public override bool Execute()
        {
            var generatedFiles = new List<ITaskItem>();

            var grouped = this.Items.GroupBy(XAMLCombineTaskItemOptions.From);

            foreach (var group in grouped)
            {
                var options = group.Key;
                var targetFile = options.TargetFile;

                if (targetFile is null or { Length: 0 })
                {
                    continue;
                }

                var sourceFiles = group.Select(x => x.ItemSpec).ToList();

                this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Generating combined XAML file \"{targetFile}\".", string.Empty, nameof(XAMLCombineTask), MessageImportance.High));

                if (options.ImportMergedResourceDictionaryReferences)
                {
                    this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Import for merged ResourceDictionary elements enabled for this generated content", string.Empty, nameof(XAMLCombine), MessageImportance.Low));
                }

                var combiner = new XAMLCombiner
                {
                    ImportMergedResourceDictionaryReferences = options.ImportMergedResourceDictionaryReferences,
                    WriteFileHeader = options.WriteFileHeader,
                    FileHeader = options.FileHeader,
                    IncludeSourceFilesInFileHeader = options.IncludeSourceFilesInFileHeader, 
                    Logger = new MSBuildLogger(this.BuildEngine, nameof(XAMLCombineTask))
                };

                try
                {
                    targetFile = MutexHelper.ExecuteLocked(() => combiner.Combine(sourceFiles, targetFile), targetFile);
                }
                catch (Exception)
                {
                    return false;
                }

                generatedFiles.Add(new TaskItem(targetFile));
            }

            this.GeneratedFiles = generatedFiles.ToArray();

            return true;
        }
    }
}
