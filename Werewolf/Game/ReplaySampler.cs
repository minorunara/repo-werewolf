using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using Werewolf.Core;
using Werewolf.Core.Replay;
using Werewolf.UI;

namespace Werewolf.Game
{
    internal static class ReplaySampler
    {
        internal static readonly ReplayRecorder Recorder = new ReplayRecorder();

        private static bool _segmentActive;
        private static readonly List<ReplayEntitySample> _samples = new List<ReplayEntitySample>(256);
        private static readonly List<int> _haulScratch = new List<int>(32);

        private static double Now => Time.realtimeSinceStartupAsDouble;

        internal static void Tick()
        {
            WerewolfDirector dir = WerewolfDirector.Instance;
            bool gate = dir != null && dir.IsRoundActiveClient && SemiFunc.RunIsLevel();
            if (gate != _segmentActive)
            {
                _segmentActive = gate;
                MapStillCapture.Invalidate();
                if (gate)
                {
                    BeginSegment(dir);
                }
                else
                {
                    Recorder.EndSegment(Now);
                    WLog.Line("replay_segment_end", secret: false,
                        ("segments", Recorder.SegmentCount),
                        ("posEntries", Recorder.PositionEntryCount),
                        ("events", Recorder.EventCount));
                }
            }
            if (!gate) return;
            if (!Recorder.ShouldSample(Now)) return;
            CollectAndSample(dir);
        }

        internal static void ResetAll()
        {
            Recorder.Reset();
            _segmentActive = false;
            MapStillCapture.Invalidate();
        }

        internal static void NoteDeath(int actor, string cause)
            => Recorder.NoteEvent(Now, "death", ("a", actor), ("cause", cause));

        internal static void NoteDeathsAnnounced(List<int> actors)
        {
            if (actors == null || actors.Count == 0) return;
            Recorder.NoteEvent(Now, "announced", ("actors", actors.ToArray()));
        }

        internal static void NoteMeetingStart(int callerActor, string kind)
            => Recorder.NoteEvent(Now, "meet", ("a", callerActor), ("kind", kind));

        internal static void NoteMeetingWarp()
            => Recorder.NoteEvent(Now, "meet_warp");

        internal static void NoteMeetingCancelled(int reason)
            => Recorder.NoteEvent(Now, "meet_cancel", ("reason", reason));

        internal static void NoteChat(int actor, string text)
            => Recorder.NoteEvent(Now, "chat", ("a", actor), ("text", text));

        internal static void NoteMeetingResult(int executedActor)
            => Recorder.NoteEvent(Now, "meeting_result", ("a", executedActor));

        internal static void NotePhase(string phase)
            => Recorder.NoteEvent(Now, "phase", ("to", phase));

        internal static void NoteScatterGroups(List<List<int>> groups)
            => Recorder.NoteEvent(Now, "scatter", ("groups", groups));

        internal static void NoteGuardWindow(int sec)
            => Recorder.NoteEvent(Now, "guard", ("sec", sec));

        internal static void NoteGameOver(int team, int[] actors, byte[] roles)
            => Recorder.NoteEvent(Now, "gameover", ("team", team), ("actors", actors),
                ("roles", roles != null ? Array.ConvertAll(roles, b => (int)b) : null));

        internal static void NoteValueLoss(float dollars, bool isOrb, int vid, bool destroyed)
            => Recorder.NoteLoss(Now, vid, (int)(dollars + 0.5f), isOrb, destroyed);

        internal static int ReplayVid(ValuableObject valuable)
        {
            Photon.Pun.PhotonView view = valuable.GetComponent<Photon.Pun.PhotonView>();
            return view != null && view.ViewID != 0 ? view.ViewID : valuable.GetInstanceID();
        }

        internal static object[] BuildLossLedgerWire() => Recorder.BuildLossLedgerWire();

        internal static void ApplyHostLedger(object[] payload)
        {
            bool ok = Recorder.ApplyLossLedgerWire(payload);
            WLog.Line("replay_ledger_recv", secret: false,
                ("ok", ok), ("segments", Recorder.HostLedgerSegmentCount));
        }

