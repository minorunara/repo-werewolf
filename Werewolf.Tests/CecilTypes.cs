using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Werewolf.Tests
{
    internal static class CecilTypes
    {
        public static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition module)
            => module.Types.SelectMany(t => AllTypes(t));

        public static IEnumerable<TypeDefinition> AllTypes(TypeDefinition type)
        {
            yield return type;
            foreach (TypeDefinition nested in type.NestedTypes)
            foreach (TypeDefinition inner in AllTypes(nested))
                yield return inner;
        }
    }
}
