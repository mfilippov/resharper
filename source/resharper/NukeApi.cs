// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.Util;
using EnvDTE;

namespace ReSharper.Nuke
{
    public static class NukeApi
    {
        public static void CreateNukeArgumentsFile(string argumentsFilePath, bool skipDependencies, [CanBeNull] string targetName)
        {
            File.WriteAllText(argumentsFilePath, $"{targetName}{(skipDependencies ? " -Skip" : string.Empty)}");
        }

        public static FileSystemPath GetNukeArgumentsFilePath(IProject buildProject)
        {
            return buildProject.GetOutputDirectory(buildProject.GetCurrentTargetFrameworkId()) / "nuke.tmp";
        }

        public static Lifetime StartNukeTempFileWatcher(FileSystemPath tempFilePath, Lifetime parentLifetime = null)
        {
            var lifetime = parentLifetime == null
                ? Lifetimes.Define(nameof(StartNukeTempFileWatcher))
                : Lifetimes.Define(parentLifetime, nameof(StartNukeTempFileWatcher));
            var watcher = new FileSystemWatcher(tempFilePath.Directory.FullPath, tempFilePath.Name) { EnableRaisingEvents = true };
            watcher.Deleted += DeletedEventHandler;

            lifetime.Lifetime.AddAction(() =>
            {
                watcher.Deleted -= DeletedEventHandler;
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            });
            return lifetime.Lifetime;

            void DeletedEventHandler(object sender, FileSystemEventArgs args)
            {
                lifetime.Terminate();
            }
        }

        public static void RestoreStartupProjectAfterExecution(Lifetime lifetime, _DTE dte)
        {
            var startupObjects = dte.Solution.SolutionBuild.StartupProjects;
            lifetime.AddAction(() =>
            {
                if (startupObjects is object[] startupObjectsArray && startupObjectsArray.Length == 1)
                {
                    startupObjects = startupObjectsArray[0];
                }

                dte.Solution.SolutionBuild.StartupProjects = startupObjects;
            });
        }

        public static void TemporaryEnableNukeProjectBuild(Lifetime lifetime, IProject buildProject, ISolution solution, _DTE dte)
        {
            lifetime.AddBracket(() => SetEnableBuild(shouldBuild: true, project: buildProject, solution: solution, dte: dte),
                () =>
                {
                    SetEnableBuild(shouldBuild: false, project: buildProject, solution: solution, dte: dte);
                    dte.Solution.SaveAs(solution.SolutionFilePath.FullPath);
                });
        }

        private static void SetEnableBuild(bool shouldBuild, IProject project, ISolution solution, _DTE dte)
        {
            var name = project.ProjectFileLocation.FullPath.Substring(solution.SolutionFilePath.Directory.FullPath.Length + 1);

            var activeConfiguration = dte.Solution.SolutionBuild.ActiveConfiguration;

            foreach (SolutionContext solutionContext in activeConfiguration.SolutionContexts)
            {
                if (solutionContext.ProjectName != name) continue;
                solutionContext.ShouldBuild = shouldBuild;
                break;
            }
        }
    }
}
