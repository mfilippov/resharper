// Copyright Matthias Koch, Sebastian Karasek 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/ide-extensions/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;
using ReSharper.Nuke.Highlightings;

namespace ReSharper.Nuke.Tests.Analysis
{
    public class NukeTargetPropertyAnalyzerTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => "Analysis";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is INukeHighlighting;
        }

        [Test] public void TestNukeTargetPropertyAnalyzer()
        {
            DoNamedTest2();
        }
    }
}