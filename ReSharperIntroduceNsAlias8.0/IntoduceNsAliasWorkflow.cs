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

using System.Linq;

using JetBrains.TextControl;
using JetBrains.Util;

namespace IntroduceNsAlias
{
    public sealed class IntoduceNsAliasWorkflow : IntroduceLocalWorkflowBase
    {
        private static readonly string[] _ignoredNamespaces = new[] { "System.Linq", "System.Xml.Linq" };

        public ITreeNodePointer<IUsingNamespaceDirective> ImportedNamespacePointer;

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

            var sourceTokenAtCaret = TextControlToPsi.GetSourceTokenAtCaret(Solution, TextControl);
            var usingDirective = sourceTokenAtCaret.FindParent<IUsingNamespaceDirective>();

            if (usingDirective == null || usingDirective.ImportedNamespace == null)
                return false;

            // Does not support Linq
            if (_ignoredNamespaces.Contains(usingDirective.ImportedNamespace.QualifiedName))
                return false;

            ImportedNamespacePointer = usingDirective.CreateTreeElementPointer();

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