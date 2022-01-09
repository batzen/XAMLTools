namespace XAMLTools.MSBuild
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using XAMLTools.Helpers;
    using XAMLTools.XAMLCombine;

    public class XAMLCombineTask : Task
    {
        public const string TargetFileMetadataName = "TargetFile";

        [Required]
        public ITaskItem[] Items { get; set; } = null!;

        [Output]
        public ITaskItem[]? GeneratedFiles { get; set; }

        public override bool Execute()
        {
            //System.Diagnostics.Debugger.Launch();

            var generatedFiles = new List<ITaskItem>();

            var grouped = this.Items.GroupBy(x => x.GetMetadata(TargetFileMetadataName));

            foreach (var item in grouped)
            {
                var sourceFiles = item.Select(x => x.ItemSpec);
                var targetFile = item.Key;

                this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Generating combined XAML file \"{targetFile}\".", string.Empty, nameof(XAMLCombineTask), MessageImportance.High));

                var combiner = new XAMLCombiner();
                targetFile = MutexHelper.ExecuteLocked(() => combiner.Combine(sourceFiles, targetFile), targetFile);

                generatedFiles.Add(new TaskItem(targetFile));
            }

            this.GeneratedFiles = generatedFiles.ToArray();

            return true;
        }
    }
}