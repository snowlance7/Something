using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;

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

        public override void EquipItem()
        {
            LoggerInstance.LogDebug("Item held: " + hasBeenHeld); // TODO: Test this
            if (IsServerOrHost && !hasBeenHeld && !StartOfRound.Instance.inShipPhase)
            {
                if (UnityEngine.Random.Range(0f, 1f) < configCursedPolaroidSomethingChance.Value)
                {
                    SpawnSomething(playerHeldBy);
                }
            }
            base.EquipItem();
        }

        public override void LoadItemSaveData(int saveData)
        {
            hasBeenHeld = saveData == 1;
        }

        public override int GetItemDataToSave()
        {
            return hasBeenHeld ? 1 : 0;
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);

            renderer.enabled = enable;
        }

        public void SpawnSomething(PlayerControllerB playerToHaunt)
        {
            SomethingAI something = Instantiate(SomethingPrefab, Vector3.zero, Quaternion.identity).GetComponent<SomethingAI>();
            something.NetworkObject.Spawn(destroyWithScene: true);
            RoundManager.Instance.SpawnedEnemies.Add(something);
            something.ChangeTargetPlayerClientRpc(playerToHaunt.actualClientId);
            DoAnimationClientRpc("play");
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            ItemAnimator.SetTrigger(animationName);
            ItemAudio.Play();
            playerHeldBy.insanityLevel = 45f;
        }
    }
}