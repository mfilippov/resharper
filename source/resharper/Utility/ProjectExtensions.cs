// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.Util.Reflection;

namespace ReSharper.Nuke.Utility
{
    /// <summary>
    /// Extension methods for <see cref="IProject"/>.
    /// </summary>
    public static class ProjectExtensions
    {
        private static readonly AssemblyNameInfo _NukeAssemblyNameInfo = AssemblyNameInfoFactory.Create2("Nuke.Common", version: null);

        /// <summary>
        /// Checks if the given project is a Nuke build project.
        /// </summary>
        /// <param name="project">The project to test.</param>
        /// <returns>True if the project is a Nuke build project.</returns>
        public static bool IsNukeProject([CanBeNull] this IProject project)
        {
            if (project == null) return false;
            var targetFrameworkId = project.GetCurrentTargetFrameworkId();

            if (!ReferencedAssembliesService.IsProjectReferencingAssemblyByName(project,
                targetFrameworkId,
                _NukeAssemblyNameInfo,
                out var _)) return false;

            var buildClass = project.GetAllProjectFiles(p => p.Name == "Build.cs").FirstOrDefault();
            return buildClass != null;
        }
    }
}