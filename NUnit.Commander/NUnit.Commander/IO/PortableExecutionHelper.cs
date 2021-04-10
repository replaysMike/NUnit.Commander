using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
                using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var peReader = new PEReader(fileStream);
                var reader = peReader.GetMetadataReader();
                var assembly = reader.GetAssemblyDefinition();
                var targetFramework = GetTargetFrameworkFromAssembly(reader, assembly);
                if (targetFramework?.StartsWith(".NETFramework", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    return FrameworkType.DotNetFramework;
                else if (targetFramework?.StartsWith(".NETCoreApp", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    return FrameworkType.DotNetCore;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception trying to load assembly '{filename}'! {ex.GetType()}: {ex.GetBaseException().Message}");
            }
            return FrameworkType.Unknown;
        }

        /// <summary>
        /// Get the parameter values of a custom attribute
        /// </summary>
        /// <param name="customAttribute"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static ImmutableArray<string> GetParameterValues(this CustomAttribute customAttribute, MetadataReader reader)
        {
            if (customAttribute.Constructor.Kind != HandleKind.MemberReference) throw new InvalidOperationException();

            var ctor = reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
            var provider = new StringParameterValueTypeProvider(reader, customAttribute.Value);
            var signature = ctor.DecodeMethodSignature(provider, null);
            return signature.ParameterTypes;
        }

        /// <summary>
        /// Provider for parsing string parameter values
        /// </summary>
        private sealed class StringParameterValueTypeProvider : ISignatureTypeProvider<string, object>
        {
            private readonly BlobReader valueReader;

            public StringParameterValueTypeProvider(MetadataReader reader, BlobHandle value)
            {
                Reader = reader;
                valueReader = reader.GetBlobReader(value);

                var prolog = valueReader.ReadUInt16();
                if (prolog != 1) throw new BadImageFormatException("Invalid custom attribute prolog.");
            }

            public MetadataReader Reader { get; }

            public string GetArrayType(string elementType, ArrayShape shape) => "";
            public string GetByReferenceType(string elementType) => "";
            public string GetFunctionPointerType(MethodSignature<string> signature) => "";
            public string GetGenericInstance(string genericType, ImmutableArray<string> typestrings) => "";
            public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) { throw new NotImplementedException(); }
            public string GetGenericMethodParameter(int index) => "";
            public string GetGenericMethodParameter(object genericContext, int index) { throw new NotImplementedException(); }
            public string GetGenericTypeParameter(int index) => string.Empty;
            public string GetGenericTypeParameter(object genericContext, int index) { throw new NotImplementedException(); }
            public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => "";
            public string GetPinnedType(string elementType) => string.Empty;
            public string GetPointerType(string elementType) => string.Empty;
            public string GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                if (typeCode == PrimitiveTypeCode.String) return valueReader.ReadSerializedString();
                return string.Empty;
            }
            public string GetSZArrayType(string elementType) => "";
            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => "";
            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => "";
            public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => "";
        }

        /// <summary>
        /// Get the TargetFrameworkAttribute parameter value from an <see cref="AssemblyDefinition"/>
        /// </summary>
        /// <param name="reader">The metadata reader</param>
        /// <param name="assembly">The assembly definition</param>
        /// <returns></returns>
        private static string GetTargetFrameworkFromAssembly(MetadataReader reader, AssemblyDefinition assembly)
        {
            var customAttrs = assembly.GetCustomAttributes().Select(reader.GetCustomAttribute).Where(x => x.Constructor.Kind == HandleKind.MemberReference);
            foreach (var attribute in customAttrs)
            {
                var memberRef = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                var container = memberRef.Parent;
                var attributeTypeRef = reader.GetTypeReference((TypeReferenceHandle)container);
                var attributeNameHandle = attributeTypeRef.Name;
                var attributeName = reader.GetString(attributeNameHandle);
                if (attributeName.Equals(nameof(TargetFrameworkAttribute)))
                {
                    return attribute.GetParameterValues(reader).FirstOrDefault();
                }
            }
            return string.Empty;
        }
    }
}
