namespace XAMLTools.MSBuild
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using XAMLTools.XAMLCombine;

    public class XAMLCombineTask : Task
    {
        [Required]
        public ITaskItem[] Items { get; set; } = null!;

        [Output]
        public ITaskItem[]? WrittenFiles { get; set; }

        public override bool Execute()
        {
            //System.Diagnostics.Debugger.Launch();

            var writtenFiles = new List<ITaskItem>();

            var grouped = this.Items.GroupBy(x => x.GetMetadata("TargetFile"));

            foreach (var item in grouped)
            {
                var sourceFiles = item.Select(x => x.ItemSpec);
                var targetFile = item.Key;

                var combiner = new XAMLCombiner();

                combiner.Combine(sourceFiles, targetFile);

                writtenFiles.Add(new TaskItem(targetFile));
            }

            this.WrittenFiles = writtenFiles.ToArray();

            return true;
        }
    }
}