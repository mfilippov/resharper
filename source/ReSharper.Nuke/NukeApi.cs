// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/ide-extensions/blob/master/LICENSE

using System;
using System.IO;
using System.Linq;
using EnvDTE;
using JetBrains.DataFlow;
using JetBrains.IDE.RunConfig;
using JetBrains.IDE.SolutionBuilders.Prototype.Services.Interaction;
using JetBrains.ProjectModel;
using JetBrains.Util;
using JetBrains.VsIntegration.IDE.RunConfig;
using JetBrains.VsIntegration.Interop;
using ReSharper.Nuke.Utility;

namespace ReSharper.Nuke
{
    [SolutionComponent]
    public class NukeApi
    {
        private readonly IRunConfigBuilder _runConfigBuilder;
        private readonly RunConfigProjectProvider _runConfigProjectProvider;
        private readonly VsDtePropertiesFacade _vsDtePropertiesFacade;
        private readonly IExecutionProviderFactory _executionProviderFactory;
        private readonly Lifetime _lifetime;

        public NukeApi(
            IRunConfigBuilder runConfigBuilder,
            IExecutionProviderFactory executionProviderFactory,
            RunConfigProjectProvider runConfigProjectProvider,
            VsDtePropertiesFacade vsDtePropertiesFacade,
            Lifetime lifetime)
        {
            _runConfigBuilder = runConfigBuilder;
            _executionProviderFactory = executionProviderFactory;
            _runConfigProjectProvider = runConfigProjectProvider;
            _vsDtePropertiesFacade = vsDtePropertiesFacade;
            _lifetime = lifetime;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="solution"></param>
        /// <param name="debug"></param>
        /// <param name="skipDependencies"></param>
        public void ExecuteTarget(string targetName, ISolution solution, bool debug, bool skipDependencies)
        {
            var buildProject = solution.GetAllProjects().First(x => x.IsNukeProject());
            var runConfig = CreateNukeTargetRunConfig(buildProject);
            var runContext = CreateRunConfigContext(solution, debug);

            var argumentsFilePath = GetNukeArgumentsFilePath(buildProject);
            CreateNukeArgumentsFile(argumentsFilePath.FullPath, skipDependencies, targetName);

            var nukeFileWatcherLifetime = StartNukeTempFileWatcher(argumentsFilePath);

            RestoreStartupProjectAfterExecution(nukeFileWatcherLifetime);
            TemporaryEnableNukeProjectBuild(nukeFileWatcherLifetime,buildProject,solution);
          
            _runConfigBuilder.BuildRunConfigAndExecute(solution, runContext, runConfig);
        }

        private void CreateNukeArgumentsFile(string argumentsFilePath, bool skipDependencies, string targetName)
        {
            File.WriteAllText(argumentsFilePath, $"{targetName}{(skipDependencies ? " -Skip" : string.Empty)}");
        }

        private FileSystemPath GetNukeArgumentsFilePath(IProject buildProject)
        {
            return (buildProject.GetOutputDirectory(buildProject.GetCurrentTargetFrameworkId()) / "nuke.tmp");
        }

        private IRunConfig CreateNukeTargetRunConfig(IProject buildProject)
        {
            var runConfig = _runConfigProjectProvider.CreateNew();
            runConfig.ProjectGuid = buildProject.Guid;
            return runConfig;
        }

        private RunConfigContext CreateRunConfigContext(ISolution solution, bool debug)
        {
            return new RunConfigContext
                   {
                       ExecutionProvider =
                           debug ? _executionProviderFactory.GetDebugExecutionProvider() : _executionProviderFactory.GetRunExecutionProvider(),
                       Solution = solution
                   };
        }

        private Lifetime StartNukeTempFileWatcher(FileSystemPath tempFilePath)
        {
            var lifetime = Lifetimes.Define(_lifetime, nameof(StartNukeTempFileWatcher));
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

        private void SetEnableBuild(bool shouldBuild, IProject project, ISolution solution)
        {
            var name = project.ProjectFileLocation.FullPath.Substring(solution.SolutionFilePath.Directory.FullPath.Length + 1);

            var activeConfiguration = _vsDtePropertiesFacade.DTE.Solution.SolutionBuild.ActiveConfiguration;

            foreach (SolutionContext solutionContext in activeConfiguration.SolutionContexts)
            {
                if (solutionContext.ProjectName != name) continue;
                solutionContext.ShouldBuild = shouldBuild;
                break;
            }
        }

        private void RestoreStartupProjectAfterExecution(Lifetime lifetime)
        {
            var startupObjects = _vsDtePropertiesFacade.DTE.Solution.SolutionBuild.StartupProjects;
            lifetime.AddAction(() =>
            {
                if (startupObjects is object[] startupObjectsArray && startupObjectsArray.Length == 1)
                {
                    startupObjects = startupObjectsArray[0];
                }
                _vsDtePropertiesFacade.DTE.Solution.SolutionBuild.StartupProjects = startupObjects;
            });
        }

        private void TemporaryEnableNukeProjectBuild(Lifetime lifetime, IProject buildProject, ISolution solution)
        {
            lifetime.AddBracket(() => SetEnableBuild(true, buildProject, solution),
                () =>
                {
                    SetEnableBuild(false, buildProject, solution);
                    _vsDtePropertiesFacade.DTE.Solution.SaveAs(solution.SolutionFilePath.FullPath);
                });
        }
    }
}