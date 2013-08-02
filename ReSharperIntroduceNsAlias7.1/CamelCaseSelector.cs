using System.Linq;

namespace IntroduceNsAlias
{
    public static class CamelCaseSelector
    {
        public static string GetCamelCaseSuggestion(string clrName)
        {
            if (clrName == null)
            {
                return null;
            }

            var result = string.Concat(clrName.Where(char.IsUpper));

            if (string.IsNullOrEmpty(result))
            {
                var dotPosition = clrName.LastIndexOf('.');
                if (dotPosition == -1 || dotPosition == clrName.Length - 1)
                {
                    result = clrName;
                }
                else
                {
                    result = clrName.Substring(dotPosition + 1);
                }
            }

            return result;
        }
    }
}