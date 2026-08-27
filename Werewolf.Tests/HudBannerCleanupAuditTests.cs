using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class HudBannerCleanupAuditTests
    {
        private const string HudPanelTypeName = "Werewolf.UI.HudPanel";

        private static readonly string[] BannerBuilders = { "BuildStaticBanner", "BuildTimerBanner" };

        private static string Meta(string key)
        {
            return typeof(HudBannerCleanupAuditTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        private static TypeDefinition LoadHudPanel()
        {
            string dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            var module = ModuleDefinition.ReadModule(dllPath);
            var type = module.GetTypes().FirstOrDefault(t => t.FullName == HudPanelTypeName);
            Assert.True(type != null, $"{HudPanelTypeName} が見つからない");
            return type;
        }

        private static MethodDefinition MethodOf(TypeDefinition type, string name)
        {
            var method = type.Methods.FirstOrDefault(m => m.Name == name && m.HasBody);
            Assert.True(method != null, $"{type.Name}.{name} が見つからない");
            return method;
        }

        private static List<string> BannerFields(TypeDefinition hudPanel)
        {
            var fields = new List<string>();
            string pending = null;
            foreach (var il in MethodOf(hudPanel, "Build").Body.Instructions)
            {
                if (il.OpCode == OpCodes.Call || il.OpCode == OpCodes.Callvirt)
                {
                    pending = il.Operand is MethodReference callee && BannerBuilders.Contains(callee.Name)
                        ? callee.Name
                        : null;
                    continue;
                }
                if (il.OpCode != OpCodes.Stfld) continue;
                if (pending != null && il.Operand is FieldReference field) fields.Add(field.Name);
                pending = null;
            }
            return fields;
        }

        private static HashSet<string> FieldsRead(MethodDefinition method)
        {
            return new HashSet<string>(method.Body.Instructions
                .Where(il => il.OpCode == OpCodes.Ldfld)
                .Select(il => ((FieldReference)il.Operand).Name));
        }

        private static HashSet<string> FieldsDestroyed(MethodDefinition method)
        {
            var destroyed = new HashSet<string>();
            string lastLoaded = null;
            foreach (var il in method.Body.Instructions)
            {
                if (il.OpCode == OpCodes.Ldfld)
                {
                    lastLoaded = ((FieldReference)il.Operand).Name;
                    continue;
                }
                if (il.OpCode != OpCodes.Call && il.OpCode != OpCodes.Callvirt) continue;
                if (il.Operand is MethodReference callee
                    && callee.Name == "Destroy"
                    && callee.DeclaringType.FullName == "UnityEngine.Object"
                    && lastLoaded != null)
                {
                    destroyed.Add(lastLoaded);
                }
                lastLoaded = null;
            }
            return destroyed;
        }

        [Fact]
        public void Build_CreatesBannerRowsOutsideTheHudRoot()
        {
            Assert.NotEmpty(BannerFields(LoadHudPanel()));
        }

        [Fact]
        public void Hide_TurnsOffEveryBannerRow()
        {
            TypeDefinition hudPanel = LoadHudPanel();
            HashSet<string> hidden = FieldsRead(MethodOf(hudPanel, "Hide"));
            foreach (string field in BannerFields(hudPanel))
            {
                Assert.True(hidden.Contains(field), $"HudPanel.Hide が {field} を消していない");
            }
        }

        [Fact]
        public void Destroy_DestroysEveryBannerRow()
        {
            TypeDefinition hudPanel = LoadHudPanel();
            HashSet<string> destroyed = FieldsDestroyed(MethodOf(hudPanel, "Destroy"));
            foreach (string field in BannerFields(hudPanel))
            {
                Assert.True(destroyed.Contains(field), $"HudPanel.Destroy が {field} を破棄していない");
            }
        }
    }
}
