using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Werewolf.Tests
{
    public static class GameRefAudit
    {
        public static List<string> FindUnresolvedRefs(ModuleDefinition module, Func<string, bool> auditScope)
        {
            var failures = new List<string>();

            foreach (var typeRef in module.GetTypeReferences())
            {
                var scope = ScopeName(typeRef);
                if (!auditScope(scope)) continue;
                if (!CanResolve(() => typeRef.Resolve() != null, out var reason))
                    failures.Add($"type {typeRef.FullName} @ {scope}{reason}");
            }

            foreach (var memberRef in module.GetMemberReferences())
            {
                var scope = ScopeName(memberRef.DeclaringType);
                if (!auditScope(scope)) continue;
                if (!CanResolve(() => ResolveMember(memberRef), out var reason))
                    failures.Add($"member {memberRef.FullName} @ {scope}{reason}");
            }

            failures.Sort(StringComparer.Ordinal);
            return failures;
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
