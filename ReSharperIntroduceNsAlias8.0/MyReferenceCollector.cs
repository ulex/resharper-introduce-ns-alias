using System.Collections.Generic;

using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace IntroduceNsAlias
{
    public class MyReferenceCollector : IRecursiveElementProcessor
    {
        private readonly List<IReference> _referenced = new List<IReference>();

        public IEnumerable<IReference> Referenced
        {
            get
            {
                return _referenced;
            }
        }

        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            return true;
        }

        public void ProcessBeforeInterior(ITreeNode element)
        {
            if (element is ITypeUsage || element is IUserDeclaredTypeUsage)
            {
                _referenced.AddRange(element.LastChild.GetFirstClassReferences().OfType<IReference>());
            }
            else if (element is IInvocationExpression)
            {
                var expr = (element as IInvocationExpression).InvokedExpression;
                _referenced.AddRange(expr.GetFirstClassReferences().OfType<IReference>());
            }
            else if (element is IAttribute)
            {
                _referenced.AddRange((element as IAttribute).Name.GetFirstClassReferences().OfType<IReference>());
            }
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
        }

        public bool ProcessingIsFinished { get { return false; } }
    }
}