        internal static void NotePerkUnlocked(string perk)
            => Recorder.NoteEvent(Now, "perk", ("perk", perk));

        internal static void NoteExtractionCompleted()
        {
            RoundDirector round = RoundDirector.instance;
            if (round == null) return;
            Recorder.NoteEvent(Now, "extract",
                ("completed", GameRefs.RoundDirector_extractionPointsCompleted(round)),
                ("total", GameRefs.RoundDirector_extractionPoints(round)));
        }

        internal static string DumpToFile()
        {
            AttachMapImageIfAvailable();
            string replayDir = FallbackReplayDir();
            Directory.CreateDirectory(replayDir);
            string path = Path.Combine(replayDir,
                "replay_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".jsonl");
            File.WriteAllLines(path, Recorder.ToJsonLines(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return path;
        }

        private static void AttachMapImageIfAvailable()
        {
            try
            {
                MapStillCapture.Still still = MapStillCapture.GetOrCapture(
                    Plugin.Bindings != null ? Plugin.Bindings.MeetingMapOrthoSize.Value : 15f,
                    Plugin.Bindings != null ? Plugin.Bindings.MeetingMapResolution.Value : 1);
                if (still == null || still.Texture == null) return;
                byte[] png = still.PngBytes;
                if (png == null || png.Length == 0) return;
                Recorder.AttachMapImage(new ReplayMapImage
                {
                    Width = still.Texture.width,
                    Height = still.Texture.height,
                    MinX = still.MinX,
                    MaxX = still.MaxX,
                    MinZ = still.MinZ,
                    MaxZ = still.MaxZ,
                    Png = png,
                });
                WLog.Line("replay_mapimg_attached", secret: false,
                    ("w", still.Texture.width), ("h", still.Texture.height),
                    ("pngKb", png.Length / 1024));
            }
            catch (Exception e)
            {
                WLog.Line("replay_mapimg_error", secret: false, ("err", e.Message));
            }
        }

        internal static ReplayExportReport ExportForUser()
        {
            ReplaySegmentHeader header = Recorder.FirstSegmentHeader;
            if (header == null || Recorder.SegmentCount == 0)
            {
                return new ReplayExportReport { Outcome = ReplayExportOutcome.Empty };
            }

            AttachMapImageIfAvailable();
            string fileName = ReplayExportNaming.FileName(header);
            try
            {
                string dir = ResolveDownloadsDir(out bool toDownloads);
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, fileName);
                if (File.Exists(path))
                {
                    WLog.Line("replay_export", secret: false,
                        ("result", "exists"), ("file", fileName), ("downloads", toDownloads));
                    return new ReplayExportReport
                    {
                        Outcome = ReplayExportOutcome.AlreadyExists,
                        FileName = fileName,
                        ToDownloads = toDownloads,
                    };
                }
                File.WriteAllLines(path, Recorder.ToJsonLines(),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                WLog.Line("replay_export", secret: false,
                    ("result", "saved"), ("file", fileName), ("downloads", toDownloads),
                    ("path", path));
                return new ReplayExportReport
                {
                    Outcome = ReplayExportOutcome.Saved,
                    FileName = fileName,
                    ToDownloads = toDownloads,
                };
            }
            catch (Exception e)
            {
                WLog.Line("replay_export_error", secret: false,
                    ("file", fileName), ("err", e.Message));
                return new ReplayExportReport
                {
                    Outcome = ReplayExportOutcome.Failed,
                    FileName = fileName,
                };
            }
        }

        private static string ResolveDownloadsDir(out bool toDownloads)
        {
            try
            {
                string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(profile))
                {
                    string downloads = Path.Combine(profile, "Downloads");
                    if (Directory.Exists(downloads))
                    {
                        toDownloads = true;
                        return downloads;
                    }
                }
            }
            catch
            {
            }
            toDownloads = false;
            return FallbackReplayDir();
        }

        private static string FallbackReplayDir()
        {
            string dllDir = Path.GetDirectoryName(typeof(ReplaySampler).Assembly.Location);
            return Path.Combine(dllDir, "Replays");
        }

        private static void BeginSegment(WerewolfDirector dir)
        {
            var header = new ReplaySegmentHeader
            {
                LevelName = RunManager.instance != null && RunManager.instance.levelCurrent != null
                    ? RunManager.instance.levelCurrent.name
                    : "?",
                StartedAtIso = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
                IsHost = SemiFunc.IsMasterClientOrSingleplayer(),
                LocalActor = dir.DebugLocalActor,
            };

            List<PlayerAvatar> players = GameDirector.instance != null ? GameDirector.instance.PlayerList : null;
            if (players != null)
            {
                foreach (PlayerAvatar avatar in players)
                {
                    if (avatar == null) continue;
                    int actor = dir.Registry.ResolveActor(avatar);
                    header.Players.Add(new ReplayPlayerInfo
                    {
                        Actor = actor,
                        ParticipantId = dir.IdRoster.IdOf(actor),
                        Name = SemiFunc.PlayerGetName(avatar),
                    });
                }
            }

            foreach (var pair in dir.IdRoster.Entries)
            {
                if (header.Players.Exists(p => p.Actor == pair.Key)) continue;
                header.Players.Add(new ReplayPlayerInfo
                {
                    Actor = pair.Key,
                    ParticipantId = pair.Value,
                    Name = dir.DisplayNameForActor(pair.Key),
                });
            }

            foreach (ExtractionPoint ep in UnityEngine.Object.FindObjectsOfType<ExtractionPoint>())
            {
                ExtractionPoint.State state = GameRefs.ExtractionPoint_currentState(ep);
                Vector3 pos = ep.transform.position;
                header.ExtractionPoints.Add(new ReplayEpInfo
                {
                    Id = ep.GetInstanceID(),
                    State = (byte)state,
                    StateName = state.ToString(),
                    X = pos.x,
                    Y = pos.y,
                    Z = pos.z,
                });
            }

            foreach (ValuableObject valuable in UnityEngine.Object.FindObjectsOfType<ValuableObject>())
            {
                Vector3 pos = valuable.transform.position;
                header.Valuables.Add(new ReplayValuableInfo
                {
                    Id = valuable.GetInstanceID(),
                    Name = CleanName(valuable.gameObject.name),
                    Dollars = (int)GameRefs.ValuableObject_dollarValueCurrent(valuable),
                    Vid = ReplayVid(valuable),
                    X = pos.x,
                    Y = pos.y,
                    Z = pos.z,
                });
            }

            header.Map = CaptureNavMeshMap();

            Recorder.BeginSegment(header, Now);
            WLog.Line("replay_segment_begin", secret: false,
                ("level", header.LevelName),
                ("players", header.Players.Count),
                ("eps", header.ExtractionPoints.Count),
                ("vals", header.Valuables.Count),
                ("mapTris", header.Map != null ? header.Map.Triangles.Length / 3 : 0));
        }

        private static ReplayMapMesh CaptureNavMeshMap()
        {
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            if (tri.vertices == null || tri.vertices.Length == 0 || tri.indices == null) return null;
            var verts = new float[tri.vertices.Length * 3];
            for (int i = 0; i < tri.vertices.Length; i++)
            {
                Vector3 v = tri.vertices[i];
                verts[i * 3] = v.x;
                verts[i * 3 + 1] = v.y;
                verts[i * 3 + 2] = v.z;
            }
            return new ReplayMapMesh { Vertices = verts, Triangles = tri.indices };
        }

        private static void CollectAndSample(WerewolfDirector dir)
        {
            double now = Now;
            _samples.Clear();

            List<PlayerAvatar> players = GameDirector.instance != null ? GameDirector.instance.PlayerList : null;
            if (players != null)
            {
                foreach (PlayerAvatar avatar in players)
                {
                    if (avatar == null) continue;
                    int actor = dir.Registry.ResolveActor(avatar);
                    Vector3 pos = avatar.transform.position;
                    _samples.Add(new ReplayEntitySample(ReplayEntityKind.Player, actor, pos.x, pos.y, pos.z));

                    if (GameRefs.PlayerAvatar_deadSet(avatar)
                        && TruckWarper.TryGetDeathHeadPosition(avatar, out Vector3 headPos))
                    {
                        _samples.Add(new ReplayEntitySample(
                            ReplayEntityKind.Corpse, actor, headPos.x, headPos.y, headPos.z));
                    }
                }
            }

            foreach (EnemyParent parent in UnityEngine.Object.FindObjectsOfType<EnemyParent>())
            {
                if (!GameRefs.EnemyParent_Spawned(parent)) continue;
                Enemy enemy = GameRefs.EnemyParent_Enemy(parent);
                if (enemy == null) continue;
                EnemyRigidbody rigidbody = GameRefs.Enemy_HasRigidbody(enemy) ? GameRefs.Enemy_Rigidbody(enemy) : null;
                Vector3 pos = (rigidbody != null ? rigidbody.transform : enemy.transform).position;
                int id = parent.GetInstanceID();
                Recorder.NoteEntity(now, ReplayEntityKind.Enemy, id, parent.enemyName);
                _samples.Add(new ReplayEntitySample(ReplayEntityKind.Enemy, id, pos.x, pos.y, pos.z));
            }

            foreach (PhysGrabCart cart in UnityEngine.Object.FindObjectsOfType<PhysGrabCart>())
            {
                int id = cart.GetInstanceID();
                Vector3 pos = cart.transform.position;
                Recorder.NoteEntity(now, ReplayEntityKind.Cart, id, CleanName(cart.gameObject.name));
                _samples.Add(new ReplayEntitySample(ReplayEntityKind.Cart, id, pos.x, pos.y, pos.z));
            }

            foreach (ValuableObject valuable in UnityEngine.Object.FindObjectsOfType<ValuableObject>())
            {
                int id = valuable.GetInstanceID();
                Vector3 pos = valuable.transform.position;
                Recorder.NoteEntity(now, ReplayEntityKind.Valuable, id,
                    CleanName(valuable.gameObject.name), ReplayVid(valuable));
                Recorder.NoteValuableValue(now, id, (int)GameRefs.ValuableObject_dollarValueCurrent(valuable));
                _samples.Add(new ReplayEntitySample(ReplayEntityKind.Valuable, id, pos.x, pos.y, pos.z));
            }

            _haulScratch.Clear();
            RoundDirector haulRound = RoundDirector.instance;
            if (haulRound != null && haulRound.dollarHaulList != null)
            {
                foreach (GameObject haulGo in haulRound.dollarHaulList)
                {
                    if (haulGo == null) continue;
                    ValuableObject haulValuable = haulGo.GetComponent<ValuableObject>();
                    if (haulValuable != null) _haulScratch.Add(haulValuable.GetInstanceID());
                }
            }
            Recorder.NoteHaulIds(now, _haulScratch);

            foreach (ItemAttributes item in UnityEngine.Object.FindObjectsOfType<ItemAttributes>())
            {
                int id = item.GetInstanceID();
                Vector3 pos = item.transform.position;
                Recorder.NoteEntity(now, ReplayEntityKind.Item, id, CleanName(item.gameObject.name));
                _samples.Add(new ReplayEntitySample(ReplayEntityKind.Item, id, pos.x, pos.y, pos.z));
            }

            foreach (ExtractionPoint ep in UnityEngine.Object.FindObjectsOfType<ExtractionPoint>())
            {
                ExtractionPoint.State state = GameRefs.ExtractionPoint_currentState(ep);
                Recorder.NoteEpState(now, ep.GetInstanceID(), (byte)state, state.ToString());
            }

            Recorder.Sample(now, _samples);
        }

        private static string CleanName(string goName)
        {
            if (string.IsNullOrEmpty(goName)) return "";
            const string clone = "(Clone)";
            return goName.EndsWith(clone, StringComparison.Ordinal)
                ? goName.Substring(0, goName.Length - clone.Length).TrimEnd()
                : goName;
        }
    }
}
