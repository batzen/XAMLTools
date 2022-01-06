namespace XAMLTools.MSBuild
{
    using System.Collections.Generic;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using XAMLTools.Helpers;
    using XAMLTools.XAMLColorSchemeGenerator;

    public class XAMLColorSchemeGeneratorTask : Task
    {
        public const string ParametersFileMetadataName = "ParametersFile";

        public const string OutputPathMetadataName = "OutputPath";

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
                var generatorParametersFile = item.GetMetadata(ParametersFileMetadataName);
                var outputPath = item.GetMetadata(OutputPathMetadataName);

                this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Generating XAML files from \"{templateFile}\" with \"{generatorParametersFile}\" to \"{outputPath}\".", string.Empty, nameof(XAMLColorSchemeGeneratorTask), MessageImportance.High));

                var generator = new ColorSchemeGenerator();
                var generatedFiles = MutexHelper.ExecuteLocked(() => generator.GenerateColorSchemeFiles(generatorParametersFile, templateFile, outputPath), templateFile);

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