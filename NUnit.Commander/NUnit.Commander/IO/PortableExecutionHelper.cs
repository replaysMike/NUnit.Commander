using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace NUnit.Commander.IO
{
    internal static class PortableExecutableHelper
    {
        /// <summary>
        /// Get the framework type for an assembly
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        internal static FrameworkType GetAssemblyFrameworkType(string filename)
        {
            if (!File.Exists(filename))
                return FrameworkType.Unknown;

            try
            {
                var assembly = Assembly.LoadFrom(filename);
                var targetFrameworkAttribute = assembly.CustomAttributes.Where(x => x.AttributeType.Equals(typeof(TargetFrameworkAttribute))).FirstOrDefault();
                if (targetFrameworkAttribute != null)
                {
                    var targetFramework = targetFrameworkAttribute.ConstructorArguments.First().Value.ToString();
                    if (targetFramework?.StartsWith(".NETFramework") ?? false)
                        return FrameworkType.DotNetFramework;
                    else if (targetFramework?.StartsWith(".NETCoreApp") ?? false)
                        return FrameworkType.DotNetCore;
                }
            }
            catch (FileLoadException)
            {
            }
            return FrameworkType.Unknown;
        }
    }
}
