using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game
{
    internal sealed class RoleRevealController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private readonly RoleRevealCinematic _revealCinematic = new RoleRevealCinematic();

        private readonly BlackCatAwakeningToast _catAwakenToast = new BlackCatAwakeningToast();
        private Coroutine _catAwakenToastCoroutine;

        private bool _revealStarted;
        private bool _awakeningRevealStarted;
        private Coroutine _revealCoroutine;

        private readonly CatAwakenToastGate _catAwakenGate = new CatAwakenToastGate();

        internal RoleRevealController(WerewolfDirector director)
        {
            _d = director;
        }

        internal bool RevealStarted => _revealStarted;

        internal bool CinematicVisible => _revealCinematic.Visible;

        internal void TryStartRoleReveal()
        {
            try
            {
                if (_revealStarted) return;
                if (_d.LocalRoleClient == null) return;
                if (_d.ClientRoundEndUnixMs == 0) return;

                Role role = _d.LocalRoleClient.Value;
                int[] knownWerewolves = _d.KnownWerewolves;
                byte[] knownTeammateRoles = _d.KnownTeammateRoles;
                bool isWolfTeam = role == Role.Werewolf || role == Role.Bomber;
                if (isWolfTeam && knownWerewolves == null && _d.ClientWerewolfCount != 1) return;

                string[] teammateNames;
                if (isWolfTeam && knownWerewolves != null)
                {
                    var list = new List<string>(knownWerewolves.Length);
                    int selfActor = _d.LocalActor;
                    for (int i = 0; i < knownWerewolves.Length; i++)
                    {
                        int a = knownWerewolves[i];
                        if (a == selfActor) continue;
                        string name = _d.ParticipantIdLabel(a);
                        if (knownTeammateRoles != null && i < knownTeammateRoles.Length
                            && (Role)knownTeammateRoles[i] == Role.Bomber)
                        {
                            name = name + "（" + Texts.Get(TextId.RoleNameBomber) + "）";
                        }
                        list.Add(name);
                    }
                    teammateNames = list.ToArray();
                }
                else
                {
                    teammateNames = Array.Empty<string>();
                }

                RevealContent content = RevealScript.Build(role, teammateNames, _d.ClientCatPossible,
                    _d.ClientValuableMapMode, _d.IsBlackCatCurseEnabledForClient(),
                    _d.IdRoster.IdOf(_d.LocalActor));

                _d.EnsurePanelBuilt(_revealCinematic);
                if (!_revealCinematic.Exists)
                {
                    _revealStarted = true;
                    WLog.Line("reveal_skip", secret: false, ("reason", "not_built"));
                    return;
                }

                if (_revealCoroutine != null)
                {
                    _d.StopCoroutine(_revealCoroutine);
                    _revealCoroutine = null;
                }
                _revealCoroutine = _d.StartCoroutine(_revealCinematic.Play(content));
                _revealStarted = true;
                WLog.Line("reveal_start", secret: true,
                    ("role", role), ("catPossible", _d.ClientCatPossible),
                    ("teammates", teammateNames.Length));
            }
            catch (Exception e)
            {
                WLog.Line("reveal_start_error", secret: false, ("err", e.Message));
            }
        }

        public void Tick(long now)
        {
            if (_catAwakenGate.ShouldFire(_d.ClientPhaseMirror, _d.ClientCatPossible,
                    _d.GameStartUnixMsClient, _d.ClientRevealDelaySec, now))
            {
                _d.PushToast(SessionNotice.ForCatAwakened());
                WLog.Line("cat_awaken_toast", secret: false,
                    ("delaySec", _d.ClientRevealDelaySec));
                if (_d.LocalIsVillagerTeam) _d.MaybeShowTutorial(TutorialId.VillagerSeesCatAwakened);
                else if (_d.LocalRoleClient == Role.Werewolf) _d.MaybeShowTutorial(TutorialId.WerewolfSeesCatAwakened);
            }
        }

        internal void TryStartAwakeningReveal()
        {
            try
            {
                if (_awakeningRevealStarted) return;
                if (_d.LocalRoleClient != Role.BlackCat) return;

                RevealContent content = RevealScript.BuildBlackCatAwakening(_d.IsBlackCatCurseEnabledForClient());

                if (_revealCoroutine != null)
                {
                    _d.StopCoroutine(_revealCoroutine);
                    _revealCoroutine = null;
                }
                if (_revealCinematic.Exists) _revealCinematic.HideNow();

                _d.EnsurePanelBuilt(_catAwakenToast);
                if (!_catAwakenToast.Exists)
                {
                    _awakeningRevealStarted = true;
                    WLog.Line("awakening_reveal_skip", secret: false, ("reason", "not_built"));
                    return;
                }

                if (_catAwakenToastCoroutine != null)
                {
                    _d.StopCoroutine(_catAwakenToastCoroutine);
                    _catAwakenToastCoroutine = null;
                }
                _catAwakenToastCoroutine = _d.StartCoroutine(PlayBlackCatAwakeningAndTutorial(content));
                _awakeningRevealStarted = true;
                WLog.Line("awakening_reveal_start", secret: true);
            }
            catch (Exception e)
            {
                WLog.Line("awakening_reveal_error", secret: false, ("err", e.Message));
            }
        }

        private IEnumerator PlayBlackCatAwakeningAndTutorial(RevealContent content)
        {
            yield return _catAwakenToast.Play(content);
            _catAwakenToastCoroutine = null;

            if (_d.LocalRoleClient == Role.BlackCat)
                _d.MaybeShowTutorial(TutorialId.BlackCatRoleDrawn);
        }

        internal bool DebugPlay(RevealContent content)
        {
            _d.EnsurePanelBuilt(_revealCinematic);
            if (!_revealCinematic.Exists) return false;

            if (_revealCoroutine != null)
            {
                _d.StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }
            _revealCoroutine = _d.StartCoroutine(_revealCinematic.Play(content));
            return true;
        }

        internal void HideCinematicNow()
        {
            if (_revealCoroutine != null)
            {
                _d.StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }
            if (_revealCinematic.Exists) _revealCinematic.HideNow();
        }

        public void Reset()
        {
            if (_revealCoroutine != null)
            {
                _d.StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }
            _revealCinematic.HideNow();
            if (_catAwakenToastCoroutine != null)
            {
                _d.StopCoroutine(_catAwakenToastCoroutine);
                _catAwakenToastCoroutine = null;
            }
            _catAwakenToast.HideNow();
            _revealStarted = false;
            _awakeningRevealStarted = false;
            _catAwakenGate.Reset();
        }

        public void DestroyUi()
        {
            _revealCinematic.Destroy();
            _catAwakenToast.Destroy();
        }
    }
}
