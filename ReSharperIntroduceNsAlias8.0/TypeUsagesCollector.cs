using System.Collections.Generic;

using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace IntroduceNsAlias
{
    internal sealed class TypeUsagesCollector : IRecursiveElementProcessor
    {
        private readonly ITreeNode _myFile;

        private readonly DocumentRange _myRange;

        private readonly List<IReference> _myReferences = new List<IReference>();

        public TypeUsagesCollector(ITreeNode file, DocumentRange range)
        {
            _myFile = file;
            _myRange = range;
        }

        public bool ProcessingIsFinished
        {
            get
            {
                return false;
            }
        }

        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            return true;
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
            foreach (IQualifiableReferenceBase qualifiableReference in
                element.GetFirstClassReferences().OfType<IQualifiableReferenceBase>())
            {
                if ((qualifiableReference).GetDocumentRange().ContainedIn(_myRange))
                {
                    _myReferences.Add(qualifiableReference);
                }
            }
        }

        public void ProcessBeforeInterior(ITreeNode element)
        {
        }

        public IEnumerable<IReference> Run()
        {
            _myFile.ProcessDescendantsForResolve(this);
            return _myReferences;
        }
    }
}