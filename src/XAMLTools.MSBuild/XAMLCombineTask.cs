namespace XAMLTools.MSBuild
{
    using System.Collections.Generic;
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
        public ITaskItem[]? WrittenFiles { get; set; }

        [Output]
        public ITaskItem[]? NewFiles { get; set; }

        public override bool Execute()
        {
            //System.Diagnostics.Debugger.Launch();

            var writtenFiles = new List<ITaskItem>();
            var newFiles = new List<ITaskItem>();

            var grouped = this.Items.GroupBy(x => x.GetMetadata(TargetFileMetadataName));

            foreach (var item in grouped)
            {
                var sourceFiles = item.Select(x => x.ItemSpec);
                var targetFile = item.Key;

                var targetFileIsNew = System.IO.File.Exists(targetFile) == false;

                this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Generating combined XAML file \"{targetFile}\".", string.Empty, nameof(XAMLCombineTask), MessageImportance.High));

                var combiner = new XAMLCombiner();
                MutexHelper.ExecuteLocked(() => combiner.Combine(sourceFiles, targetFile), targetFile);

                writtenFiles.Add(new TaskItem(targetFile));

                if (targetFileIsNew)
                {
                    newFiles.Add(new TaskItem(targetFile));
                }
            }

            this.WrittenFiles = writtenFiles.ToArray();
            this.NewFiles = newFiles.ToArray();

            return true;
        }
    }
}