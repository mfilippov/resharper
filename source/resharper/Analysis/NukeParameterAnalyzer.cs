// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharper.Nuke.Highlightings;
using ReSharper.Nuke.Utility;

namespace ReSharper.Nuke.Analysis
{
    //[ElementProblemAnalyzer(typeof(IMemberOwnerDeclaration), HighlightingTypes = new[] { typeof(NukeMarkOnGutter) })]
    public class NukeParameterAnalyzer : NukeElementProblemAnalyzer<IMemberOwnerDeclaration>
    {
        protected override void Analyze(IMemberOwnerDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var typeElement = element.DeclaredElement;
            if (typeElement == null)
                return;

            if (!typeElement.IsNukeBuildClass()) return;

            foreach (var typeMember in typeElement.GetMembers())
                if (typeMember.IsNukeBuildParameter())
                {
                    AddGutterMark(consumer, (IProperty) typeMember);
                }
        }

        private void AddGutterMark(IHighlightingConsumer consumer, IProperty property)
        {
            foreach (var declaration in property.GetDeclarations())
            {
                AddGutterMark(declaration, consumer);
            }
        }

        private void AddGutterMark(IDeclaration declaration, IHighlightingConsumer consumer)
        {
            var documentRange = declaration.GetDocumentRange();
            var tooltip = "Nuke Parameter";
            var highlighting = new NukeMarkOnGutter(declaration, documentRange, tooltip);
            consumer.AddHighlighting(highlighting, documentRange);
        }
    }
}