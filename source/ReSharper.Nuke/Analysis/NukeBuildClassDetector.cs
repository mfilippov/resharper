// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharper.Nuke.Utility;

namespace ReSharper.Nuke.Analysis
{
    //[ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[] { typeof(NukeMarkOnGutter) })]
    public class NukeBuildClassDetector : NukeElementProblemAnalyzer<IClassLikeDeclaration>
    {
        protected override void Analyze(IClassLikeDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var @class = element.DeclaredElement;
            if (@class == null) return;
            if (@class.IsNukeBuildClass()) AddGutterMark(element, element.GetDocumentRange(), "Nuke build definition", consumer);
        }
    }
}