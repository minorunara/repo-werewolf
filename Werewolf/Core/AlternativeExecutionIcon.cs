#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Werewolf.Core
{
    public static class AlternativeExecutionIcon
    {
        public const string DefaultAssetKey = "icon_status_executed";
        public const string AlternativeAssetKey = "icon_status_executed_alternative";

        [return: NotNullIfNotNull(nameof(assetKey))]
        public static string? Resolve(bool enabled, string? assetKey)
        {
            return enabled && string.Equals(assetKey, DefaultAssetKey, StringComparison.Ordinal)
                ? AlternativeAssetKey
                : assetKey;
        }
    }
}
