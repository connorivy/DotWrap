using System.Text;
using DotWrap.Generator.Builders.Method;

namespace DotWrap.Generator.Builders.Class;

public class ExplicitWrapperBuilder(ClassBuilderContext context)
    : EntryPointStaticClassBuilderBase(context)
{
    public override void CreateClassBody(
        StringBuilder methodsSource,
        ClassMetadataBuilder classMetadataBuilder
    )
    {
        this.AddInstanceMethods(methodsSource, classMetadataBuilder);
    }

    protected void AddInstanceMethods(StringBuilder sb, ClassMetadataBuilder classMetadataBuilder)
    {
        MethodBuilder instanceMethodBuilder = new(sb, classMetadataBuilder, Context);
        instanceMethodBuilder.GenerateAllMethods(Context);
    }
}
