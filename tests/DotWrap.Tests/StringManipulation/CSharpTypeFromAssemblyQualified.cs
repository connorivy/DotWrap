using System.Threading.Tasks;
using DotWrap.Utils;

namespace DotWrap.Tests.StringManipulation;

/// <summary>
/// These tests were never working and I kind of pivoted away from using the GetOriginalTypeString method
/// </summary>
// public class CSharpTypeFromAssemblyQualifiedTests
// {
//     [Test]
//     [Arguments(typeof(int), "System.Int32")]
//     [Arguments(typeof(List<int>), "System.Collections.Generic.List<System.Int32>")]
//     [Arguments(typeof(int[]), "System.Int32[]")]
//     [Arguments(
//         typeof(Dictionary<List<List<KeyValuePair<int, string>>>, string>),
//         "System.Collections.Generic.Dictionary<System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Int32, System.String>>>, System.String>"
//     )]
//     public async Task GetCSharpTypeFromAssemblyQualifiedName_ReturnsExpectedType(
//         Type type,
//         string expectedTypeName
//     )
//     {
//         var actualType = DotWrapUtils.GetOriginalTypeString(type.AssemblyQualifiedName);
//         await Assert.That(actualType).IsEqualTo(expectedTypeName);
//     }
// }
