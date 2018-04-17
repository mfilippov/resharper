// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;

namespace ReSharper.Nuke.Actions
{
    /// <summary>
    /// Action to debug the specified target without executing its dependencies.
    /// </summary>
    public sealed class DebugSingleNukeTargetBulbAction : NukeTargetBulbAction
    {
        /// <summary>
        /// Action to debug the specified target without executing its dependencies.
        /// </summary>
        /// <param name="targetName">The name of the target to debug. If null or empty the default target will be executed.</param>
        public DebugSingleNukeTargetBulbAction([CanBeNull] string targetName)
            : base(targetName, debug: true, skipDependencies: true)
        {
        }
    }
}