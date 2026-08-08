using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using Werewolf.Core;

namespace Werewolf
{
    internal static class GameRefs
    {
        public static AccessTools.FieldRef<PlayerAvatar, bool> PlayerAvatar_deadSet;
        public static AccessTools.FieldRef<PlayerAvatar, bool> PlayerAvatar_isDisabled;
        public static AccessTools.FieldRef<PlayerAvatar, bool> PlayerAvatar_isTumbling;
        public static AccessTools.FieldRef<PlayerAvatar, bool> PlayerAvatar_levelAnimationCompleted;
        public static AccessTools.FieldRef<PlayerAvatar, PlayerDeathHead> PlayerAvatar_playerDeathHead;
        public static AccessTools.FieldRef<PlayerAvatar, PlayerVoiceChat> PlayerAvatar_voiceChat;
        public static AccessTools.FieldRef<PlayerAvatar, bool> PlayerAvatar_voiceChatFetched;
        public static AccessTools.FieldRef<PlayerAvatar, PlayerTumble> PlayerAvatar_tumble;
        public static AccessTools.FieldRef<PlayerHealth, int> PlayerHealth_health;
        public static AccessTools.FieldRef<PlayerTumble, PhysGrabObject> PlayerTumble_physGrabObject;
        public static AccessTools.FieldRef<PlayerDeathHead, PhysGrabObject> PlayerDeathHead_physGrabObject;

        public static AccessTools.FieldRef<PlayerVoiceChat, bool> PlayerVoiceChat_isTalking;
        public static AccessTools.FieldRef<PlayerVoiceChat, AudioSource> PlayerVoiceChat_audioSource;
        public static AccessTools.FieldRef<PlayerVoiceChat, Recorder> PlayerVoiceChat_recorder;
        public static AccessTools.FieldRef<PlayerVoiceChat, bool> PlayerVoiceChat_TTSinstantiated;
        public static AccessTools.FieldRef<PlayerVoiceChat, float> PlayerVoiceChat_overrideMuteTimer;
        public static AccessTools.FieldRef<PlayerVoiceChat, float> PlayerVoiceChat_clipLoudness;
        public static AccessTools.FieldRef<PlayerVoiceChat, PlayerAvatar> PlayerVoiceChat_playerAvatar;

        public static AccessTools.FieldRef<RunManager, Level> RunManager_debugLevel;
        public static AccessTools.FieldRef<RunManager, bool> RunManager_restarting;
        public static AccessTools.FieldRef<RunManager, List<PlayerVoiceChat>> RunManager_voiceChats;
        public static AccessTools.FieldRef<RunManager, float> RunManager_cosmeticWorldObjectCooldown;
        public static AccessTools.FieldRef<RunManager, bool> RunManager_moonLevelChanged;
        public static AccessTools.FieldRef<RoundDirector, int> RoundDirector_extractionPoints;
        public static AccessTools.FieldRef<RoundDirector, int> RoundDirector_extractionPointsCompleted;
        public static AccessTools.FieldRef<RoundDirector, bool> RoundDirector_allExtractionPointsCompleted;
        public static AccessTools.FieldRef<RoundDirector, int> RoundDirector_haulGoal;
        public static AccessTools.FieldRef<RoundDirector, int> RoundDirector_extractionPointSurplus;
        public static AccessTools.FieldRef<ExtractionPoint, ExtractionPoint.State> ExtractionPoint_currentState;
        public static AccessTools.FieldRef<StatsManager, string> StatsManager_saveFileCurrent;
        public static AccessTools.FieldRef<StatsManager,
            SortedDictionary<string, Dictionary<string, int>>> StatsManager_dictionaryOfDictionaries;
        public static AccessTools.FieldRef<PunManager, PhotonView> PunManager_photonView;
        public static AccessTools.FieldRef<MetaManager, List<int>> MetaManager_cosmeticTokens;

