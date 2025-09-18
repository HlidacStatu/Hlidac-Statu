using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Util
{
    public static class DebugUtil
    {
        public static string GetClassAndMethodName(MethodBase method)
        {
            if (method == null)
                return "";
            var className = method.DeclaringType?.Name ?? "";
            var methodName = method.Name;
            return $"{className}.{methodName}";
        }

    }
}
