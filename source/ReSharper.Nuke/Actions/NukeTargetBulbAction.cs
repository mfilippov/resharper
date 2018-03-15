// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/ide-extensions/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.TextControl;

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

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            solution.GetComponent<NukeApi>().ExecuteTarget(TargetName, solution, Debug, SkipDependencies);
            return null;
        }

        /// <summary>
        /// The text of the bulb action.
        /// </summary>
        public override string Text =>
            $"{(Debug ? "Debug" : "Run")} {(SkipDependencies ? "Single " : string.Empty)}Target";
    }
}