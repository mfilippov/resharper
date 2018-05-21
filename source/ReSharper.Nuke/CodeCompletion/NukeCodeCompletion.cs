// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using ReSharper.Nuke.Resources;
using ReSharper.Nuke.Utility;

namespace ReSharper.Nuke.CodeCompletion
{
    [Language(typeof(CSharpLanguage))]
    public class NukeCodeCompletion : CSharpItemsProviderBase<CSharpCodeCompletionContext>
    {
        private static DeclaredElementPresentation<DeclaredElementInfo> GetOrCreatePresentation(
            DeclaredElementInfo info,
            DeclaredElementPresenterStyle style,
            string typeParameterString = null)
        {
            var preferredDeclaredElement = info.PreferredDeclaredElement;
            if (preferredDeclaredElement != null && preferredDeclaredElement.Element is ICompiledElement
                                                 && preferredDeclaredElement.Element.Type() == null)
                return CSharpLookupItemFactory.GetPresentationCache(preferredDeclaredElement.Element.GetSolution())
                    .GetPresentation(info, (ICompiledElement) preferredDeclaredElement.Element, style, typeParameterString);
            return new DeclaredElementPresentation<DeclaredElementInfo>(info, style, typeParameterString, LogoThemedIcons.NukeLogo.Id);
        }

        private readonly Func<LookupItem<MethodsInfo>, ILookupItemPresentation> _gGetMethodsPresentation = item =>
            GetOrCreatePresentation(item.Info, PresenterStyles.DefaultNoParametersPresenter, !item.Info.SkipGenericArguments ? "<>" : null);

        private readonly Func<LookupItem<MethodsInfo>, ILookupItemMatcher> _getMethodsMatcher = item =>
            new DeclaredElementMatcher(item.Info, item.Info.Owner.MatchingStyle, "@");

        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.BasicContext.SourceFile.GetProject().IsNukeProject();
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            var nodeInFile = context.NodeInFile;
            var tokenType = nodeInFile.GetTokenType();
            if (tokenType == null || !tokenType.IsIdentifier) return false;

            var project = context.BasicContext.SourceFile.GetProject();
            if (project == null || !project.IsNukeProject()) return false;
            var psiService = context.PsiModule.GetPsiServices();
            var symbolScope = psiService.Symbols.GetSymbolScope(LibrarySymbolScope.REFERENCED, caseSensitive: false);

            foreach (var taskMethod in GetTaskMethods(symbolScope))
            {
                collector.Add(CreateLookoutItem(context, taskMethod.Key, taskMethod.Value, skipGenericArguments: true));
            }

            return true;
        }

        #region Privates

        private ILookupItem CreateLookoutItem(
            CSharpCodeCompletionContext context,
            string name,
            IList<DeclaredElementInstance<IMethod>> methods,
            bool skipGenericArguments,
            bool includeFollowingExpression = true,
            bool setFunctionParameters = true)
        {
            var basicContext = context.BasicContext;
            var editorBrowsableProcessing = basicContext.EditorBrowsableProcessing;
            if (editorBrowsableProcessing != EditorBrowsableProcessingType.All)
                methods = methods.Where(x => CodeInsightUtil.IsBrowsable(x.Element,
                    showNever: false,
                    showAdvanced: editorBrowsableProcessing == EditorBrowsableProcessingType.Advanced)).ToList();
            if (methods.IsEmpty())
                return null;
            var elements = methods.Select(_ => new Pair<IDeclaredElement, ISubstitution>(_.Element, _.Substitution));
            var methodsInfo = new MethodsInfo(name, elements, CSharpLanguage.Instance, basicContext.LookupItemsOwner, context);

            methodsInfo.Ranges = context.CompletionRanges;
            methodsInfo.SkipGenericArguments = skipGenericArguments;
            methodsInfo.PutData(CompletionKeys.IsMethodsKey, new object());

            if (methods.All(x => x.Element.IsExtensionMethod))
                methodsInfo.PutKey(CompletionKeys.IsExtensionMethodsKey);

            var lookupItem = LookupItemFactory.CreateLookupItem(methodsInfo).WithPresentation(_gGetMethodsPresentation)
                .WithBehavior(_ => new NukeTasksBehaviour(_.Info)).WithMatcher(_getMethodsMatcher);
            var qualifiableReference = CSharpLookupItemFactory.GetQualifiableReference(context);
            var qualifier = qualifiableReference != null ? qualifiableReference.GetQualifier() : null;
            var qualifierStaticness = qualifier == null ? new Staticness?() : AccessUtil.GetQualifierStaticness(qualifier);
            if (setFunctionParameters)
            {
                var functions = methodsInfo.AllDeclaredElements.Select(x => x.Element).OfType<IMethod>();
                CSharpLookupItemFactory.Instance.SetFunctionParameters(context,
                    functions,
                    lookupItem,
                    methodsInfo,
                    qualifiableReference,
                    qualifierStaticness,
                    includeFollowingExpression);
            }

            methodsInfo.Placement.OrderString = name;
            return lookupItem;
        }

        private IEnumerable<KeyValuePair<string, IList<DeclaredElementInstance<IMethod>>>> GetTaskMethods(ISymbolScope symbolScope)
        {
            var map = new OneToListMap<string, DeclaredElementInstance<IMethod>>();
           
            foreach (var shortName in symbolScope.GetAllShortNames())
            {
                if (!shortName.EndsWith("Tasks")) continue;
                var symbols = symbolScope.GetElementsByShortName(shortName);

                foreach (var symbol in symbols)
                {
                    if (!(symbol is IClass @class)) continue;
                    if (!@class.GetContainingNamespace().QualifiedName.StartsWith("Nuke")) continue;

                    foreach (var classMethod in @class.Methods
                        .Where(x => x.AccessibilityDomain.DomainType == AccessibilityDomain.AccessibilityDomainType.PUBLIC && x.IsStatic)) map.Add(classMethod.ShortName, new DeclaredElementInstance<IMethod>(classMethod));
                }
            }

            return map;
        }

        #endregion
    }
}