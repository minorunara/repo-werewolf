using System;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using Werewolf.Core;

namespace Werewolf.Game
{
    internal static class CosmeticCoinApplier
    {
        internal enum ApplyRoute
        {
            Primary,

            RecoveredSaveOnly,

            FallbackDirect,
        }

        private static readonly AccessTools.FieldRef<MetaManager, List<int>> CosmeticTokensRef =
            GameRefs.MetaManager_cosmeticTokens;

        internal static ApplyRun BeginRun(int[] countsByRarity)
        {
            if (countsByRarity == null || countsByRarity.Length != CoinRarity.Count)
            {
                WLog.Line("cosmetic_grant_apply_failed", secret: false,
                    ("actor", LocalActorNumber()),
                    ("counts", countsByRarity ?? Array.Empty<int>()),
                    ("err", "invalid_counts"));
                return null;
            }
            return new ApplyRun(countsByRarity);
        }

        internal sealed class ApplyRun
        {
            private readonly int[] _plannedCounts;
            private readonly int[] _remaining;
            private int _primaryCount;
            private int _recoveredSaveOnlyCount;
            private int _fallbackDirectCount;
            private string _firstError;
            private bool _finished;
            private bool _success;

            internal ApplyRun(int[] countsByRarity)
            {
                _plannedCounts = (int[])countsByRarity.Clone();
                _remaining = (int[])countsByRarity.Clone();
            }

            internal int TotalRemaining
            {
                get
                {
                    int sum = 0;
                    for (int i = 0; i < _remaining.Length; i++)
                    {
                        if (_remaining[i] > 0) sum += _remaining[i];
                    }
                    return sum;
                }
            }

            internal bool HasRemaining => TotalRemaining > 0;

            internal bool Finished => _finished;

            internal bool TryApplyNext(out byte rarity, out string outcome)
            {
                rarity = 0;
                outcome = null;
                for (byte r = 0; r < _remaining.Length; r++)
                {
                    if (_remaining[r] <= 0) continue;
                    _remaining[r]--;
                    rarity = r;
                    outcome = ApplyOneCoin(r);
                    return true;
                }
                return false;
            }

            internal void FlushRemaining()
            {
                while (TryApplyNext(out _, out _)) { }
            }

            internal bool Finish()
            {
                if (_finished) return _success;
                _finished = true;

                if (_firstError == null)
                {
                    WLog.Line("cosmetic_grant_applied", secret: false,
                        ("actor", LocalActorNumber()), ("counts", _plannedCounts),
                        ("primary", _primaryCount),
                        ("recovered_save_only", _recoveredSaveOnlyCount),
                        ("fallback_direct", _fallbackDirectCount));
                    _success = true;
                }
                else
                {
                    WLog.Line("cosmetic_grant_apply_failed", secret: false,
                        ("actor", LocalActorNumber()), ("counts", _plannedCounts),
                        ("err", _firstError));
                    _success = false;
                }
                return _success;
            }

            private string ApplyOneCoin(byte rarity)
            {
                try
                {
                    var instance = MetaManager.instance;
                    if (instance == null)
                    {
                        RecordError(rarity, "no_metamanager");
                        return "error:no_metamanager";
                    }

                    string err = ApplyOne(instance, rarity, out ApplyRoute route);
                    if (err != null)
                    {
                        RecordError(rarity, err);
                        return "error:" + err;
                    }

                    switch (route)
                    {
                        case ApplyRoute.Primary:
                            _primaryCount++;
                            return "primary";
                        case ApplyRoute.RecoveredSaveOnly:
                            _recoveredSaveOnlyCount++;
                            return "recovered_save_only";
                        default:
                            _fallbackDirectCount++;
                            return "fallback_direct";
                    }
                }
                catch (Exception e)
                {
                    RecordError(rarity, e.Message);
                    return "error:" + e.Message;
                }
            }

            private void RecordError(byte rarity, string err)
            {
                if (_firstError == null)
                {
                    _firstError = "rarity=" + rarity + ":" + err;
                }
            }
        }

        private static string ApplyOne(MetaManager instance, byte rarity, out ApplyRoute route)
        {
            int countBefore = ReadTokenCount(instance);

            try
            {
                instance.CosmeticTokenAdd((SemiFunc.Rarity)rarity);
                route = ApplyRoute.Primary;
                return null;
            }
            catch (Exception primaryError)
            {
                route = ApplyRoute.FallbackDirect;

                int countAfter = ReadTokenCount(instance);
                if (countAfter > countBefore)
                {
                    try
                    {
                        instance.Save();
                        route = ApplyRoute.RecoveredSaveOnly;
                        return null;
                    }
                    catch (Exception saveError)
                    {
                        return "primary_partial(" + primaryError.Message + ") save(" + saveError.Message + ")";
                    }
                }

                if (CosmeticTokensRef == null)
                {
                    return "primary(" + primaryError.Message + ") fallback(field_unresolved)";
                }

                try
                {
                    var tokens = CosmeticTokensRef(instance);
                    if (tokens == null)
                    {
                        return "primary(" + primaryError.Message + ") fallback(tokens_null)";
                    }

                    tokens.Add((int)rarity);
                    instance.Save();
                    route = ApplyRoute.FallbackDirect;
                    return null;
                }
                catch (Exception fallbackError)
                {
                    return "primary(" + primaryError.Message + ") fallback(" + fallbackError.Message + ")";
                }
            }
        }

        private static int ReadTokenCount(MetaManager instance)
        {
            try
            {
                return CosmeticTokensRef != null ? (CosmeticTokensRef(instance)?.Count ?? -1) : -1;
            }
            catch
            {
                return -1;
            }
        }

        private static int LocalActorNumber()
        {
            try
            {
                return PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null
                    ? PhotonNetwork.LocalPlayer.ActorNumber
                    : -1;
            }
            catch
            {
                return -1;
            }
        }
    }
}
