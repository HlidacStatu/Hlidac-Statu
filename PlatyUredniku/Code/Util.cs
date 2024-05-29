using Microsoft.AspNetCore.Http;
using System.Linq;

namespace PlatyUredniku
{
    public static class Util
    {
        static Util()
        { 
        }

        public static string GetFirstQueryParameter(this IQueryCollection queryCollection, string parameterName)
        {
            if (queryCollection.TryGetValue(parameterName, out var par))
            {
                return par.FirstOrDefault();
            }
            return string.Empty;
        }
        public static string[] GetQueryParameters(this IQueryCollection queryCollection, string parameterName)
        {
            if (queryCollection.TryGetValue(parameterName, out var par))
            {
                return par.Select(m=>m).ToArray();
            }
            return System.Array.Empty<string>();
        }
    }
}
