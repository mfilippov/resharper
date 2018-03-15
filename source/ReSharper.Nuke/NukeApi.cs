// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/ide-extensions/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.IDE.RunConfig;
using JetBrains.IDE.SolutionBuilders.Prototype.Services.Interaction;
using JetBrains.ProjectModel;
using JetBrains.Util;
using JetBrains.VsIntegration.IDE.RunConfig;
using ReSharper.Nuke.Utility;

namespace ReSharper.Nuke
{
    [SolutionComponent]
    public class NukeApi
    {
        private readonly IRunConfigBuilder _runConfigBuilder;
        private readonly RunConfigProjectProvider _runConfigProjectProvider;
        private readonly IExecutionProviderFactory _executionProviderFactory;

        public NukeApi(
            IRunConfigBuilder runConfigBuilder,
            IExecutionProviderFactory executionProviderFactory,
            RunConfigProjectProvider runConfigProjectProvider)
        {
            _runConfigBuilder = runConfigBuilder;
            _executionProviderFactory = executionProviderFactory;
            _runConfigProjectProvider = runConfigProjectProvider;
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

            CreateNukeArgumentsFile(solution, skipDependencies, targetName);
            _runConfigBuilder.ExecuteRunConfig(runConfig, runContext, isBuildRequired: true);
        }

        private void CreateNukeArgumentsFile(ISolution solution, bool skipDependencies, string targetName)
        {
            var buildProject = solution.GetAllProjects().First(p => p.IsNukeProject());
            var argumentsFilePath = buildProject.GetOutputAssemblyInfo(buildProject.GetCurrentTargetFrameworkId())?.Location?.Directory;
            if (argumentsFilePath == null) return;
            argumentsFilePath = argumentsFilePath / "nuke.tmp";
            argumentsFilePath.WriteAllText($"{targetName}{(skipDependencies ? " -Skip" : string.Empty)}");
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
    }
}