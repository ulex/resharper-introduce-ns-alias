using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.IntroduceVariable;
using JetBrains.ReSharper.Refactorings.Workflow;

using System.Linq;
using JetBrains.Util;
using JetBrains.Util.Special;

namespace IntroduceNsAlias
{
    public class IntoduceNsAliasRefactoring : IntroduceLocalRefactoring
    {
        private readonly IntoduceNsAliasWorkflow _workFlow;

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

            var scope = usingDirective.FindParent<ITypeAndNamespaceHolderDeclaration>();
            if (scope == null) return null;
            
            CallExtensionMethodsAsStatic(scope, importedNs, factory);

            var replacedNodes = new List<ITreeNode>();

            var myReferenceCollector = new MyReferenceCollector();
            scope.ProcessDescendantsForResolve(myReferenceCollector);

            using (WriteLockCookie.Create())
            {
                // Add alias to namespace
                var newchild = factory.CreateUsingDirective("$0 = $1", _suggestedName, importedNs.QualifiedName);
                newchild = ModificationUtil.AddChildAfter(usingDirective, newchild);
            
                replacedNodes.Add((newchild as IUsingAliasDirective).Alias);

                AppendUsages(myReferenceCollector.Referenced, importedNs, replacedNodes);

                // delete old using
                ModificationUtil.DeleteChild(usingDirective);
            }

            return new ReplaceInfo(
                null,
                null,
                replacedNodes.ToArray(),
                new NameSuggestionsExpression(new[] { _suggestedName }),
                Solution.GetPsiServices().Files);
        }

        private void AppendUsages(IList<IReference> referencesTypesReplace, INamespace importedNs, List<ITreeNode> replacedNodes)
        {
            for (int index = 0; index < referencesTypesReplace.Count; index++)
            {
                var bindedResult = referencesTypesReplace[index];
                if (!bindedResult.IsValid()) continue;
                var declaredElement = bindedResult.Resolve().DeclaredElement;

                var astypeElement = declaredElement as ITypeElement;
                var asTypeMember = declaredElement as ITypeMember;
                IClrTypeName clrName = null;
                string containedns = null;
                string methodName = null;
                if (astypeElement != null)
                {
                    clrName = astypeElement.GetClrName();
                    containedns = astypeElement.GetContainingNamespace().QualifiedName;
                }
                else if (asTypeMember != null)
                {
                    var containingType = asTypeMember.GetContainingType();
                    clrName = containingType.GetClrName();
                    containedns = containingType.GetContainingNamespace().QualifiedName;
                    methodName = asTypeMember.ShortName;
                }

                if (((astypeElement != null && !(bindedResult is IPredefinedTypeReference)) ||
                     (asTypeMember != null && asTypeMember.IsStatic))
                    && containedns == importedNs.QualifiedName)
                {
                    var list = new List<string> {_suggestedName};
                    foreach (var typeParameterNumber in clrName.TypeNames)
                    {
                        list.Add(CSharpImplUtil.MakeSafeName(typeParameterNumber.TypeName));
                    }

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
        }

        private static void CallExtensionMethodsAsStatic(ITreeNode node, INamespace importedNs, CSharpElementFactory factory)
        {
            var targetsCollector = new TypeUsagesCollector(node, node.GetDocumentRange());
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

                if (method != null && method.IsExtensionMethod() && containingNsQualifiedName == importedNs.QualifiedName)
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