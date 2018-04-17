// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;

namespace ReSharper.Nuke.Actions
{
    /// <summary>
    /// Action to run the specified target.
    /// </summary>
    public sealed class RunNukeTargetBulbAction : NukeTargetBulbAction
    {
        /// <summary>
        /// Action to run the specified target.
        /// </summary>
        /// <param name="targetName">The name of the target to execute. If null or empty the default target will be executed.</param>
        public RunNukeTargetBulbAction([CanBeNull] string targetName)
            : base(targetName, debug: false, skipDependencies: false)
        {
        }
    }
}