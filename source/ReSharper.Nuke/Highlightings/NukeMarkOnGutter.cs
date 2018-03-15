// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/ide-extensions/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharper.Nuke.Actions;
using ReSharper.Nuke.Resources;
using ReSharper.Nuke.Utility;

namespace ReSharper.Nuke.Highlightings
{
    [StaticSeverityHighlighting(Severity.INFO,
        HighlightingGroupIds.GutterMarksGroup,
        OverlapResolve = OverlapResolveKind.NONE,
        AttributeId = NukeHighlitingAttributeIds.NukeGutterIconAttribute)]
    public class NukeMarkOnGutter : IHighlighting, INukeHighlighting
    {
        private readonly ITreeNode _myElement;
        private readonly DocumentRange _myRange;

        public NukeMarkOnGutter(ITreeNode element, DocumentRange range, string tooltip)
        {
            _myElement = element;
            _myRange = range;
            ToolTip = tooltip;
        }

        public bool IsValid() => _myElement == null || _myElement.IsValid();

        public IEnumerable<BulbMenuItem> GetBulbMenuItems(ISolution solution, ITextControl textControl, IAnchor gutterMarkAnchor)
        {
            var propertyDeclaration = _myElement as IPropertyDeclaration;
            var property = propertyDeclaration?.DeclaredElement;

            if (property == null) return EmptyList<BulbMenuItem>.Enumerable;

            var propertyName = propertyDeclaration.DeclaredName;
            if (property.IsNukeBuildParameter())
            {
                return CreateParameterMenu(propertyName, gutterMarkAnchor, solution, textControl);
            }

            if (property.IsNukeBuildTarget())
            {
                return CreateRunTargetMenu(propertyName, gutterMarkAnchor, solution, textControl);
            }

            return EmptyList<BulbMenuItem>.Enumerable;
        }

        private BulbMenuItem CreateItem(IBulbAction bulbAction, ISolution solution, ITextControl textControl, IAnchor anchor)
        {
            return new BulbMenuItem(new IntentionAction.MyExecutableProxi(bulbAction, solution, textControl),
                bulbAction.Text,
                LogoThemedIcons.NukeLogo.Id,
                anchor);
        }

        private BulbMenuItem[] CreateRunTargetMenu(string propertyName, IAnchor gutterMarkAnchor, ISolution solution, ITextControl textControl)
        {
            var runSubMenuAnchor = new SubmenuAnchor(gutterMarkAnchor,
                new SubmenuBehavior(text: null, icon: null, executable: true, removeFirst: true));
            var debugSubMenuAnchor = new SubmenuAnchor(gutterMarkAnchor,
                new SubmenuBehavior(text: null, icon: null, executable: true, removeFirst: true));
            return new[]
                   {
                       CreateItem(new RunNukeTargetBulbAction(propertyName), solution, textControl, runSubMenuAnchor),
                       CreateItem(new DebugNukeTargetBulbAction(propertyName), solution, textControl, debugSubMenuAnchor),
                       CreateItem(new RunSingleNukeTargetBulbAction(propertyName), solution, textControl, runSubMenuAnchor),
                       CreateItem(new DebugSingleNukeTargetBulbAction(propertyName), solution, textControl, debugSubMenuAnchor)
                   };
        }

        private BulbMenuItem[] CreateParameterMenu(string propertyName, IAnchor gutterMarkAnchor, ISolution solution, ITextControl textControl)
        {
            return new[]

                   {
                       CreateItem(new NukeParameterBulbAction(propertyName), solution, textControl, gutterMarkAnchor)
                   };
        }

        public DocumentRange CalculateRange() => _myRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
    }
}