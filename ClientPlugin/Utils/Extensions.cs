using System.Linq;
using System.Reflection;

namespace ClientPlugin.Utils
{
    public static class Extensions
    {
        public static string GetSignature(this MethodInfo methodInfo)
        {
            var parameters = string.Join(",", methodInfo.GetParameters()
                .Select(p => $"{p.ParameterType.Name}"));

            return $"{methodInfo.ReturnType.Name} {methodInfo.DeclaringType?.FullName}.{methodInfo.Name}({parameters})";
        }
    }
}