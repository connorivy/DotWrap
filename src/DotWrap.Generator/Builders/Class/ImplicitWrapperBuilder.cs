using System.Text;

namespace DotWrap.Generator.Builders.Class;

public class ImplicitWrapperBuilder(ClassBuilderContext context)
    : EntryPointStaticClassBuilderBase(context)
{
    // don't add any additional methods beyond the memory management methods added by the base class
    public override void CreateClassBody(
        StringBuilder methodsSource,
        ClassMetadataBuilder classMetadataBuilder
    ) { }
}
