using System.Collections.Generic;

using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Refactorings.Workflow;

namespace IntroduceNsAlias
{
    [RefactoringWorkflowProvider]
    public class IntoroduceNsAliasWorkflowProvider : IRefactoringWorkflowProvider
    {
        public IEnumerable<IRefactoringWorkflow> CreateWorkflow(IDataContext dataContext)
        {
            yield return new IntoduceNsAliasWorkflow(dataContext.GetData(JetBrains.ProjectModel.DataContext.DataConstants.SOLUTION), "ReSharperIntoduceNsAlias.Action");
        }
    }
}