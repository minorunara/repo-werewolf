using System;
using Werewolf.Core;

namespace Werewolf.Debugging
{
    internal static class StructuredLog
    {
        private static readonly SecretLogBuffer DeferredSecrets = new SecretLogBuffer();

        internal static void Install()
        {
            WLog.Sink = Write;

            Texts.FormatErrorLogger = id =>
                WLog.Line("text_format_error", secret: false, ("id", id));
        }

        private static void Write(string line, bool secret)
        {
            if (!secret || IsDebugMode())
            {
                Plugin.Logger?.LogInfo(line);
                return;
            }

            DeferredSecrets.Add(line, DateTime.Now);
        }

        private static bool IsDebugMode()
        {
            GameConfig cfg = Plugin.GameConfig;
            return cfg != null && cfg.DebugMode;
        }

        internal static void FlushDeferredSecrets(string reason)
        {
            var log = Plugin.Logger;
            if (log == null) return;

            var lines = DeferredSecrets.Flush(out int dropped);
            if (lines.Count == 0 && dropped == 0) return;

            log.LogInfo($"{WLog.Prefix} secretlog_flush reason={reason} count={lines.Count} dropped={dropped}");
            foreach (string line in lines)
            {
                log.LogInfo(line);
            }
            log.LogInfo($"{WLog.Prefix} secretlog_flush_end");
        }
    }
}
