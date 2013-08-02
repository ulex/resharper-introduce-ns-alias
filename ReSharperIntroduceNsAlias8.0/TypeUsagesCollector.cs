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
        private readonly IFile myFile;

        private readonly DocumentRange myRange;

        private readonly List<IReference> myReferences = new List<IReference>();

        public TypeUsagesCollector(IFile file, DocumentRange range)
        {
            myFile = file;
            myRange = range;
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
            //if (this.myRegionDetector.InGeneratedCode)
            //    return;
            foreach (IQualifiableReference qualifiableReference in
                element.GetFirstClassReferences().OfType<IQualifiableReference>())
            {
                if ((qualifiableReference).GetDocumentRange().ContainedIn(myRange))
                {
                    myReferences.Add(qualifiableReference);
                }
            }
        }

        public void ProcessBeforeInterior(ITreeNode element)
        {
            //this.myRegionDetector.Process(element);
        }

        public IEnumerable<IReference> Run()
        {
            myFile.ProcessDescendantsForResolve(this);
            return myReferences;
        }
    }
}