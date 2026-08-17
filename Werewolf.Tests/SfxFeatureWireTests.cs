using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class SfxFeatureWireTests
    {
        private static string Meta(string key)
        {
            return typeof(SfxFeatureWireTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        [Theory]
        [InlineData("sfx_bell_five_minutes")]
        [InlineData("sfx_chat_message")]
        public void RequestedClips_AreEmbeddedSupportedPcm16Wav(string clipKey)
        {
            using var module = ModuleDefinition.ReadModule(Meta("Werewolf.ModDllPath"));
            var resource = module.Resources.OfType<EmbeddedResource>()
                .SingleOrDefault(r => r.Name == "Werewolf.Assets." + clipKey + ".wav");
            Assert.NotNull(resource);

            using Stream stream = resource.GetResourceStream();
            using var reader = new BinaryReader(stream);
            Assert.Equal("RIFF", new string(reader.ReadChars(4)));
            reader.ReadUInt32();
            Assert.Equal("WAVE", new string(reader.ReadChars(4)));

            ushort format = 0;
            ushort channels = 0;
            uint sampleRate = 0;
            ushort bits = 0;
            uint dataBytes = 0;
            while (stream.Position + 8 <= stream.Length)
            {
                string id = new string(reader.ReadChars(4));
                uint size = reader.ReadUInt32();
                long next = stream.Position + size + (size & 1u);
                Assert.True(next <= stream.Length, $"{clipKey}: WAVチャンクがファイル外へはみ出している");

                if (id == "fmt ")
                {
                    Assert.True(size >= 16, $"{clipKey}: fmt チャンクが短すぎる");
                    format = reader.ReadUInt16();
                    channels = reader.ReadUInt16();
                    sampleRate = reader.ReadUInt32();
                    reader.ReadUInt32();
                    reader.ReadUInt16();
                    bits = reader.ReadUInt16();
                }
                else if (id == "data")
                {
                    dataBytes = size;
                }

                stream.Position = next;
            }

            Assert.Equal((ushort)1, format);
            Assert.InRange(channels, (ushort)1, (ushort)2);
            Assert.True(sampleRate > 0);
            Assert.Equal((ushort)16, bits);
            Assert.True(dataBytes > 0);
        }

        [Fact]
        public void TickRolesClient_ResolvesBellClipFromCrossedMark()
        {
            using var module = ModuleDefinition.ReadModule(Meta("Werewolf.ModDllPath"));
            MethodDefinition method = DirectorMethod(module, "TickRolesClient");

            Assert.Contains(method.Body.Instructions, ins =>
                (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) &&
                ins.Operand is MethodReference mr &&
                mr.DeclaringType.FullName == "Werewolf.Core.BellSchedule" &&
                mr.Name == "ClipKeyFor");
        }

        [Fact]
        public void SharedMeetingChatAppend_PlaysPopOnlyAfterAcceptedAppend()
        {
            using var module = ModuleDefinition.ReadModule(Meta("Werewolf.ModDllPath"));
            MethodDefinition method = DirectorMethod(module, "AppendMeetingChatMessageClient");
            var instructions = method.Body.Instructions;

            int append = IndexOfCall(instructions, "Werewolf.Core.MeetingChatLog", "Append");
            int clip = instructions.Select((ins, index) => (ins, index))
                .Where(x => x.ins.OpCode == OpCodes.Ldstr &&
                            (string)x.ins.Operand == "sfx_chat_message")
                .Select(x => x.index)
                .SingleOrDefault(-1);
            int play = IndexOfCall(instructions, "Werewolf.UI.SfxPlayer", "Play");

            Assert.True(append >= 0, "共通チャット追加経路から MeetingChatLog.Append が消えている");
            Assert.True(clip > append, "チャット音がログ追加より前、または配線されていない");
            Assert.True(play > clip, "sfx_chat_message が SfxPlayer.Play へ渡されていない");
            Assert.Contains(instructions.Skip(append + 1).Take(clip - append - 1),
                ins => ins.OpCode.FlowControl == FlowControl.Cond_Branch);
        }

        [Fact]
        public void TickMeetingClient_OpensDiscussionOnlyAfterTaxmanPost()
        {
            using var module = ModuleDefinition.ReadModule(Meta("Werewolf.ModDllPath"));
            MethodDefinition method = DirectorMethod(module, "TickMeetingClient");
            var instructions = method.Body.Instructions;

            int taxmanPost = IndexOfCall(instructions,
                "Werewolf.Game.WerewolfDirector", "PostMeetingChatSystemLines");
            int openDiscussion = IndexOfCall(instructions,
                "Werewolf.Core.MeetingClientState", "MarkDiscussionOpen");

            Assert.True(taxmanPost >= 0, "会議冒頭のTaxman投稿配線が消えている");
            Assert.True(openDiscussion > taxmanPost,
                "議論受付がTaxman投稿より前、または明示的に開かれていない");
        }

        [Theory]
        [InlineData("RecordMeetingChatClient")]
        [InlineData("DebugInjectChatCore")]
        public void RealAndDebugChat_UseSharedAppendAndSfxPath(string methodName)
        {
            using var module = ModuleDefinition.ReadModule(Meta("Werewolf.ModDllPath"));
            MethodDefinition method = DirectorMethod(module, methodName);

            Assert.Contains(method.Body.Instructions, ins =>
                (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) &&
                ins.Operand is MethodReference mr &&
                mr.DeclaringType.FullName == "Werewolf.Game.WerewolfDirector" &&
                mr.Name == "AppendMeetingChatMessageClient");
        }

        private static MethodDefinition DirectorMethod(ModuleDefinition module, string name)
        {
            TypeDefinition director = module.GetType("Werewolf.Game.WerewolfDirector");
            Assert.NotNull(director);
            MethodDefinition method = director.Methods.SingleOrDefault(m => m.Name == name);
            Assert.NotNull(method);
            Assert.True(method.HasBody, $"{name} に IL Body が無い");
            return method;
        }

        private static int IndexOfCall(Mono.Collections.Generic.Collection<Instruction> instructions,
                                       string declaringType, string methodName)
        {
            return instructions.Select((ins, index) => (ins, index))
                .Where(x => (x.ins.OpCode == OpCodes.Call || x.ins.OpCode == OpCodes.Callvirt) &&
                            x.ins.Operand is MethodReference mr &&
                            mr.DeclaringType.FullName == declaringType && mr.Name == methodName)
                .Select(x => x.index)
                .SingleOrDefault(-1);
        }
    }
}