        public static AccessTools.FieldRef<InputManager, Dictionary<InputKey, InputAction>> InputManager_inputActions;
        public static AccessTools.FieldRef<DataDirector, bool> DataDirector_toggleMute;
        public static AccessTools.FieldRef<ChatManager, string> ChatManager_chatMessage;
        public static AccessTools.FieldRef<MenuPlayerListed, PlayerAvatar> MenuPlayerListed_playerAvatar;
        public static AccessTools.FieldRef<MenuPlayerListed, int> MenuPlayerListed_listSpot;
        public static AccessTools.FieldRef<MenuPlayerHead, bool> MenuPlayerHead_isTalking;
        public static AccessTools.FieldRef<MenuPlayerHead, RectTransform> MenuPlayerHead_headTransform;
        public static AccessTools.FieldRef<PlayerCrown, PlayerAvatar> PlayerCrown_playerAvatar;
        public static AccessTools.FieldRef<PlayerCrown, bool> PlayerCrown_active;
        public static AccessTools.FieldRef<WorldSpaceUIPlayerName, PlayerAvatar> WorldSpaceUIPlayerName_playerAvatar;
        public static AccessTools.FieldRef<MapToolController, bool> MapToolController_Active;
        public static AccessTools.FieldRef<TruckScreenText, TruckScreenText.PlayerChatBoxState> TruckScreenText_playerChatBoxState;

        public static AccessTools.FieldRef<PlayerController, bool> PlayerController_JumpFirst;
        public static AccessTools.FieldRef<PlayerController, int> PlayerController_JumpExtraCurrent;
        public static AccessTools.FieldRef<ItemMelee, HurtCollider> ItemMelee_hurtCollider;
        public static AccessTools.FieldRef<PhysGrabber, PhysGrabObject> PhysGrabber_grabbedPhysGrabObject;
        public static AccessTools.FieldRef<PhysGrabObject, bool> PhysGrabObject_isMelee;
        public static AccessTools.FieldRef<ValuableObject, float> ValuableObject_dollarValueCurrent;
        public static AccessTools.FieldRef<ValuableObject, float> ValuableObject_dollarValueOriginal;
        public static AccessTools.FieldRef<ValuableObject, int> ValuableObject_dollarValueOverride;
        public static AccessTools.FieldRef<ValuableObject, bool> ValuableObject_discovered;
        public static AccessTools.FieldRef<PhysGrabObjectImpactDetector, ValuableObject> PhysGrabObjectImpactDetector_valuableObject;
        public static AccessTools.FieldRef<PhysGrabObjectImpactDetector, PhysGrabObject> PhysGrabObjectImpactDetector_physGrabObject;

        public static AccessTools.FieldRef<EnemyParent, Enemy> EnemyParent_Enemy;
        public static AccessTools.FieldRef<EnemyParent, bool> EnemyParent_Spawned;
        public static AccessTools.FieldRef<Enemy, EnemyRigidbody> Enemy_Rigidbody;
        public static AccessTools.FieldRef<Enemy, bool> Enemy_HasRigidbody;
        public static AccessTools.FieldRef<MapCustom, MapCustomEntity> MapCustom_mapCustomEntity;
        public static AccessTools.FieldRef<EnemyDirector, List<string>> EnemyDirector_debugNoVision;
        public static AccessTools.FieldRef<EnemyShadow, PlayerAvatar> EnemyShadow_playerTarget;
        public static MethodInfo EnemyShadow_UpdatePlayerTarget;
        public static AccessTools.FieldRef<AudioManager, AudioManager.SoundSnapshot> AudioManager_currentSnapshot;
        public static FieldInfo ParticleScriptExplosion_explosionPrefab;
        public static FieldInfo ParticlePrefabExplosion_HurtColliderSecondSetup;

        private static List<string> s_failures;
        private static int s_resolvedCount;

