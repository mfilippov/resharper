// Copyright Matthias Koch, Sebastian Karasek 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/ide-extensions/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;
[assembly: RequiresSTA]

namespace ReSharper.Nuke.Tests
{
    [ZoneDefinition]
    public interface INukeTestZone : ITestsZone, IRequire<PsiFeatureTestZone>
    {
    }

    [SetUpFixture]
    public class TestEnvironment : ExtensionTestEnvironmentAssembly<INukeTestZone>
    {
    }
}