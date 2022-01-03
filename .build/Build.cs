using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

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

        Console.WriteLine("IsLocalBuild           : {0}", IsLocalBuild);
        Console.WriteLine("Informational   Version: {0}", GitVersion.InformationalVersion);
        Console.WriteLine("SemVer          Version: {0}", GitVersion.SemVer);
        Console.WriteLine("AssemblySemVer  Version: {0}", GitVersion.AssemblySemVer);
        Console.WriteLine("MajorMinorPatch Version: {0}", GitVersion.MajorMinorPatch);
        Console.WriteLine("NuGet           Version: {0}", GitVersion.NuGetVersion);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [GitRepository] readonly GitRepository GitRepository;
    
    [GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;

    string AssemblySemVer => GitVersion?.AssemblySemVer ?? "1.0.0";
    string SemVer => GitVersion?.SemVer ?? "1.0.0";
    string InformationalVersion => GitVersion?.InformationalVersion ?? "1.0.0";
    string NuGetVersion => GitVersion?.NuGetVersion ?? "1.0.0";
    string MajorMinorPatch => GitVersion?.MajorMinorPatch ?? "1.0.0";
    string AssemblySemFileVer => GitVersion?.AssemblySemFileVer ?? "1.0.0";

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
        });

    Target CI => _ => _
        .DependsOn(Pack);
}
