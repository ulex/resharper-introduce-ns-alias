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

        public IList<IReference> Referenced
        {
            get
            {
                return _referenced;
            }
        }

        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            //if (element is IReferenceName || element is IReferenceExpression)
            //{
            //    return false;
            //}
            return true;
        }

        public void ProcessBeforeInterior(ITreeNode element)
        {
            if (element is IReferenceName || element is IReferenceExpression)
            {
                _referenced.AddRange(element.GetFirstClassReferences().OfType<IReference>());
            }
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
        }

        public bool ProcessingIsFinished { get { return false; } }
    }
}