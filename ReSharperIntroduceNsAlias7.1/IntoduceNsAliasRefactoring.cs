using System.Collections.Generic;

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.IntroduceVariable;
using JetBrains.ReSharper.Refactorings.Workflow;

using System.Linq;

using JetBrains.Util.Special;

namespace IntroduceNsAlias
{
    public class IntoduceNsAliasRefactoring : IntroduceLocalRefactoring
    {
        private readonly IntoduceNsAliasWorkflow _workFlow;

        private readonly ISolution _solution;

        private string _suggestedName;

        public IntoduceNsAliasRefactoring(IntoduceNsAliasWorkflow workFlow, ISolution solution, IRefactoringDriver driver)
            : base(workFlow, solution, driver)
        {
            _workFlow = workFlow;
        }

        protected override string CanNotPreformActionText
        {
            get { return "Refactoring failed"; }
        }

        protected override ReplaceInfo CreateReplaceInfo()
        {
            var usingDirective = _workFlow.ImportedNamespacePointer.GetTreeNode();

            if (usingDirective == null) return null;

            var importedNs = usingDirective.ImportedNamespace;
            if (importedNs == null) return null;

            _suggestedName = CamelCaseSelector.GetCamelCaseSuggestion(importedNs.QualifiedName);

            var factory = CSharpElementFactory.GetInstance(usingDirective.GetPsiModule());


            // replace using of extension methods
            var refname = usingDirective.Children<IReferenceName>().SingleOrDefault();
            var selectedName = importedNs.QualifiedName;

            var file = usingDirective.Root() as IFile;
            if (file == null) return null;

            CallExtensionMethodsAsStatic(file, importedNs, factory);

            var replacedNodes = new List<ITreeNode>();

            var myReferenceCollector = new MyReferenceCollector();
            file.ProcessDescendantsForResolve(myReferenceCollector);
            var referencesTypesReplace = myReferenceCollector.Referenced;

            // Add alias to namespace
            var newchild = factory.CreateUsingDirective("$0 = $1", _suggestedName, importedNs.QualifiedName);
            //ModificationUtil(usingDirective.Parent, newchild);
            newchild = ModificationUtil.AddChildAfter(usingDirective, newchild);
            replacedNodes.Add((newchild as IUsingAliasDirective).Alias);

            // Modify call

            foreach (var bindedResult in referencesTypesReplace)
            {
                //var type = bindedResult.GetTreeNode();
                //if (type == null) continue;
                var declaredElement = bindedResult.Resolve().DeclaredElement;

                var astypeElement = declaredElement as ITypeElement;
                var asMethod = declaredElement as IMethod;
                IClrTypeName clrName = null;
                string containedns = null;
                string methodName = null;
                if (astypeElement != null)
                {
                    clrName = astypeElement.GetClrName();
                    containedns = astypeElement.GetContainingNamespace().QualifiedName;
                }
                else if (asMethod != null)
                {
                    var containingType = asMethod.GetContainingType();
                    clrName = containingType.GetClrName();
                    containedns = containingType.GetContainingNamespace().QualifiedName;
                    methodName = asMethod.ShortName;
                }

                if ((astypeElement != null || (asMethod != null && asMethod.IsStatic)) && containedns == importedNs.QualifiedName)
                {
                     var list = new List<string> { _suggestedName };
                    foreach (var typeParameterNumber in clrName.TypeNames)
                        list.Add(CSharpImplUtil.MakeSafeName(typeParameterNumber.TypeName));

                    if (methodName != null)
                    {
                        list.Add(CSharpImplUtil.MakeSafeName(methodName));
                    }

                    var result = string.Join(".", list);

                    var treeNode = bindedResult.GetTreeNode();
                    var refa = CSharpReferenceBindingUtil.ReplaceReferenceElement(treeNode, result, true);
                    var node = refa.GetTreeNode();
                    var firstChild = node.GetFirstTokenIn();
                    replacedNodes.Add(firstChild);
                }
            }

            ModificationUtil.DeleteChild(usingDirective);

            return new ReplaceInfo(
                null,
                null,
                replacedNodes.ToArray(),
                new NameSuggestionsExpression(new[] { _suggestedName }),
                PsiManager.GetInstance(Solution));
        }

        private static void CallExtensionMethodsAsStatic(IFile file, INamespace importedNs, CSharpElementFactory factory)
        {
            var targetsCollector = new TypeUsagesCollector(file, file.GetDocumentRange());
            var references = targetsCollector.Run();
            foreach (var reference in references)
            {
                var resolve = reference.Resolve();
                var declaredElement = resolve.DeclaredElement;

                var imethod = declaredElement as IMethod;

                var methodExecution = reference.GetTreeNode();
                var method = methodExecution as IReferenceExpression;

                var containingNsQualifiedName =
                    imethod.IfNotNull(m => m.GetContainingType())
                           .IfNotNull(t => t.GetContainingNamespace())
                           .IfNotNull(ns => ns.QualifiedName);

                if (imethod != null && method.IsExtensionMethod() && containingNsQualifiedName == importedNs.QualifiedName)
                {
                    var invocate = InvocationExpressionNavigator.GetByInvokedExpression(method);
                    //if (invocate == null) return null;

                    // build string like $0($1,$2,..argc)
                    var tokens = string.Format(
                        "$0({0})",
                        string.Join(
                            ",",
                            new[] { "$1" }.Concat(invocate.Arguments.Select((a, i) => "$" + (i + 2).ToString("G"))).ToArray()));

                    var funcAndThis = new object[] { "faketobind", (invocate.InvokedExpression as IReferenceExpression).QualifierExpression };
                    var paramsObject = funcAndThis.Concat(invocate.Arguments).ToArray();
                    var cSharpExpression = (IInvocationExpression)factory.CreateExpression(tokens, paramsObject);
                    var replaceResult = invocate.ReplaceBy(cSharpExpression);
                    (replaceResult.InvokedExpression as IReferenceExpression).Reference.BindTo(declaredElement);
                }
            }
        }

        protected override void ValueSelectedCallback(Hotspot oldHotspot)
        {
            throw new System.NotImplementedException();
        }
    }
}