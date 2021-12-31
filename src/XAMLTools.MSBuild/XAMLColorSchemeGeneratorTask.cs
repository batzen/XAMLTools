namespace XAMLTools.MSBuild
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using XAMLTools.XAMLColorSchemeGenerator;
    using XAMLTools.XAMLCombine;

    public class XAMLColorSchemeGeneratorTask : Task
    {
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

            foreach (var item in this.Items)
            {
                var templateFile = item.ItemSpec;
                var generatorParametersFile = item.GetMetadata("ParametersFile");
                var outputPath = item.GetMetadata("OutputPath");

                var generator = new ColorSchemeGenerator();
                var generatedFiles = generator.GenerateColorSchemeFiles(generatorParametersFile, templateFile, outputPath);

                foreach (var generatedFile in generatedFiles)
                {
                    writtenFiles.Add(new TaskItem(generatedFile.Path));

                    if (generatedFile.IsNew)
                    {
                        newFiles.Add(new TaskItem(generatedFile.Path));
                    }
                }
            }

            this.WrittenFiles = writtenFiles.ToArray();
            this.NewFiles = newFiles.ToArray();

            return true;
        }
    }
}