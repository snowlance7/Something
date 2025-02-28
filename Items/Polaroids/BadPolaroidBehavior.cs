using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;

namespace Something.Items.Polaroids
{
    public class BadPolaroidBehavior : PhysicsProp
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AudioSource ItemAudio;
        public GameObject SomethingPrefab;
        public SpriteRenderer renderer;
        public Sprite[] Photos;
        public Sprite[] AltPhotos;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        int photoIndex;
        bool wasHeld;

        public override void Start()
        {
            base.Start();
            if (!IsServerOrHost) { return; }

            if (photoIndex == -1)
            {
                photoIndex = configSpoilerFreeVersion.Value ? Random.Range(0, AltPhotos.Length) : Random.Range(0, Photos.Length);
            }

            ChangeSpriteClientRpc(photoIndex, configSpoilerFreeVersion.Value);
        }

        public override void EquipItem()
        {
            if (IsServerOrHost && !wasHeld)
            {
                if (UnityEngine.Random.Range(0f, 1f) < configBadPolaroidSomethingChance.Value)
                {
                    SpawnSomething(playerHeldBy);
                    ItemAudio.Play();
                }
            }

            wasHeld = true;

            base.EquipItem();
        }

        public void SpawnSomething(PlayerControllerB playerToHaunt)
        {
            if (!IsServerOrHost) { return; }
            if (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }
            SomethingAI something = Instantiate(SomethingPrefab, Vector3.zero, Quaternion.identity).GetComponent<SomethingAI>();
            something.NetworkObject.Spawn(destroyWithScene: true);
            RoundManager.Instance.SpawnedEnemies.Add(something);
            something.ChangeTargetPlayerClientRpc(playerToHaunt.actualClientId);
            PlaySomethingSFXClientRpc();
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);

            renderer.enabled = enable;
        }

        public override void LoadItemSaveData(int saveData)
        {
            photoIndex = saveData;
            wasHeld = true;
        }

        public override int GetItemDataToSave()
        {
            return photoIndex;
        }

        [ClientRpc]
        public void ChangeSpriteClientRpc(int index, bool spoilerFree)
        {
            renderer.sprite = spoilerFree ? AltPhotos[index] : Photos[index];
            photoIndex = index;
        }

        [ClientRpc]
        public void PlaySomethingSFXClientRpc()
        {
            ItemAudio.Play();
        }
    }
}