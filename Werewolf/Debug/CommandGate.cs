using System;

namespace Werewolf.Debugging
{
    public enum CommandGateVerdict
    {
        Allowed = 0,

        RejectedNotHost = 1,

        RejectedDebugModeDisabled = 2,
    }

    public static class CommandGate
    {
        private static readonly char[] Separators = { ' ', '\t' };

        public static bool TryParse(string message, out string command, out string[] args)
        {
            command = "";
            args = Array.Empty<string>();

            if (string.IsNullOrWhiteSpace(message)) return false;

            string[] tokens = message.Trim().Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0 || !string.Equals(tokens[0], "/ww", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (tokens.Length >= 2)
            {
                command = tokens[1].ToLowerInvariant();
            }
            if (tokens.Length >= 3)
            {
                args = new string[tokens.Length - 2];
                Array.Copy(tokens, 2, args, 0, args.Length);
            }
            return true;
        }

        public static CommandGateVerdict Decide(string command, bool isHost, bool debugMode)
        {
            if (!isHost) return CommandGateVerdict.RejectedNotHost;
            if (!debugMode) return CommandGateVerdict.RejectedDebugModeDisabled;
            return CommandGateVerdict.Allowed;
        }
    }
}
