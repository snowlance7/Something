using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;
using HarmonyLib;
using BepInEx.Logging;
using static Something.Patches;

namespace Something.Items.Polaroids
{
    public class CursedPolaroidBehavior : PhysicsProp
    {
#pragma warning disable CS8618
        public AudioSource ItemAudio;
        public AudioClip SomethingSFX;
        public Animator ItemAnimator;
        public GameObject SomethingPrefab;
        public SpriteRenderer renderer;
#pragma warning restore CS8618

        bool wasHeld;
        public PlayerControllerB? cursedPlayer;

        public override void Start()
        {
            base.Start();
            onShipLanded?.AddListener(OnShipLanded);
        }

        public override void EquipItem()
        {
            logger.LogDebug("Item held: " + wasHeld); // TODO: Test this
            if (IsServer && !wasHeld)
            {
                cursedPlayer = playerHeldBy;
                SpawnSomething(playerHeldBy, !configMinimalSpoilerVersion.Value);
            }

            wasHeld = true;

            base.EquipItem();
        }

        public override int GetItemDataToSave()
        {
            if (cursedPlayer == null)
                return -1;
            return (int)cursedPlayer.actualClientId;
        }

        public override void LoadItemSaveData(int saveData)
        {
            wasHeld = true;
            if (saveData == -1) { return; }
            cursedPlayer = PlayerFromId((ulong)saveData);
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

        public void OnShipLanded()
        {
            if (!IsServer) { return; }
            if (cursedPlayer == null) { return; }
            SpawnSomething(cursedPlayer, false);
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            ItemAnimator.SetTrigger(animationName);
            ItemAudio.Play();
        }
    }
}