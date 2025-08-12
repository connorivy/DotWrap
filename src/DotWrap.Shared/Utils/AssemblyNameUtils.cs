namespace DotWrap.Utils
{
    public static class AssemblyNameUtils
    {
        /// <summary>
        /// Takes a fully qualified assembly name and redacts the assemblySource, version, culture, and public key token.
        /// This is useful for generating a consistent type identifier that can be loaded from slightly different assemblies.
        /// For example, if you use symbol.Identity in the source generator, you will end up with System.Int32, System.Private.CoreLib, Version=... PublicKeyToken=token1
        /// but if you use typeof(int).AssemblyQualifiedName, you will end up with System.Int32, System.Runtime, Version=... PublicKeyToken=token2
        /// this method will handle several cases
        ///
        /// System.Double, System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a => System.Double
        /// System.Collections.Generic.List`1[[System.Int64, System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]] => System.Collections.Generic.List`1[[System.Int64]]
        /// </summary>
        /// <param name="fullyQualifiedAssemblyName"></param>
        /// <returns></returns>
        public static string GetSimplifiedAssemblyName(string fullyQualifiedAssemblyName)
        {
            if (string.IsNullOrEmpty(fullyQualifiedAssemblyName))
                return fullyQualifiedAssemblyName;

            // Use regex to remove assembly information, keeping only the type name
            var pattern =
                @",\s*[^,\[\]]+,\s*Version=[^,\[\]]*,\s*Culture=[^,\[\]]*,\s*PublicKeyToken=[^,\[\]]*";
            var simplified = System.Text.RegularExpressions.Regex.Replace(
                fullyQualifiedAssemblyName,
                pattern,
                ""
            );

            // Replace backtick followed by any number followed by plus with a period
            simplified = System.Text.RegularExpressions.Regex.Replace(simplified, @"`\d+\+", ".");

            // remove backtick followed by any number
            simplified = System.Text.RegularExpressions.Regex.Replace(simplified, @"`\d+", "");

            return simplified;
        }
    }
}
