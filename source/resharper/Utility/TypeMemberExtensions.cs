// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.ReSharper.Psi;

namespace ReSharper.Nuke.Utility
{
    /// <summary>
    /// Extension methods for <see cref="ITypeMember"/> and derived types.
    /// </summary>
    public static class TypeMemberExtensions
    {
        /// <summary>
        /// Checks if the given property is a Nuke build target definition.
        /// </summary>
        /// <param name="typeMember">The type member to test.</param>
        /// <returns>True if the type member is a Nuke build target definition.</returns>
        public static bool IsNukeBuildTarget(this ITypeMember typeMember)
        {
            if (!(typeMember is IProperty property)) return false;
            return property.IsNukeBuildTarget();
        }

        /// <summary>
        /// Checks if the given property is a Nuke build parameter definition.
        /// </summary>
        /// <param name="typeMember">The type member to test.</param>
        /// <returns>True if the type member is a Nuke build parameter definition.</returns>
        public static bool IsNukeBuildParameter(this ITypeMember typeMember)
        {
            if (!(typeMember is IProperty property)) return false;
            return property.IsNukeBuildParameter();
        }
    }
}