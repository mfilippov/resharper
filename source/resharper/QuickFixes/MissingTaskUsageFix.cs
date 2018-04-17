// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/resharper/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharper.Nuke.Utility;

namespace ReSharper.Nuke.QuickFixes
{
    [QuickFix]
    public class MissingTaskUsageFix : QuickFixBase
    {
        private readonly NotResolvedError _error;
        private IClass _taskClass;

        public MissingTaskUsageFix(NotResolvedError error)
        {
            _error = error;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            return textControl =>
            {
                var psiFiles = solution.GetComponent<IPsiFiles>();
                var psiServices = solution.GetComponent<IPsiServices>();

                var buildPsiSourceFile = textControl.Document.GetPsiSourceFile(solution).NotNull();
                var buildCSharpFile = psiFiles.GetPsiFiles<CSharpLanguage>(buildPsiSourceFile).OfType<ICSharpFile>().Single();

                var target = new DeclaredElementInstance<ITypeElement>(_taskClass);
                var usingDirective = CSharpElementFactory.GetInstance(buildCSharpFile).CreateUsingStaticDirective(target);

                if (UsingUtil.GetImportConflicts(buildCSharpFile, target).Any())
                {
                    MessageBox.ShowError("Unable to import static class (import introduces conflicts in file)");
                    return;
                }

                using (var cookie = new PsiTransactionCookie(psiServices, DefaultAction.Rollback, typeof(MissingTaskUsageFix).Name))
                {
                    UsingUtil.AddImportTo(buildCSharpFile, usingDirective);
                    cookie.Commit();
                }

                var method = _taskClass.Methods.First(x => x.ShortName.Equals(_error.Reference.GetName(), StringComparison.OrdinalIgnoreCase));
                textControl.Document.ReplaceText(_error.CalculateRange().TextRange, method.ShortName);
            };
        }

        public override string Text => $"Add static using to `{_taskClass.GetClrName().FullName}`.";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            //Todo cache?
            var project = _error.Reference.GetTreeNode().GetProject();
            if (project == null || !project.IsNukeProject()) return false;
            var psiService = _error.Reference.GetTreeNode().GetPsiServices();
            var symbolScope = psiService.Symbols.GetSymbolScope(LibrarySymbolScope.REFERENCED, caseSensitive: false);

            _taskClass = symbolScope.GetAllShortNames().Where(x => x.EndsWith("Tasks"))
                .SelectMany(x => symbolScope.GetElementsByShortName(x))
                .Select(x => x as IClass)
                .Where(x => x != null && x.GetContainingNamespace().QualifiedName.StartsWith("Nuke"))
                .SingleOrDefault(x => x.HasMembers(_error.Reference.GetName(), caseSensitive: false));

            return _taskClass != null;
        }
    }
}