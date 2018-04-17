// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;
using ReSharper.Nuke.Highlightings;
using ReSharper.Nuke.Utility;

namespace ReSharper.Nuke.Analysis
{
    public abstract class NukeElementProblemAnalyzer<T> : ElementProblemAnalyzer<T>
        where T : ITreeNode
    {
        protected override void Run(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var processKind = data.GetDaemonProcessKind();

            if (processKind != DaemonProcessKind.VISIBLE_DOCUMENT) return;
            if (!element.GetProject().IsNukeProject()) return;

            Analyze(element, data, consumer);
        }

        protected abstract void Analyze(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);

        protected void AddGutterMark(T element, DocumentRange documentRange, string tooltip, IHighlightingConsumer consumer)
        {
            var highlighting = new NukeMarkOnGutter(element, documentRange, tooltip);
            consumer.AddHighlighting(highlighting, documentRange);
        }
    }
}