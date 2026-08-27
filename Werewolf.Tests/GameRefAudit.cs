using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Werewolf.Tests
{
    public static class GameRefAudit
    {
        public static List<string> FindUnresolvedRefs(ModuleDefinition module, Func<string, bool> auditScope)
        {
            var failures = new List<string>();
            Dictionary<string, SortedSet<string>> owners = null;

            foreach (var typeRef in module.GetTypeReferences())
            {
                var scope = ScopeName(typeRef);
                if (!auditScope(scope)) continue;
                if (!CanResolve(() => typeRef.Resolve() != null, out var reason))
                    failures.Add($"type {typeRef.FullName} @ {scope}{reason}{OwnersSuffix(module, typeRef.FullName, ref owners)}");
            }

            foreach (var memberRef in module.GetMemberReferences())
            {
                var scope = ScopeName(memberRef.DeclaringType);
                if (!auditScope(scope)) continue;
                if (!CanResolve(() => ResolveMember(memberRef), out var reason))
                    failures.Add($"member {memberRef.FullName} @ {scope}{reason}{OwnersSuffix(module, memberRef.FullName, ref owners)}");
            }

            failures.Sort(StringComparer.Ordinal);
            return failures;
        }

        private static string OwnersSuffix(ModuleDefinition module, string refFullName,
            ref Dictionary<string, SortedSet<string>> owners)
        {
            owners ??= OwnersByRef(module);
            return owners.TryGetValue(refFullName, out var set)
                ? $" <- {string.Join(", ", set)}"
                : "";
        }

        public static Dictionary<string, SortedSet<string>> OwnersByRef(ModuleDefinition module)
        {
            var owners = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            foreach (var type in CecilTypes.AllTypes(module))
            {
                TypeDefinition root = type;
                while (root.DeclaringType != null) root = root.DeclaringType;
                foreach (var name in ReferencedNames(type))
                {
                    if (!owners.TryGetValue(name, out var set))
                        owners[name] = set = new SortedSet<string>(StringComparer.Ordinal);
                    set.Add(root.FullName);
                }
            }
            return owners;
        }

        private static IEnumerable<string> ReferencedNames(TypeDefinition type)
        {
            foreach (var n in Flatten(type.BaseType)) yield return n;
            foreach (var i in type.Interfaces)
            foreach (var n in Flatten(i.InterfaceType))
                yield return n;
            foreach (var f in type.Fields)
            foreach (var n in Flatten(f.FieldType))
                yield return n;

            foreach (var m in type.Methods)
            {
                foreach (var n in Flatten(m.ReturnType)) yield return n;
                foreach (var p in m.Parameters)
                foreach (var n in Flatten(p.ParameterType))
                    yield return n;
                if (!m.HasBody) continue;

                foreach (var v in m.Body.Variables)
                foreach (var n in Flatten(v.VariableType))
                    yield return n;

                foreach (var instr in m.Body.Instructions)
                {
                    switch (instr.Operand)
                    {
                        case TypeReference t:
                            foreach (var n in Flatten(t)) yield return n;
                            break;
                        case MemberReference member:
                            yield return member.FullName;
                            foreach (var n in Flatten(member.DeclaringType)) yield return n;
                            if (member is MethodReference method)
                            {
                                foreach (var n in Flatten(method.ReturnType)) yield return n;
                                foreach (var p in method.Parameters)
                                foreach (var n in Flatten(p.ParameterType))
                                    yield return n;
                            }
                            else if (member is FieldReference field)
                            {
                                foreach (var n in Flatten(field.FieldType)) yield return n;
                            }
                            break;
                    }
                }
            }
        }

        private static IEnumerable<string> Flatten(TypeReference type)
        {
            if (type == null) yield break;
            yield return type.FullName;
            if (type is GenericInstanceType generic)
            {
                foreach (var arg in generic.GenericArguments)
                foreach (var n in Flatten(arg))
                    yield return n;
            }
            else if (type is TypeSpecification spec)
            {
                foreach (var n in Flatten(spec.ElementType)) yield return n;
            }
        }

        private static string ScopeName(TypeReference typeRef)
        {
            return typeRef?.Scope?.Name ?? "";
        }

        private static bool ResolveMember(MemberReference memberRef)
        {
            switch (memberRef)
            {
                case MethodReference method:
                    return method.Resolve() != null;
                case FieldReference field:
                    return field.Resolve() != null;
                default:
                    return true;
            }
        }

        private static bool CanResolve(Func<bool> resolve, out string reason)
        {
            try
            {
                reason = "";
                return resolve();
            }
            catch (AssemblyResolutionException e)
            {
                reason = $" (アセンブリ未発見: {e.AssemblyReference?.Name})";
                return false;
            }
        }
    }
}
