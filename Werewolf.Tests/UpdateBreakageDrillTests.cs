using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

namespace Werewolf.Tests
{
    public class UpdateBreakageDrillTests
    {
        private const string BrokenMemberType = "PlayerHealth";
        private const string BrokenMemberName = "InvincibleSet";
        private const string BrokenMemberOwner = "Werewolf.Vanilla.LocalPlayer";

        private const string BrokenPatchTargetType = "PlayerAvatar";
        private const string BrokenPatchTargetName = "PlayerDeathRPC";
        private const string BrokenPatchClass = "Werewolf.Game.Patches.PlayerDeathPatch";

        [Fact]
        public void RenamedGameMembers_ArePointedAtTheirGatewayOwnerAndPatchClass()
        {
            string drillDir = Path.Combine(Path.GetTempPath(), $"WerewolfDrill_{Guid.NewGuid():N}");
            Directory.CreateDirectory(drillDir);
            try
            {
                WriteMutatedGameAssembly(drillDir, out string renamedMember, out string renamedTarget);
                AuditAgainst(drillDir);

                Assert.NotEqual(BrokenMemberName, renamedMember);
                Assert.NotEqual(BrokenPatchTargetName, renamedTarget);
            }
            finally
            {
                Directory.Delete(drillDir, recursive: true);
            }
        }

        private static void AuditAgainst(string drillDir)
        {
            using var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(drillDir);
            resolver.AddSearchDirectory(AuditEnvironment.Meta("Werewolf.GameManagedDir"));
            resolver.AddSearchDirectory(AuditEnvironment.Meta("Werewolf.BepInExCoreDir"));
            resolver.AddSearchDirectory(AuditEnvironment.Meta("Werewolf.MenuLibDir"));
            resolver.AddSearchDirectory(Path.GetDirectoryName(AuditEnvironment.Meta("Werewolf.ModDllPath")));

            using var mod = ModuleDefinition.ReadModule(AuditEnvironment.Meta("Werewolf.ModDllPath"),
                new ReaderParameters { AssemblyResolver = resolver });

            var refFailures = GameRefAudit.FindUnresolvedRefs(mod, AuditEnvironment.InAuditScope);
            Assert.True(refFailures.Count == 1,
                "壊した参照は1件のはず（監査が別の参照まで落としている、または拾えていない）:\n  "
                + string.Join("\n  ", refFailures));
            Assert.Contains($"{BrokenMemberType}::{BrokenMemberName}(", refFailures[0]);
            Assert.Contains($"<- {BrokenMemberOwner}", refFailures[0]);

            var targets = HarmonyPatchAudit.Enumerate(mod);
            var patchFailures = HarmonyPatchAudit.FindUnresolvedTargets(targets, AuditEnvironment.InAuditScope);
            Assert.True(patchFailures.Count == 1,
                "壊したパッチ対象は1件のはず:\n  " + string.Join("\n  ", patchFailures));
            Assert.Contains(BrokenPatchClass, patchFailures[0]);
            Assert.Contains(BrokenPatchTargetName, patchFailures[0]);
        }

        private static void WriteMutatedGameAssembly(string dir, out string renamedMember, out string renamedTarget)
        {
            string gamePath = Path.Combine(AuditEnvironment.Meta("Werewolf.GameManagedDir"), "Assembly-CSharp.dll");
            Assert.True(File.Exists(gamePath), $"本体 DLL が見つからない: {gamePath}");

            using var game = AssemblyDefinition.ReadAssembly(gamePath,
                new ReaderParameters { AssemblyResolver = AuditEnvironment.CreateResolver() });

            var member = game.MainModule.GetType(BrokenMemberType)?.Methods
                .SingleOrDefault(m => m.Name == BrokenMemberName);
            Assert.True(member != null, $"{BrokenMemberType}.{BrokenMemberName} が本体に無い（ドリルの前提が古い）");
            member.Name = renamedMember = BrokenMemberName + "__drill";

            var target = game.MainModule.GetType(BrokenPatchTargetType)?.Methods
                .SingleOrDefault(m => m.Name == BrokenPatchTargetName);
            Assert.True(target != null, $"{BrokenPatchTargetType}.{BrokenPatchTargetName} が本体に無い（ドリルの前提が古い）");
            target.Name = renamedTarget = BrokenPatchTargetName + "__drill";

            game.Write(Path.Combine(dir, "Assembly-CSharp.dll"));
        }
    }
}
