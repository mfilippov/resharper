// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.ReSharper.Psi;

namespace ReSharper.Nuke.Utility
{
    /// <summary>
    /// Extension methods for <see cref="IProperty"/> and derived types.
    /// </summary>
    public static class PropertyExtensions
    {
        /// <summary>
        /// Checks if the given property is a Nuke build target definition.
        /// </summary>
        /// <param name="property">The property to test.</param>
        /// <returns>True if the property is a Nuke build target definition.</returns>
        public static bool IsNukeBuildTarget(this IProperty property)
        {
            var nukeBuildTargetType = TypeFactory.CreateTypeByCLRName("Nuke.Common.Target", property.Module);
            return property.Type.Equals(nukeBuildTargetType);
        }

        /// <summary>
        /// Checks if the given property is a Nuke build parameter definition.
        /// </summary>
        /// <param name="property">The property to test.</param>
        /// <returns>True if the property is a Nuke build parameter definition.</returns>
        public static bool IsNukeBuildParameter(this IProperty property)
        {
            var nukeParameterAttribute = TypeFactory.CreateTypeByCLRName("Nuke.Common.ParameterAttribute", property.Module);
            return property.HasAttributeInstance(nukeParameterAttribute.GetClrName(), inherit: true);
        }
    }
}