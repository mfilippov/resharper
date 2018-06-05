// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Tools.Nunit;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.Core.Tooling;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
using static Nuke.Core.Tooling.NuGetPackageResolver;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.ChangeLog.ChangelogTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.Tools.Nunit.NunitTasks;
using static Nuke.Core.Tooling.ProcessTasks;

class Build : NukeBuild
{
    // Console application entry. Also defines the default target.
    public static int Main() => Execute<Build>(x => x.Pack);

    [Parameter] readonly string Source = "https://resharper-plugins.jetbrains.com/api/v2/package";
    [Parameter] readonly string ApiKey;
    [Parameter] readonly string Username;
    [Parameter] readonly string Password;

    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;
    [Solution] readonly Solution Solution;

    string ProjectFile => Solution.GetProject("ReSharper.Nuke").NotNull();

    Target Clean => _ => _
        .Executes(() =>
        {
            DeleteDirectories(GlobDirectories(SourceDirectory, "**/bin", "**/obj"));
            DeleteDirectory(RiderDirectory / "build");
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
            MSBuild(s => DefaultMSBuildCompile
                .SetMaxCpuCount(maxCpuCount: 1));
            
            GradleTask("buildPlugin");
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Nunit3(s => s
                .AddInputFiles(GlobFiles(RootDirectory / "tests", $"**/bin/{Configuration}/*.Tests.dll"))
                .AddResults(OutputDirectory / "test-result.xml"));
        });

    string ChangelogFile => RootDirectory / "CHANGELOG.md";

    IEnumerable<string> ChangelogSectionNotes => ExtractChangelogSectionNotes(ChangelogFile);

    Target Changelog => _ => _
        .OnlyWhen(() => InvokedTargets.Contains(nameof(Changelog)))
        .Executes(() =>
        {
            FinalizeChangelog(ChangelogFile, GitVersion.SemVer, GitRepository);

            Git($"add {ChangelogFile}");
            Git($"commit -m \"Finalize {Path.GetFileName(ChangelogFile)} for {GitVersion.SemVer}.\"");
            Git($"tag -f {GitVersion.SemVer}");
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var releaseNotes = ChangelogSectionNotes
                .Select(x => x.Replace("- ", "\u2022 ").Replace("`", string.Empty).Replace(",", "%2C"))
                .Concat(string.Empty)
                .Concat($"Full changelog at {GitRepository.GetGitHubBrowseUrl(ChangelogFile)}")
                .JoinNewLine();

            GlobFiles(SourceDirectory, "*.nuspec")
                .ForEach(x => NuGetPack(s => DefaultNuGetPack
                    .SetTargetPath(x)
                    .SetBasePath(Path.GetDirectoryName(x))
                    .SetProperty("wave", GetWaveVersion(ProjectFile) + ".0")
                    .SetProperty("currentyear", DateTime.Now.Year.ToString())
                    .SetProperty("releaseNotes", releaseNotes)
                    .EnableNoPackageAnalysis()));

            GlobFiles(RiderDirectory / "build" / "distributions", "*.zip")
                .ForEach(x => File.Copy(x, OutputDirectory / Path.GetFileName(x)));
        });

    Target Push => _ => _
        .DependsOn(Pack, Test, Changelog)
        .Requires(() => ApiKey)
        .Requires(() => Configuration.EqualsOrdinalIgnoreCase("Release"))
        .Executes(() =>
        {
            GlobFiles(OutputDirectory, "*.nupkg")
                .ForEach(x => NuGetPush(s => s
                    .SetTargetPath(x)
                    .SetSource(Source)
                    .SetApiKey(ApiKey)));
            
            GradleTask("publishPlugin");
        });

    static string GetWaveVersion(string packagesConfigFile)
    {
        var fullWaveVersion = GetLocalInstalledPackages(packagesConfigFile, includeDependencies: true)
            .SingleOrDefault(x => x.Id == "Wave").NotNull("fullWaveVersion != null").Version.ToString();
        return fullWaveVersion.Substring(startIndex: 0, length: fullWaveVersion.IndexOf(value: '.'));
    }

    AbsolutePath RiderDirectory => SourceDirectory / "rider";

    void GradleTask(string task)
    {
        var arguments = $":{task}";
        arguments += $" -PpluginVersion={GitVersion.NuGetVersionV2} -PBuildConfiguration={Configuration}";
        if (Username != null || Password != null)
            arguments += $" -Pusername={Username} -Ppassword={Password}";
        StartProcess(
                SourceDirectory / "rider" / "gradlew.bat",
                arguments,
                workingDirectory: RiderDirectory)
            .AssertZeroExitCode();
    }
}