using System;
using System.IO;
using System.Linq;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NuGet;
using Nuke.Core;
using Nuke.Core.Utilities.Collections;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
using static Nuke.Core.Tooling.NuGetPackageResolver;
using static Nuke.Core.IO.FileSystemTasks;
using static Nuke.Core.IO.PathConstruction;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Pack);

    [Parameter] readonly string Source = "https://resharper-plugins.jetbrains.com/api/v2/package";
    [Parameter] readonly string ApiKey;

    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    string PackagesConfigFile => GlobFiles(SolutionDirectory, "**/packages.config").Single();


    Target Clean => _ => _
        .Executes(() =>
        {
            DeleteDirectories(GlobDirectories(SourceDirectory, "**/bin", "**/obj"));
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            MSBuild(s => DefaultMSBuildRestore);
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            MSBuild(s => DefaultMSBuildCompile);
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            GlobFiles(SourceDirectory, "**/*.nuspec").ForEach(x => NuGetPack(s => DefaultNuGetPack
                .SetTargetPath(x)
                .SetBasePath(Path.GetDirectoryName(x))
                .SetProperty("wave", GetWaveVersion(PackagesConfigFile) + ".0")
                .SetProperty("currentyear", DateTime.Now.Year.ToString())
                .EnableNoPackageAnalysis()));
        });

    static string GetWaveVersion(string packagesConfigFile)
    {
        var fullWaveVersion = GetLocalInstalledPackages(packagesConfigFile, includeDependencies: true)
            .SingleOrDefault(x => x.Id == "Wave").NotNull("fullWaveVersion != null").Version.ToString();
        return fullWaveVersion.Substring(startIndex: 0, length: fullWaveVersion.IndexOf(value: '.'));
    }
}
