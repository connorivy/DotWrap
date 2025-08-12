using System;
using DotWrap.Configuration;
using DotWrap.Extensions;
using DotWrap.Utils;

namespace DotWrap.Extensions;

public static class TypeExtensions
{
    extension(Type type)
    {
        public ExportedTypeId GetExportedTypeIdFromType()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new ExportedTypeId(AssemblyNameUtils.GetSimplifiedAssemblyName(type.AssemblyQualifiedName));
#pragma warning restore CS0618 // Type or member is obsolete
            // string ns = type.Namespace ?? "global";
            // string name = type.IsGenericType ? type.GetGenericTypeDefinition().Name.Split('`')[0] : type.Name;
            // var typeArgs = type.IsGenericType
            //     ? type.GetGenericArguments().Select(GetExportedTypeIdFromType).Select(id => DotWrapUtils.NormalizeCsTypeName(id.ToString()))
            //     : [];
            // return new ExportedTypeId(ns, name, typeArgs);
        }
    }

}
