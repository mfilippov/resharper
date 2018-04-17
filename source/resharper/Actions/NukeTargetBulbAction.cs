// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.IDE.RunConfig;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.UnitTestProvider.JavaScript;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER
using JetBrains.VsIntegration.IDE.RunConfig;
using JetBrains.VsIntegration.Interop;
#endif
#if RIDER
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;
#endif

namespace ReSharper.Nuke.Actions
{
    /// <summary>
    /// Action to execute different targets.
    /// </summary>
    public abstract class NukeTargetBulbAction : BulbActionBase
    {
        /// <summary>
        /// The name of the target to execute. If null or empty the default target will be executed.
        /// </summary>
        [CanBeNull] public readonly string TargetName;

        /// <summary>
        /// Should a debugger gets attached to the process.
        /// </summary>
        public readonly bool Debug;

        /// <summary>
        /// Should the run of the dependencies be skipped.
        /// </summary>
        public readonly bool SkipDependencies;

        /// <summary>
        /// Action to debug the specified target.
        /// </summary>
        /// <param name="targetName">The name of the target to execute. If null or empty the default target will be executed.</param>
        /// <param name="debug">Should a debugger gets attached to the process.</param>
        /// <param name="skipDependencies">Should the run of the dependencies be skipped.</param>
        protected NukeTargetBulbAction([CanBeNull] string targetName, bool debug, bool skipDependencies)
        {
            TargetName = targetName;
            Debug = debug;
            SkipDependencies = skipDependencies;
        }

        [CanBeNull]
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            return textControl =>
            {
                var buildProject = textControl.Document.GetPsiSourceFile(solution)?.GetProject();
                if (buildProject == null) return;

#if RESHARPER
                var runConfig = CreateRunConfig(solution, buildProject);
                var runConfigBuilder = solution.GetComponent<IRunConfigBuilder>();
                var dte = solution.GetComponent<VsDtePropertiesFacade>().DTE;

                var nukeArgumentsFile = NukeApi.GetNukeArgumentsFilePath(buildProject);

                NukeApi.CreateNukeArgumentsFile(nukeArgumentsFile.FullPath, SkipDependencies, TargetName);
                var tempFileLifetime = NukeApi.StartNukeTempFileWatcher(nukeArgumentsFile, textControl.Lifetime);

                NukeApi.RestoreStartupProjectAfterExecution(tempFileLifetime, dte);
                NukeApi.TemporaryEnableNukeProjectBuild(tempFileLifetime, buildProject, solution, dte);

                var executionProvider = solution.GetComponent<ExecutionProviders>().GetByIdOrDebug(runConfig.DefaultAction);
                var context = new RunConfigContext
                              {
                                  Solution = solution,
                                  ExecutionProvider = executionProvider
                              };

                runConfigBuilder.BuildRunConfigAndExecute(solution, context, runConfig);
#else
                var nukeModel = buildProject.GetSolution().GetProtocolSolution().GetRdNukeModel();
                nukeModel.Build.Fire(new BuildInvocation(buildProject.ProjectFileLocation.FullPath, TargetName.NotNull(), Debug, SkipDependencies));
#endif
            };
        }

        /// <summary>
        /// The text of the bulb action.
        /// </summary>
        public override string Text =>
            $"{(Debug ? "Debug" : "Run")} {(SkipDependencies ? "single " : string.Empty)}target";

#if RESHARPER
        private IRunConfig CreateRunConfig(ISolution solution, IProject buildProject)
        {
            var runConfig = solution.GetComponent<RunConfigProjectProvider>().CreateNew();
            runConfig.ProjectGuid = buildProject.Guid;
            runConfig.DefaultAction = Debug ? "debug" : "run";
            return runConfig;
        }
#endif
    }
}