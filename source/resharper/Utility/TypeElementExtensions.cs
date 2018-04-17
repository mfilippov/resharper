// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.ReSharper.Psi;

namespace ReSharper.Nuke.Utility
{
    /// <summary>
    /// Extension methods for <see cref="ITypeElement"/> and derived types.
    /// </summary>
    public static class TypeElementExtensions
    {
        /// <summary>
        /// Checks if the given type element is a Nuke build class definition.
        /// </summary>
        /// <param name="typeElement">The type element to test.</param>
        /// <returns>True if the type element is a Nuke build class definition.</returns>
        public static bool IsNukeBuildClass(this ITypeElement typeElement)
        {
            var nukeBuildTypeElement = TypeFactory.CreateTypeByCLRName("Nuke.Common.NukeBuild", typeElement.Module).GetTypeElement();
            return typeElement.IsDescendantOf(nukeBuildTypeElement);
        }
    }
}