using System;

using JetBrains.Application.DataContext;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Refactorings.IntroduceVariable;
using JetBrains.ReSharper.Refactorings.Workflow;

using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Psi.VB.Impl;
using JetBrains.TextControl;
using JetBrains.Util;

namespace IntroduceNsAlias
{
    public sealed class IntoduceNsAliasWorkflow : IntroduceLocalWorkflowBase
    {
        private static readonly string[] _ignoredNamespaces = new[] { "System.Linq", "System.Xml.Linq" };

        public IReferenceName ImportedNamespacePointer;

        public override string Title
        {
            get
            {
                return "&Introduce namespace alias";
            }
        }

        public IntoduceNsAliasWorkflow(ISolution solution, string actionId)
            : base(solution, actionId)
        {
        }

        public override bool IsAvailable(IDataContext context)
        {
            // check text control
            if (!base.IsAvailable(context))
                return false;
            var sourceTokenAtCaret = JetBrains.ReSharper.Feature.Services.Util.TextControlToPsi.GetSourceTokenAtCaret(Solution, TextControl);

            var usingDirective = sourceTokenAtCaret.FindParent<IUsingDirective>();

            if (usingDirective == null || usingDirective.ImportedSymbolName == null)
                return false;

            // Does not support Linq
            if (_ignoredNamespaces.Contains(usingDirective.ImportedSymbolName.QualifiedName, StringComparer.Ordinal))
                return false;

            ImportedNamespacePointer = usingDirective.ImportedSymbolName;

            return true;
        }

        public override bool Initialize(IDataContext context)
        {
            IDocument data = context.GetData(JetBrains.DocumentModel.DataConstants.DOCUMENT);
            if (data == null) return false;
            Marker = JetBrains.DocumentManagers.RangeMarkerExtentions.CreateRangeMarker(new DocumentRange(data, new TextRange(ITextControlCaretEx.Offset(this.TextControl.Caret))), DocumentManager.GetInstance(Solution));
            return IsAvailable(context);

        }

        public override IRefactoringExecuter CreateRefactoring(IRefactoringDriver driver)
        {
            return new IntoduceNsAliasRefactoring(this, Solution, driver);
        }
    }
}