namespace DotWrap.Generator.Extensions;

using DotWrap.Configuration;
using Microsoft.CodeAnalysis;

public static class IParameterSymbolExtensions
{
    extension(IParameterSymbol paramSymbol)
    {
        public ParameterSpecialCaseFlags GetSpecialCaseFlags()
        {
            var flags = ParameterSpecialCaseFlags.None;
            if (paramSymbol.RefKind == RefKind.Out)
            {
                flags |= ParameterSpecialCaseFlags.Out;
            }
            return flags;
        }

        public string GetExposedType(out bool isOriginalType)
        {
            if (paramSymbol.RefKind == RefKind.Out)
            {
                isOriginalType = false;
                return "IntPtr";
            }

            return paramSymbol.Type.GetExposedType(out isOriginalType);
        }
    }
}
