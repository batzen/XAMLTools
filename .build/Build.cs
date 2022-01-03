using System;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;

using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.FileSystemTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();

        ProcessTasks.DefaultLogInvocation = true;
        ProcessTasks.DefaultLogOutput = true;

        if (GitVersion is null
            && IsLocalBuild == false)
        {
            throw new Exception("Could not initialize GitVersion.");
        }

        Console.WriteLine("IsLocalBuild           : {0}", IsLocalBuild.ToString());

        Console.WriteLine("Informational   Version: {0}", InformationalVersion);
        Console.WriteLine("SemVer          Version: {0}", SemVer);
        Console.WriteLine("AssemblySemVer  Version: {0}", AssemblySemVer);
        Console.WriteLine("MajorMinorPatch Version: {0}", MajorMinorPatch);
        Console.WriteLine("NuGet           Version: {0}", NuGetVersion);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;

    string AssemblySemVer => GitVersion?.AssemblySemVer ?? "1.0.0";
    string SemVer => GitVersion?.SemVer ?? "1.0.0";
    string InformationalVersion => GitVersion?.InformationalVersion ?? "1.0.0";
    string NuGetVersion => GitVersion?.NuGetVersion ?? "1.0.0";
    string MajorMinorPatch => GitVersion?.MajorMinorPatch ?? "1.0.0";
    string AssemblySemFileVer => GitVersion?.AssemblySemFileVer ?? "1.0.0";

    AbsolutePath BuildBinDirectory => RootDirectory / "bin";

    AbsolutePath SourceDirectory => RootDirectory / "src";

    [Parameter]
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)

                .SetAssemblyVersion(AssemblySemVer)
                .SetFileVersion(AssemblySemVer)
                .SetInformationalVersion(InformationalVersion)

                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Produces(ArtifactsDirectory / "*.nupkg", ArtifactsDirectory / "*.zip")
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);

            DotNetPack(s => s
                .SetProject(SourceDirectory / "XAMLTools.MSBuild")
                .SetConfiguration(Configuration)

                .When(GitVersion is not null, x => x
                                                   .SetProperty("RepositoryBranch", GitVersion?.BranchName)
                                                   .SetProperty("RepositoryCommit", GitVersion?.Sha))
                .SetVersion(NuGetVersion)
                .SetAssemblyVersion(AssemblySemVer)
                .SetFileVersion(AssemblySemFileVer)
                .SetInformationalVersion(InformationalVersion));

            Compress(BuildBinDirectory / Configuration / "XAMLTools", ArtifactsDirectory / $"XAMLTools-v{NuGetVersion}.zip");
        });

    Target CI => _ => _
        .DependsOn(Pack);
}
