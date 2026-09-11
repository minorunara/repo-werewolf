using System;
using Werewolf.Core;

namespace Werewolf.Debugging
{
    internal static class StructuredLog
    {
        private static readonly SecretLogBuffer DeferredSecrets = new SecretLogBuffer();
        private static readonly RepeatedLogLimiter RepeatedFailures = new RepeatedLogLimiter();

        internal static void Install()
        {
            WLog.Sink = Write;

            Texts.FormatErrorLogger = id =>
                WLog.Line("text_format_error", secret: false, ("id", id));
        }

        private static void Write(string line, bool secret)
        {
            bool debugLogging = IsDebugLoggingEnabled();
            bool debugMode = IsDebugMode();
            WLogLevel level = WLog.LevelOf(line);

            if (level != WLogLevel.Debug)
            {
                var decision = RepeatedFailures.Observe(
                    line, level, secret, DateTime.UtcNow);
                if (decision.HasSummary)
                    Route(decision.Summary.ToLine(), level, secret, debugLogging, debugMode);
                if (!decision.EmitCurrent) return;
            }

            Route(line, level, secret, debugLogging, debugMode);
        }

        private static void Route(
            string line,
            WLogLevel level,
            bool secret,
            bool debugLogging,
            bool debugMode)
        {
            switch (StructuredLogPolicy.Decide(level, secret, debugLogging, debugMode))
            {
                case StructuredLogRoute.Immediate:
                    WriteAt(level, line);
                    break;
                case StructuredLogRoute.Deferred:
                    DeferredSecrets.Add(line, level, DateTime.Now);
                    break;
            }
        }

        private static void WriteAt(WLogLevel level, string line)
        {
            switch (level)
            {
                case WLogLevel.Error:
                    Plugin.Logger?.LogError(line);
                    break;
                case WLogLevel.Warning:
                    Plugin.Logger?.LogWarning(line);
                    break;
                default:
                    Plugin.Logger?.LogDebug(line);
                    break;
            }
        }

        private static bool IsDebugLoggingEnabled()
        {
            ConfigBindings bindings = Plugin.Bindings;
            return bindings != null && bindings.DebugLogging.Value;
        }

        private static bool IsDebugMode()
        {
            GameConfig cfg = Plugin.GameConfig;
            return cfg != null && cfg.DebugMode;
        }

        internal static void FlushDeferredSecrets(string reason)
        {
            if (Plugin.Logger == null) return;

            bool debugLogging = IsDebugLoggingEnabled();
            bool debugMode = IsDebugMode();
            foreach (RepeatedLogSummary summary in RepeatedFailures.DrainAndReset())
            {
                Route(summary.ToLine(), summary.Level, summary.Secret, debugLogging, debugMode);
            }

            var lines = DeferredSecrets.Flush(out int dropped);
            if (lines.Count == 0 && dropped == 0) return;

            WLogLevel blockLevel = dropped > 0 ? WLogLevel.Warning : WLogLevel.Debug;
            foreach (SecretLogEntry entry in lines)
            {
                if (entry.Level > blockLevel) blockLevel = entry.Level;
            }

            WriteAt(blockLevel,
                $"{WLog.Prefix} secretlog_flush reason={reason} count={lines.Count} dropped={dropped}");
            foreach (SecretLogEntry entry in lines) WriteAt(entry.Level, entry.Line);
            WriteAt(blockLevel, $"{WLog.Prefix} secretlog_flush_end");
        }
    }
}
