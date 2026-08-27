using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Werewolf.Core;

namespace Werewolf.Tests
{
    public static class HarmonyPatchAudit
    {
        private const string PatchAttribute = "HarmonyLib.HarmonyPatch";

        private static readonly string[] PatchMethodAttributes =
        {
            "HarmonyLib.HarmonyPrefix", "HarmonyLib.HarmonyPostfix",
            "HarmonyLib.HarmonyTranspiler", "HarmonyLib.HarmonyFinalizer",
            "HarmonyLib.HarmonyReversePatch",
        };

        private static readonly string[] PatchMethodNames =
            { "Prefix", "Postfix", "Transpiler", "Finalizer" };

        public sealed class Target
        {
            public string PatchClass;

            public string PatchMethod;

            public TypeReference DeclaringType;

            public string MethodName;

            public IReadOnlyList<TypeReference> ArgumentTypes;

            public string MethodType;

            public bool IsManual;

            public bool IsScenarioTier => PatchTierPolicy.IsScenarioTier(PatchClass);

            public string TierLabel => IsScenarioTier ? "チュートリアル層" : "本編層";

            public override string ToString()
            {
                var args = ArgumentTypes == null
                    ? ""
                    : "(" + string.Join(", ", ArgumentTypes.Select(a => a?.FullName ?? "?")) + ")";
                var kind = MethodType != null && MethodType != "Normal" ? $" [{MethodType}]" : "";
                var via = PatchMethod != null ? $".{PatchMethod}" : "";
                return $"{TierLabel} {PatchClass}{via} -> "
                       + $"{DeclaringType?.FullName ?? "?"}.{MethodName ?? "?"}{args}{kind}";
            }
        }

        public static List<Target> Enumerate(ModuleDefinition module)
        {
            var targets = new List<Target>();
            foreach (var type in CecilTypes.AllTypes(module))
            {
                var classAttrs = PatchAttributes(type.CustomAttributes);
                if (classAttrs.Count == 0) continue;

                var manual = type.Methods.Any(m => m.Name == "TargetMethod" || m.Name == "TargetMethods");
                var classInfo = new Target { PatchClass = type.FullName, IsManual = manual };
                foreach (var attr in classAttrs) Merge(classInfo, attr);

                var perMethod = type.Methods
                    .Where(IsPatchMethod)
                    .Select(m => (method: m, attrs: PatchAttributes(m.CustomAttributes)))
                    .Where(x => x.attrs.Count > 0)
                    .ToList();

                if (perMethod.Count == 0)
                {
                    targets.Add(classInfo);
                    continue;
                }

                foreach (var (method, attrs) in perMethod)
                {
                    var merged = Clone(classInfo);
                    merged.PatchMethod = method.Name;
                    foreach (var attr in attrs) Merge(merged, attr);
                    targets.Add(merged);
                }
            }
            targets.Sort((a, b) => string.CompareOrdinal(a.ToString(), b.ToString()));
            return targets;
        }

        public static List<string> FindUnresolvedTargets(
            IEnumerable<Target> targets, Func<string, bool> inScope)
        {
            var failures = new List<string>();
            foreach (var target in targets)
            {
                if (target.IsManual) continue;
                var scope = target.DeclaringType?.Scope?.Name ?? "";
                if (target.DeclaringType != null && !inScope(scope)) continue;

                var reason = Verify(target);
                if (reason != null) failures.Add($"{target} … {reason}");
            }
            failures.Sort(StringComparer.Ordinal);
            return failures;
        }

