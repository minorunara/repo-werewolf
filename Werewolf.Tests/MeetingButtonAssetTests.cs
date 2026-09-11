using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingButtonAssetTests
    {
        private const string ResolverType = "Werewolf.Vanilla.MeetingButtonAssetResolver";
        private const string ConstName = "KnownModuleName";

        [Fact]
        public void KnownShopModule_IsStillReferencedByNameInLevelAssets()
        {
            string dataDir = AuditEnvironment.GameDataDir();
            Assert.True(Directory.EnumerateFiles(dataDir, "*.assets").Any()
                        && File.Exists(Path.Combine(dataDir, "globalgamemanagers")),
                $"本体のアセットファイル（*.assets / globalgamemanagers）が {dataDir} に無い。"
                + "tools/Sync-GameData.ps1 で Steam 実体からコピーする。");

            string moduleName = ReadKnownModuleName();
            var hits = new List<(string file, string resourcePath)>();
            foreach (string path in Directory.EnumerateFiles(dataDir, "*.assets"))
            {
                byte[] data = File.ReadAllBytes(path);
                foreach (string resourcePath in FindPrefabRefs(data, moduleName))
                    hits.Add((Path.GetFileName(path), resourcePath));
                Assert.Empty(FindPrefabRefs(data, moduleName + " NoSuch_Probe"));
            }

            Assert.True(hits.Count > 0,
                $"プレハブ名 \"{moduleName}\" を持つ PrefabRef が本体の Level 資産（{dataDir} の *.assets）に無い。"
                + "本体アップデートでショップモジュールが改名・削除された可能性があり、MeetingButtonAssetResolver は"
                + "全走査へ落ちてラウンド開始が止まる。resources.assets 内の \"Module - Shop\" 系プレハブ名から"
                + "shopButton を持つモジュールを探し、KnownModuleName を差し替える。");

            string container = File.ReadAllText(Path.Combine(dataDir, "globalgamemanagers"), Encoding.Latin1);
            var unregistered = hits
                .Select(h => h.resourcePath)
                .Distinct(StringComparer.Ordinal)
                .Where(rp => container.IndexOf(rp.ToLowerInvariant(), StringComparison.Ordinal) < 0)
                .ToList();
            Assert.True(unregistered.Count == 0,
                "PrefabRef は在るが resourcePath が Resources コンテナ（globalgamemanagers）に登録されていない"
                + "（Resources.Load が失敗し全走査へ落ちる）:\n  " + string.Join("\n  ", unregistered));
        }

        [Fact]
        public void FindPrefabRefs_MatchesOnlyAdjacentNameAndPath()
        {
            const string name = "Module - Probe";
            byte[] positive = Concat(
                Aligned("junk"), Aligned(name), Aligned("Level/Shop/Modules/" + name), new byte[12], Aligned("tail"));
            Assert.Equal(new[] { "Level/Shop/Modules/" + name }, FindPrefabRefs(positive, name));

            byte[] nameOnly = Concat(Aligned("junk"), Aligned(name), new byte[16]);
            Assert.Empty(FindPrefabRefs(nameOnly, name));

            byte[] wrongPath = Concat(Aligned(name), Aligned("Level/Shop/Modules/Other"));
            Assert.Empty(FindPrefabRefs(wrongPath, name));

            byte[] bare = Concat(Encoding.UTF8.GetBytes(name), Aligned("Level/Shop/Modules/" + name));
            Assert.Empty(FindPrefabRefs(bare, name));
        }

        [Fact]
        public void KnownModuleName_IsReadFromTheBuiltDll()
        {
            string name = ReadKnownModuleName();
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.StartsWith("Module - Shop", name, StringComparison.Ordinal);
        }

        private static string ReadKnownModuleName()
        {
            using var mod = ModuleDefinition.ReadModule(AuditEnvironment.Meta("Werewolf.ModDllPath"),
                new ReaderParameters { AssemblyResolver = AuditEnvironment.CreateResolver() });
            var type = mod.GetType(ResolverType);
            Assert.NotNull(type);
            var field = type.Fields.SingleOrDefault(f => f.Name == ConstName && f.HasConstant);
            Assert.NotNull(field);
            return (string)field.Constant;
        }

        internal static List<string> FindPrefabRefs(byte[] data, string name)
        {
            var found = new List<string>();
            byte[] needle = Encoding.UTF8.GetBytes(name);
            ReadOnlySpan<byte> span = data;
            int from = 0;
            while (true)
            {
                int rel = span.Slice(from).IndexOf(needle);
                if (rel < 0) break;
                int at = from + rel;
                from = at + 1;

                if (at < 4 || ReadU32(data, at - 4) != needle.Length) continue;
                int pathLenAt = Align4(at + needle.Length);
                if (pathLenAt + 4 > data.Length) continue;
                int pathLen = ReadU32(data, pathLenAt);
                if (pathLen < needle.Length || pathLen > 512 || pathLenAt + 4 + pathLen > data.Length) continue;
                ReadOnlySpan<byte> path = span.Slice(pathLenAt + 4, pathLen);
                if (!path.EndsWith(needle)) continue;
                if (pathLen > needle.Length && path[pathLen - needle.Length - 1] != (byte)'/') continue;
                found.Add(Encoding.UTF8.GetString(path));
            }
            return found;
        }

        private static int ReadU32(byte[] d, int at)
            => d[at] | (d[at + 1] << 8) | (d[at + 2] << 16) | (d[at + 3] << 24);

        private static int Align4(int n) => (n + 3) & ~3;

        private static byte[] Aligned(string s)
        {
            byte[] body = Encoding.UTF8.GetBytes(s);
            byte[] outp = new byte[Align4(4 + body.Length)];
            outp[0] = (byte)body.Length;
            outp[1] = (byte)(body.Length >> 8);
            Buffer.BlockCopy(body, 0, outp, 4, body.Length);
            return outp;
        }

        private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
    }
}
