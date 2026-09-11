using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Werewolf.Tests
{
    internal static class AuditEnvironment
    {
        private static readonly string[] AuditScopePrefixes =
            { "Assembly-CSharp", "Photon", "Unity", "BepInEx", "0Harmony", "MenuLib" };

        public static bool InAuditScope(string scopeName)
        {
            return AuditScopePrefixes.Any(p => scopeName.StartsWith(p, StringComparison.Ordinal));
        }

        public static string Meta(string key)
        {
            return typeof(AuditEnvironment).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        public static string GameDataDir()
        {
            string managed = Meta("Werewolf.GameManagedDir").TrimEnd('\\', '/');
            return Path.GetDirectoryName(managed);
        }

        public static DefaultAssemblyResolver CreateResolver()
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Meta("Werewolf.GameManagedDir"));
            resolver.AddSearchDirectory(Meta("Werewolf.BepInExCoreDir"));
            resolver.AddSearchDirectory(Meta("Werewolf.MenuLibDir"));
            resolver.AddSearchDirectory(Path.GetDirectoryName(Meta("Werewolf.ModDllPath")));
            return resolver;
        }
    }
}
