using System;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

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

        Serilog.Log.Information("IsLocalBuild      : {0}", IsLocalBuild.ToString());

        Serilog.Log.Information("Informational     : {0}", InformationalVersion);
        Serilog.Log.Information("SemVer            : {0}", SemVer);
        Serilog.Log.Information("FullSemVer        : {0}", FullSemVer);
        Serilog.Log.Information("AssemblySemVer    : {0}", AssemblySemVer);
        Serilog.Log.Information("AssemblySemFileVer: {0}", AssemblySemFileVer);
        Serilog.Log.Information("MajorMinorPatch   : {0}", MajorMinorPatch);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;

    string InformationalVersion => GitVersion?.InformationalVersion ?? "1.0.0";
    string SemVer => GitVersion?.SemVer ?? "1.0.0";
    string FullSemVer => GitVersion?.FullSemVer ?? "1.0.0";
    string AssemblySemVer => GitVersion?.AssemblySemVer ?? "1.0.0";
    string AssemblySemFileVer => GitVersion?.AssemblySemFileVer ?? "1.0.0";
    string MajorMinorPatch => GitVersion?.MajorMinorPatch ?? "1.0.0";

    AbsolutePath BuildBinDirectory => RootDirectory / "bin";

    AbsolutePath SourceDirectory => RootDirectory / "src";

    [Parameter]
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    AbsolutePath TestResultsDir => RootDirectory / "TestResults";

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                             .SetProjectFile(Solution)
                             .SetConfiguration("Debug")
                             .SetAssemblyVersion(AssemblySemVer)
                             .SetFileVersion(AssemblySemVer)
                             .SetInformationalVersion(InformationalVersion));

            DotNetBuild(s => s
                             .SetProjectFile(Solution)
                             .SetConfiguration("Release")
                             .SetAssemblyVersion(AssemblySemVer)
                             .SetFileVersion(AssemblySemVer)
                             .SetInformationalVersion(InformationalVersion));
        });

    Target Test => _ => _
            .After(Compile)
            .Before(Pack)
            .Executes(() =>
        {
            TestResultsDir.CreateOrCleanDirectory();

            DotNetTest(_ => _
                .SetConfiguration(Configuration)
                .SetProjectFile(SourceDirectory / "tests" / "XAMLTools.Tests")
                .SetProcessWorkingDirectory(SourceDirectory / "tests" / "XAMLTools.Tests")
                .EnableNoBuild()
                .EnableNoRestore()
                .AddLoggers("trx")
                .SetResultsDirectory(TestResultsDir)
                .SetVerbosity(DotNetVerbosity.normal));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Produces(ArtifactsDirectory / "*.nupkg", ArtifactsDirectory / "*.zip")
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();

            DotNetPack(s => s
                .SetProject(SourceDirectory / "XAMLTools.MSBuild")
                .SetConfiguration(Configuration)

                .When(_ => GitVersion is not null, x => x
                                                   .SetProperty("RepositoryBranch", GitVersion?.BranchName)
                                                   .SetProperty("RepositoryCommit", GitVersion?.Sha))
                .SetVersion(FullSemVer)
                .SetAssemblyVersion(AssemblySemVer)
                .SetFileVersion(AssemblySemFileVer)
                .SetInformationalVersion(InformationalVersion));

            (BuildBinDirectory / Configuration / "XAMLTools").CompressTo(ArtifactsDirectory / $"XAMLTools-v{FullSemVer}.zip");

            DotNetPack(s => s
                            .SetProject(SourceDirectory / "XAMLTools")
                            .SetConfiguration(Configuration)

                            .When(_ => GitVersion is not null, x => x
                                                               .SetProperty("RepositoryBranch", GitVersion?.BranchName)
                                                               .SetProperty("RepositoryCommit", GitVersion?.Sha))
                            .SetVersion(FullSemVer)
                            .SetAssemblyVersion(AssemblySemVer)
                            .SetFileVersion(AssemblySemFileVer)
                            .SetInformationalVersion(InformationalVersion));
        });

    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once InconsistentNaming
    Target CI => _ => _
        .DependsOn(Compile, Test, Pack);
}
