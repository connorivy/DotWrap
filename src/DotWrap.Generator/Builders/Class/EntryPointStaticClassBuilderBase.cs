using System.Text;
using System.Text.Json;
using DotWrap.Generator.Extensions;
using DotWrap.MSBuild;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator.Builders.Class;

public abstract class EntryPointStaticClassBuilderBase(ClassBuilderContext context)
{
    protected ClassBuilderContext Context => context;

    public string GenerateClassFile()
    {
        StringBuilder classBody = new();

        if (!context.IsStatic)
        {
            this.AddMemoryManagmentMethods(classBody);
        }

        var classMetadataBuilder = new ClassMetadataBuilder(Context);
        this.CreateClassBody(classBody, classMetadataBuilder);

        this.AddSpecialMethods(classBody, classMetadataBuilder, context);

        this.AddMetadata(classBody, classMetadataBuilder);

        var sourceText =
            $@"
using System;
using System.Runtime.InteropServices;

namespace {Context.Namespace}
{{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [global::System.CodeDom.Compiler.GeneratedCode(""DotWrap"", ""1.0.0"")]
    [global::{nameof(DotWrap)}.{nameof(DotWrap.DotWrapGeneratedAttribute).Replace("Attribute", "")}]
    internal static class {Context.WrapperName}
    {{
{classBody.ToString().TrimEnd()}
    }}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}}
";

        return sourceText;
    }

    protected void AddMemoryManagmentMethods(StringBuilder methodsSource)
    {
        var className = Context.FullyQualifiedClassName;
        var entryPrefix = Context.EntryPrefix;

        // internal create method
        methodsSource.Append(
            @$"
        internal static {SelfPtrType} {Create}({className} {Obj})
        {{
            var handle = GCHandle.Alloc({Obj}, GCHandleType.Normal);
            return GCHandle.ToIntPtr(handle);
        }}
"
        );

        // internal get method
        methodsSource.AppendLine(
            @$"
        internal static {className} {Get}({SelfPtrType} {SelfPointerName})
        {{
            var handle = GCHandle.FromIntPtr({SelfPointerName});
            if (!handle.IsAllocated) throw new System.ArgumentException($""Invalid handle: {{{SelfPointerName}}}"");
            var {Obj} = ({className})handle.Target;
            return {Obj};
        }}
"
        );

        // Destroy method
        methodsSource.AppendLine(
            @$"
        [UnmanagedCallersOnly(EntryPoint = ""{entryPrefix}{Destroy}"")]
        public static void {Destroy}({SelfPtrType} {SelfPointerName})
        {{
            var handle = GCHandle.FromIntPtr({SelfPointerName});
            if (handle.IsAllocated)
            {{
                handle.Free();
            }}
        }}
"
        );
    }

    public abstract void CreateClassBody(
        StringBuilder methodsSource,
        ClassMetadataBuilder classMetadataBuilder
    );

    protected void AddSpecialMethods(
        StringBuilder classBody,
        ClassMetadataBuilder classMetadataBuilder,
        ClassBuilderContext context
    )
    {
        ExportedClassInfo cls = classMetadataBuilder.ClassInfo;
        // get icollection<T> interface symbol if implemented by context.ClassSymbol
        var iCollectionSymbol = context.ClassSymbol.AllInterfaces.FirstOrDefault(i =>
            i.Name == "ICollection" || i.Name == "IReadOnlyCollection"
        );

        if (iCollectionSymbol is not { TypeArguments: [var typeArg] })
        {
            return;
        }
        context.GlobalContext.AddInferedType(typeArg);

        if (
            cls.TryGetICollectionType(out var collectionType)
            || cls.TryGetIReadonlyCollectionType(out collectionType)
        )
        {
            ExportedMethodInfo getCount = new()
            {
                OriginalName = GetCount,
                OriginalType = "int",
                IsStatic = false,
                ExposedTypeIfDifferent = null,
                GenericTypeName = null,
                SpecialCaseFlags = MethodSpecialCaseFlags.None,
                Parameters = [],
            };
            classMetadataBuilder.AddMethod(getCount);

            ExportedMethodInfo fillArr = new()
            {
                OriginalName = FillArr,
                OriginalType = "void",
                IsStatic = false,
                ExposedTypeIfDifferent = null,
                GenericTypeName = null,
                SpecialCaseFlags = MethodSpecialCaseFlags.None,
                Parameters =
                [
                    new ExportedParameterInfo
                    {
                        Name = "numpyArrPtr",
                        OriginalType = "IntPtr",
                        ExposedTypeIfDifferent = null,
                        GenericTypeName = null,
                    },
                    new ExportedParameterInfo
                    {
                        Name = "collectionCount",
                        OriginalType = "int",
                        ExposedTypeIfDifferent = null,
                        GenericTypeName = null,
                    },
                ],
            };
            classMetadataBuilder.AddMethod(fillArr);
            classBody.AppendLine(
                @$"
        [UnmanagedCallersOnly(EntryPoint = ""{Context.EntryPrefix}{GetCount}"")]
        public static int {GetCount}({SelfPtrType} {SelfPointerName})
        {{
            var {Obj} = {Get}({SelfPointerName});
            return ((ICollection){Obj}).Count;
        }}

        [UnmanagedCallersOnly(EntryPoint = ""{Context.EntryPrefix}{FillArr}"")]
        public static void {FillArr}({SelfPtrType} {SelfPointerName}, IntPtr numpyArrPtr, int collectionCount)
        {{
            var {Obj} = {Get}({SelfPointerName});
        "
            );

            bool isBlitable = typeArg.SpecialType.IsBlittable();
            var blittable = isBlitable ? "Blittable" : "NonBlittable";
            string collectionTypeName;
            if (
                isBlitable
                && context.ClassSymbol is IArrayTypeSymbol arrayTypeSymbol
                && SymbolEqualityComparer.Default.Equals(arrayTypeSymbol.ElementType, typeArg)
            )
            {
                collectionTypeName = "Array";
            }
            else
            {
                collectionTypeName = "Enumerable";
            }

            classBody.AppendLine(
                @$"
            global::DotWrap.Operations.Ops.Copy{blittable}{collectionTypeName}InfoToNumpyArray({Obj}, numpyArrPtr, collectionCount);
        }}
        "
            );
        }
    }

    protected void AddMetadata(StringBuilder classBody, ClassMetadataBuilder classMetadataBuilder)
    {
        var jsonMeta =
            @$"
#pragma warning disable CS0414 // Field is assigned to but its value is never used
        private static readonly string {ClassMetadata} =  
        """"""
        {JsonSerializer.Serialize(
            classMetadataBuilder.ClassInfo,
            DotWrapSerializerOptions.Default
        )}
        """""";
#pragma warning restore CS0414 // Field is assigned to but its value is never used";
        classBody.Append(jsonMeta);
    }
}
