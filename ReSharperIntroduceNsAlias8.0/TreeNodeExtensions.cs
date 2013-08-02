using JetBrains.ReSharper.Psi.Tree;

namespace IntroduceNsAlias
{
    static internal class TreeNodeExtensions
    {
        public static T FindParent<T>(this ITreeNode node)
        {
            while (node != null)
            {
                if (node is T)
                {
                    return (T)node;
                }

                node = node.Parent;
            }

            return default(T);
        }
    }
}