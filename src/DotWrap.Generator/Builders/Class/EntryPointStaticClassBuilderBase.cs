using System.Text;
using System.Text.Json;
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
    public static class {Context.WrapperName}
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
        var className = Context.ClassName;
        var entryPrefix = Context.EntryPrefix;

        // methodsSource.AppendLine(
        //     $"        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, {className}> _instances = new();"
        // );
        // methodsSource.AppendLine($"        private static int _nextId = int.MinValue;");
        // methodsSource.AppendLine();

        // internal create method
        methodsSource.AppendLine(
            $"        internal static {SelfPtrType} {Create}({className} {Obj})"
        );
        methodsSource.AppendLine("        {");
        // methodsSource.AppendLine(
        //     $"            int id = System.Threading.Interlocked.Increment(ref _nextId);"
        // );
        // methodsSource.AppendLine($"            _instances[id] = {Obj};");
        // methodsSource.AppendLine($"            return id;");
        methodsSource.AppendLine(
            @$"
            var handle = GCHandle.Alloc({Obj}, GCHandleType.Pinned);
            return GCHandle.ToIntPtr(handle);
"
        );
        methodsSource.AppendLine("        }");
        methodsSource.AppendLine();

        // internal get method
        methodsSource.AppendLine(
            $"        internal static {className} {Get}({SelfPtrType} {SelfPointerName})"
        );
        methodsSource.AppendLine("        {");
        methodsSource.AppendLine(
            @$"
            var handle = GCHandle.FromIntPtr({SelfPointerName});
            if (!handle.IsAllocated) throw new System.ArgumentException($""Invalid handle: {{{SelfPointerName}}}"");
            var {Obj} = ({className})handle.Target;
"
        );
        // methodsSource.AppendLine(
        //     $"            if (!_instances.TryGetValue({SelfPointerName}, out var {Obj}))"
        // );
        // methodsSource.AppendLine(
        //     @$"                throw new System.ArgumentException($""Invalid instance handle: {{{SelfPointerName}}}"");"
        // );
        methodsSource.AppendLine($"            return {Obj};");
        methodsSource.AppendLine("        }");
        methodsSource.AppendLine();

        // Destroy method
        methodsSource.AppendLine(
            $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{Destroy}\")]"
        );
        methodsSource.AppendLine(
            $"        public static void {Destroy}({SelfPtrType} {SelfPointerName})"
        );
        methodsSource.AppendLine("        {");
        methodsSource.AppendLine(
            @$"
            var handle = GCHandle.FromIntPtr({SelfPointerName});
            if (handle.IsAllocated)
            {{
                handle.Free();
            }}
"
        );
        // methodsSource.AppendLine($"            _instances.TryRemove({SelfPointerName}, out _);");
        methodsSource.AppendLine("        }");
        methodsSource.AppendLine();
    }

    public abstract void CreateClassBody(
        StringBuilder methodsSource,
        ClassMetadataBuilder classMetadataBuilder
    );

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