        public static bool ResolveAll()
        {
            s_failures = new List<string>();
            s_resolvedCount = 0;

            PlayerAvatar_deadSet = Field<PlayerAvatar, bool>("deadSet");
            PlayerAvatar_isDisabled = Field<PlayerAvatar, bool>("isDisabled");
            PlayerAvatar_isTumbling = Field<PlayerAvatar, bool>("isTumbling");
            PlayerAvatar_levelAnimationCompleted = Field<PlayerAvatar, bool>("levelAnimationCompleted");
            PlayerAvatar_playerDeathHead = Field<PlayerAvatar, PlayerDeathHead>("playerDeathHead");
            PlayerAvatar_voiceChat = Field<PlayerAvatar, PlayerVoiceChat>("voiceChat");
            PlayerAvatar_voiceChatFetched = Field<PlayerAvatar, bool>("voiceChatFetched");
            PlayerAvatar_tumble = Field<PlayerAvatar, PlayerTumble>("tumble");
            PlayerHealth_health = Field<PlayerHealth, int>("health");
            PlayerTumble_physGrabObject = Field<PlayerTumble, PhysGrabObject>("physGrabObject");
            PlayerDeathHead_physGrabObject = Field<PlayerDeathHead, PhysGrabObject>("physGrabObject");

            PlayerVoiceChat_isTalking = Field<PlayerVoiceChat, bool>("isTalking");
            PlayerVoiceChat_audioSource = Field<PlayerVoiceChat, AudioSource>("audioSource");
            PlayerVoiceChat_recorder = Field<PlayerVoiceChat, Recorder>("recorder");
            PlayerVoiceChat_TTSinstantiated = Field<PlayerVoiceChat, bool>("TTSinstantiated");
            PlayerVoiceChat_overrideMuteTimer = Field<PlayerVoiceChat, float>("overrideMuteTimer");
            PlayerVoiceChat_clipLoudness = Field<PlayerVoiceChat, float>("clipLoudness");
            PlayerVoiceChat_playerAvatar = Field<PlayerVoiceChat, PlayerAvatar>("playerAvatar");

            RunManager_debugLevel = Field<RunManager, Level>("debugLevel");
            RunManager_restarting = Field<RunManager, bool>("restarting");
            RunManager_voiceChats = Field<RunManager, List<PlayerVoiceChat>>("voiceChats");
            RunManager_cosmeticWorldObjectCooldown = Field<RunManager, float>("cosmeticWorldObjectCooldown");
            RunManager_moonLevelChanged = Field<RunManager, bool>("moonLevelChanged");
            RoundDirector_extractionPoints = Field<RoundDirector, int>("extractionPoints");
            ExtractionPoint_currentState = Field<ExtractionPoint, ExtractionPoint.State>("currentState");
            RoundDirector_extractionPointsCompleted = Field<RoundDirector, int>("extractionPointsCompleted");
            RoundDirector_allExtractionPointsCompleted = Field<RoundDirector, bool>("allExtractionPointsCompleted");
            RoundDirector_haulGoal = Field<RoundDirector, int>("haulGoal");
            RoundDirector_extractionPointSurplus = Field<RoundDirector, int>("extractionPointSurplus");
            StatsManager_saveFileCurrent = Field<StatsManager, string>("saveFileCurrent");
            StatsManager_dictionaryOfDictionaries = Field<StatsManager,
                SortedDictionary<string, Dictionary<string, int>>>("dictionaryOfDictionaries");
            PunManager_photonView = Field<PunManager, PhotonView>("photonView");
            MetaManager_cosmeticTokens = Field<MetaManager, List<int>>("cosmeticTokens");

            InputManager_inputActions = Field<InputManager, Dictionary<InputKey, InputAction>>("inputActions");
            DataDirector_toggleMute = Field<DataDirector, bool>("toggleMute");
            ChatManager_chatMessage = Field<ChatManager, string>("chatMessage");
            MenuPlayerListed_playerAvatar = Field<MenuPlayerListed, PlayerAvatar>("playerAvatar");
            MenuPlayerListed_listSpot = Field<MenuPlayerListed, int>("listSpot");
            MenuPlayerHead_isTalking = Field<MenuPlayerHead, bool>("isTalking");
            MenuPlayerHead_headTransform = Field<MenuPlayerHead, RectTransform>("headTransform");
            PlayerCrown_playerAvatar = Field<PlayerCrown, PlayerAvatar>("playerAvatar");
            PlayerCrown_active = Field<PlayerCrown, bool>("active");
            WorldSpaceUIPlayerName_playerAvatar = Field<WorldSpaceUIPlayerName, PlayerAvatar>("playerAvatar");
            MapToolController_Active = Field<MapToolController, bool>("Active");
            TruckScreenText_playerChatBoxState =
                Field<TruckScreenText, TruckScreenText.PlayerChatBoxState>("playerChatBoxState");

            PlayerController_JumpFirst = Field<PlayerController, bool>("JumpFirst");
            PlayerController_JumpExtraCurrent = Field<PlayerController, int>("JumpExtraCurrent");
            ItemMelee_hurtCollider = Field<ItemMelee, HurtCollider>("hurtCollider");
            PhysGrabber_grabbedPhysGrabObject = Field<PhysGrabber, PhysGrabObject>("grabbedPhysGrabObject");
            PhysGrabObject_isMelee = Field<PhysGrabObject, bool>("isMelee");
            ValuableObject_dollarValueCurrent = Field<ValuableObject, float>("dollarValueCurrent");
            ValuableObject_dollarValueOriginal = Field<ValuableObject, float>("dollarValueOriginal");
            ValuableObject_dollarValueOverride = Field<ValuableObject, int>("dollarValueOverride");
            ValuableObject_discovered = Field<ValuableObject, bool>("discovered");
            PhysGrabObjectImpactDetector_valuableObject =
                Field<PhysGrabObjectImpactDetector, ValuableObject>("valuableObject");
            PhysGrabObjectImpactDetector_physGrabObject =
                Field<PhysGrabObjectImpactDetector, PhysGrabObject>("physGrabObject");

            EnemyParent_Enemy = Field<EnemyParent, Enemy>("Enemy");
            EnemyParent_Spawned = Field<EnemyParent, bool>("Spawned");
            Enemy_Rigidbody = Field<Enemy, EnemyRigidbody>("Rigidbody");
            Enemy_HasRigidbody = Field<Enemy, bool>("HasRigidbody");
            MapCustom_mapCustomEntity = Field<MapCustom, MapCustomEntity>("mapCustomEntity");
            EnemyDirector_debugNoVision = Field<EnemyDirector, List<string>>("debugNoVision");
            EnemyShadow_playerTarget = Field<EnemyShadow, PlayerAvatar>("playerTarget");
            EnemyShadow_UpdatePlayerTarget = RawMethod(typeof(EnemyShadow), "UpdatePlayerTarget");
            AudioManager_currentSnapshot = Field<AudioManager, AudioManager.SoundSnapshot>("currentSnapshot");
            ParticleScriptExplosion_explosionPrefab = RawField(typeof(ParticleScriptExplosion), "explosionPrefab");
            ParticlePrefabExplosion_HurtColliderSecondSetup =
                RawField(typeof(ParticlePrefabExplosion), "HurtColliderSecondSetup");

            if (s_failures.Count == 0)
            {
                WLog.Line("game_refs_resolved", secret: false, ("count", s_resolvedCount));
                return true;
            }
            WLog.Line("game_refs_disabled", secret: false, ("failedFields", s_failures.Count));
            return false;
        }

        private static AccessTools.FieldRef<TObj, TField> Field<TObj, TField>(string fieldName)
            where TObj : class
        {
            try
            {
                var fieldRef = AccessTools.FieldRefAccess<TObj, TField>(fieldName);
                s_resolvedCount++;
                return fieldRef;
            }
            catch (Exception e)
            {
                Fail(typeof(TObj).Name, fieldName, e.Message);
                return null;
            }
        }

        private static MethodInfo RawMethod(Type type, string methodName)
        {
            MethodInfo method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                Fail(type.Name, methodName, "method not found");
                return null;
            }
            s_resolvedCount++;
            return method;
        }

        private static FieldInfo RawField(Type type, string fieldName)
        {
            FieldInfo field = AccessTools.Field(type, fieldName);
            if (field == null)
            {
                Fail(type.Name, fieldName, "field not found");
                return null;
            }
            s_resolvedCount++;
            return field;
        }

        private static void Fail(string typeName, string fieldName, string error)
        {
            s_failures.Add($"{typeName}.{fieldName}");
            WLog.Line("game_ref_failed", secret: false,
                ("type", typeName), ("field", fieldName), ("err", error));
        }
    }
}
