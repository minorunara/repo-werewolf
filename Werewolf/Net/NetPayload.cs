using System;
using Werewolf.Core;

namespace Werewolf.Net
{
    public static class NetPayload
    {
        public static bool IsAllowedType(Type t) =>
            t == typeof(int) || t == typeof(long) || t == typeof(string) ||
            t == typeof(byte) || t == typeof(byte[]) || t == typeof(int[]) || t == typeof(string[]);

        public static object[] Build(object[] payload)
        {
            if (payload == null) return Array.Empty<object>();

            var result = new object[payload.Length];
            for (int i = 0; i < payload.Length; i++)
            {
                object value = payload[i];
                if (value is bool b)
                {
                    result[i] = (byte)(b ? 1 : 0);
                    continue;
                }
                if (value == null)
                    throw new ArgumentException($"payload[{i}] が null です（送信payloadに null は許可されない）。");
                if (!IsAllowedType(value.GetType()))
                    throw new ArgumentException($"payload[{i}] が未許可の型です: {value.GetType()}。");
                result[i] = value;
            }
            return result;
        }

        public static bool TryDeserialize(byte code, object content, out object[] payload, out string dropReason)
        {
            payload = null;

            if (!EventCodes.IsInRange(code))
                return Drop(code, "badcode", out dropReason);

            Type[] schema = EventCodes.Schema(code);
            if (schema == null)
                return Drop(code, "unimplemented", out dropReason);

            if (!(content is object[] raw))
                return Drop(code, "notarray", out dropReason);

            if (raw.Length != schema.Length)
                return Drop(code, "arity", out dropReason);

            var normalized = new object[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                object value = raw[i];
                if (value is bool b) value = (byte)(b ? 1 : 0);
                if (value == null || value.GetType() != schema[i])
                    return Drop(code, "badtype", out dropReason);
                normalized[i] = value;
            }

            payload = normalized;
            dropReason = null;
            return true;
        }

        private static bool Drop(byte code, string reason, out string dropReason)
        {
            dropReason = reason;
            WLog.Line("drop", secret: false, ("reason", reason), ("code", (int)code));
            return false;
        }
    }
}
