using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;
using HarmonyLib;
using BepInEx.Logging;

namespace Something.Items.Polaroids
{
    public class CursedPolaroidBehavior : PhysicsProp
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AudioSource ItemAudio;
        public AudioClip SomethingSFX;
        public Animator ItemAnimator;
        public GameObject SomethingPrefab;
        public SpriteRenderer renderer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        bool wasHeld;
        public PlayerControllerB? cursedPlayer;

        public override void EquipItem()
        {
            LoggerInstance.LogDebug("Item held: " + wasHeld); // TODO: Test this
            if (IsServer && !wasHeld)
            {
                if (UnityEngine.Random.Range(0f, 1f) < configCursedPolaroidSomethingChance.Value)
                {
                    cursedPlayer = playerHeldBy;
                    SpawnSomething(playerHeldBy, !configSpoilerFreeVersion.Value);
                }
            }

            wasHeld = true;

            base.EquipItem();
        }

        public override void LoadItemSaveData(int saveData)
        {
            wasHeld = true;
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);

            renderer.enabled = enable;
        }

        public void SpawnSomething(PlayerControllerB playerToHaunt, bool playAnim)
        {
            if (!IsServer) { return; }
            if (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }
            SomethingAI something = Instantiate(SomethingPrefab, Vector3.zero, Quaternion.identity).GetComponent<SomethingAI>();
            something.NetworkObject.Spawn(destroyWithScene: true);
            RoundManager.Instance.SpawnedEnemies.Add(something);
            something.ChangeTargetPlayerClientRpc(playerToHaunt.actualClientId);

            if (playAnim)
            {
                DoAnimationClientRpc("play");
            }
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            ItemAnimator.SetTrigger(animationName);
            ItemAudio.Play();
        }
    }

    [HarmonyPatch]
    public class CursedPolaroidPatches
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
        public static void OnShipLandedMiscEventsPostfix()
        {
            try
            {
                if (!IsServer) { return; }

                foreach (CursedPolaroidBehavior polaroid in Object.FindObjectsOfType<CursedPolaroidBehavior>())
                {
                    if (polaroid.cursedPlayer == null) { continue; }
                    polaroid.SpawnSomething(polaroid.cursedPlayer, false);
                }
            }
            catch (System.Exception e)
            {
                LoggerInstance.LogError(e);
                return;
            }
        }
    }
}