        private static string Verify(Target target)
        {
            if (target.DeclaringType == null) return "対象型が属性から決まらない";

            TypeDefinition declaring;
            try
            {
                declaring = target.DeclaringType.Resolve();
            }
            catch (AssemblyResolutionException e)
            {
                return $"アセンブリ未発見: {e.AssemblyReference?.Name}";
            }
            if (declaring == null) return "対象型が本体アセンブリに無い";

            var kind = target.MethodType ?? "Normal";
            string wanted;
            switch (kind)
            {
                case "Normal": wanted = target.MethodName; break;
                case "Getter": wanted = "get_" + target.MethodName; break;
                case "Setter": wanted = "set_" + target.MethodName; break;
                case "Constructor": wanted = ".ctor"; break;
                case "StaticConstructor": wanted = ".cctor"; break;
                default: return $"MethodType {kind} は静的監査対象外（実機確認が要る）";
            }
            if (wanted == null) return "対象メソッド名が属性から決まらない";

            var candidates = declaring.Methods.Where(m => m.Name == wanted).ToList();
            if (candidates.Count == 0)
            {
                var inherited = FindInBaseTypes(declaring, wanted);
                if (inherited != null)
                {
                    return $"{inherited} でのみ宣言（Harmony の DeclaredMethod は基底型を見ない）";
                }
                return "同名メソッドが無い";
            }

            if (target.ArgumentTypes == null)
            {
                return candidates.Count > 1
                    ? $"同名の多重定義が {candidates.Count} 件（argumentTypes 無しでは AmbiguousMatch で落ちる）: "
                      + string.Join(" / ", candidates.Select(Signature))
                    : null;
            }

            var wantedArgs = target.ArgumentTypes.Select(a => a?.FullName ?? "?").ToList();
            var hit = candidates.Any(m =>
                m.Parameters.Count == wantedArgs.Count
                && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(wantedArgs));
            return hit
                ? null
                : "argumentTypes に一致する多重定義が無い。現物: "
                  + string.Join(" / ", candidates.Select(Signature));
        }

        private static string FindInBaseTypes(TypeDefinition type, string name)
        {
            for (var baseRef = type.BaseType; baseRef != null;)
            {
                TypeDefinition baseDef;
                try
                {
                    baseDef = baseRef.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    return null;
                }
                if (baseDef == null) return null;
                if (baseDef.Methods.Any(m => m.Name == name)) return baseDef.FullName;
                baseRef = baseDef.BaseType;
            }
            return null;
        }

        private static string Signature(MethodDefinition method)
        {
            return $"{method.Name}({string.Join(", ", method.Parameters.Select(p => p.ParameterType.FullName))})";
        }

        private static List<CustomAttribute> PatchAttributes(IEnumerable<CustomAttribute> attributes)
        {
            return attributes.Where(a => a.AttributeType.FullName == PatchAttribute).ToList();
        }

        private static bool IsPatchMethod(MethodDefinition method)
        {
            return PatchMethodNames.Contains(method.Name)
                   || method.CustomAttributes.Any(a => PatchMethodAttributes.Contains(a.AttributeType.FullName));
        }

        private static Target Clone(Target source)
        {
            return new Target
            {
                PatchClass = source.PatchClass,
                DeclaringType = source.DeclaringType,
                MethodName = source.MethodName,
                ArgumentTypes = source.ArgumentTypes,
                MethodType = source.MethodType,
                IsManual = source.IsManual,
            };
        }

        private static void Merge(Target target, CustomAttribute attr)
        {
            foreach (var arg in attr.ConstructorArguments)
            {
                var argType = arg.Type.FullName;
                if (argType == "System.Type" && arg.Value is TypeReference type)
                {
                    target.DeclaringType = type;
                }
                else if (argType == "System.Type[]" && arg.Value is CustomAttributeArgument[] types)
                {
                    target.ArgumentTypes = types.Select(t => t.Value as TypeReference).ToList();
                }
                else if (argType == "System.String" && arg.Value is string name)
                {
                    target.MethodName = name;
                }
                else if (argType == "HarmonyLib.MethodType")
                {
                    target.MethodType = EnumName(arg.Type, arg.Value);
                }
            }
        }

        private static string EnumName(TypeReference enumType, object value)
        {
            try
            {
                var def = enumType.Resolve();
                var field = def?.Fields.FirstOrDefault(f => f.HasConstant && Equals(f.Constant, value));
                if (field != null) return field.Name;
            }
            catch (AssemblyResolutionException)
            {
            }
            return Convert.ToString(value);
        }
    }
}
