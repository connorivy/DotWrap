using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Extensions;

public static class IMethodSymbolExtensions
{
    extension(IMethodSymbol methodSymbol)
    {
        /// <summary>
        /// Checks if the constructor is marked with the SetsRequiredMembers attribute.
        /// </summary>
        public bool HasSetsRequiredMembersAttribute()
        {
            return methodSymbol.GetAttributes().Any(attr => 
                attr.AttributeClass?.Name == "SetsRequiredMembersAttribute" ||
                attr.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute");
        }

        /// <summary>
        /// Determines if this constructor needs required members added as parameters.
        /// This is true for constructors without [SetsRequiredMembers] attribute when the containing type has required members.
        /// </summary>
        public bool NeedsRequiredMembersAsParameters()
        {
            if (methodSymbol.MethodKind != MethodKind.Constructor)
            {
                return false;
            }

            // If the constructor already has [SetsRequiredMembers], it doesn't need additional parameters
            if (methodSymbol.HasSetsRequiredMembersAttribute())
            {
                return false;
            }

            // Get required members that aren't satisfied by this constructor
            var requiredMembers = methodSymbol.ContainingType.GetRequiredMembers().ToList();
            if (requiredMembers.Count == 0)
            {
                return false;
            }

            // Check if all required members are already satisfied by existing constructor parameters
            var constructorParamNames = new HashSet<string>(methodSymbol.Parameters.Select(p => p.Name.ToLowerInvariant()));
            var unsatisfiedRequiredMembers = requiredMembers.Where(rm => 
                !constructorParamNames.Contains(rm.Name.ToLowerInvariant())).Any();

            return unsatisfiedRequiredMembers;
        }
    }
}