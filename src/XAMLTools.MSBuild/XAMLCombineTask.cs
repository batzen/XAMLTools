namespace XAMLTools.MSBuild
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using XAMLTools.XAMLCombine;

    public class XAMLCombineTask : Task
    {
        [Required]
        public string SourceFile { set; get; } = null!;

        [Required]
        public string TargetFile { set; get; } = null!;

        public override bool Execute()
        {
            var combiner = new XAMLCombiner();

            combiner.Combine(this.SourceFile, this.TargetFile);

            return true;
        }
    }
}