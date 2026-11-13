#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf
{
    internal static class LanguageBinding
    {
        private static string? _appliedCode;
        private static bool _probeErrorLogged;

        internal static void Tick()
        {
            string? code;
            try
            {
                code = GameLocale.CodeOrNull;
            }
            catch (Exception e)
            {
                if (!_probeErrorLogged)
                {
                    _probeErrorLogged = true;
                    WLog.Exception("lang_probe_error", e, secret: false);
                }
                return;
            }
            if (code == null || string.Equals(code, _appliedCode, StringComparison.Ordinal)) return;
            _appliedCode = code;
            Apply(code);
        }

        private static void Apply(string code)
        {
            LanguagePolicy.Choice choice = LanguagePolicy.Resolve(code);

            IReadOnlyDictionary<TextId, string>? table = null;
            string fileState;
            try
            {
                table = TryLoadLangFile(choice.FileCode, out fileState);
            }
            catch (Exception e)
            {
                fileState = "error:" + e.GetType().Name;
            }

            try
            {
                Texts.Apply(choice.Language, table);
            }
            catch (Exception e)
            {
                WLog.Exception("lang_apply_error", e, secret: false, ("language", choice.Language));
            }

            WLog.Line("lang_applied", secret: false,
                ("locale", code), ("language", choice.Language),
                ("file", choice.FileCode + ".txt"), ("file_state", fileState));
        }

        private static IReadOnlyDictionary<TextId, string>? TryLoadLangFile(string fileCode, out string fileState)
        {
            string dllPath = typeof(LanguageBinding).Assembly.Location;
            string? dir = string.IsNullOrEmpty(dllPath) ? null : Path.GetDirectoryName(dllPath);
            if (string.IsNullOrEmpty(dir))
            {
                fileState = "no_dll_dir";
                return null;
            }

            string path = Path.Combine(dir, "Lang", fileCode + ".txt");
            if (!File.Exists(path))
            {
                fileState = "missing";
                return null;
            }

            fileState = "loaded";
            return LangFile.Parse(File.ReadAllText(path, Encoding.UTF8));
        }
    }
}
