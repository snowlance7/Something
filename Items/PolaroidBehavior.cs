using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;

namespace Something.Items
{
    public class PolaroidBehavior : PhysicsProp
    {
#pragma warning disable CS8618
        public AudioSource audioSource;
        public GameObject somethingPrefab;
        public SpriteRenderer renderer;
        public Animator animator;

        public Sprite[] goodPhotos;
        public Sprite[] altGoodPhotos;
        public Sprite[] badPhotos;
        public Sprite[] altBadPhotos;
#pragma warning restore CS8618

        int photoType;
        int photoIndex;

        bool wasHeld;

        int goodWeight; // 0
        int badWeight; // 1
        int cursedWeight; // 2
        float badSomethingChance = 0.5f;
        // bool isGlobalAudio = ContentHandler<EnemyHandler>.Instance.DuckSong.GetConfig<bool>("Duck | Global Spawn Audio").Value; // TODO

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer || wasHeld)
                return;

            photoType = RoundManager.Instance.GetRandomWeightedIndex([goodWeight, badWeight, cursedWeight]);

            if (photoType != 2) // not cursed
            {
                var photos = (photoType == 0) ? goodPhotos : badPhotos;
                photoIndex = UnityEngine.Random.Range(0, photos.Length);
            }

            ChangeSpriteClientRpc(photoType, photoIndex);
        }


        public override void EquipItem() // Synced
        {
            base.EquipItem();

            if (!wasHeld && playerHeldBy == localPlayer)
            {
                bool spawnSomething =
                    photoType == 2 ||
                    (photoType == 1 && UnityEngine.Random.value < badSomethingChance);

                if (spawnSomething)
                {
                    audioSource.Play();

                    if (photoType == 2)
                        animator.SetTrigger("play");

                    SpawnSomething(playerHeldBy);
                }
            }

            wasHeld = true;
        }


        public void SpawnSomething(PlayerControllerB playerToHaunt)
        {
            if (!IsServer) { return; }
            if (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }
            SomethingAI something = Instantiate(somethingPrefab, Vector3.zero, Quaternion.identity).GetComponent<SomethingAI>();
            something.NetworkObject.Spawn(destroyWithScene: true);
            RoundManager.Instance.SpawnedEnemies.Add(something);
            something.ChangeTargetPlayerClientRpc(playerToHaunt.actualClientId);
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);
            renderer.enabled = enable;
        }

        public override void LoadItemSaveData(int saveData)
        {
            wasHeld = true;
            Decode(saveData, out int type, out int index);
            ChangeSprite(type, index);
        }

        public override int GetItemDataToSave()
        {
            return Encode(photoType, photoIndex);
        }

        public int GetAltPhotoIndex()
        {
            if (photoType == 2)
                return -1;

            int src = photoType == 0 ? goodPhotos.Length : badPhotos.Length;
            int dst = photoType == 0 ? altGoodPhotos.Length : altBadPhotos.Length;

            return photoIndex * dst / src;
        }

        int Encode(int type, int index)
        {
            return (type << 8) | index;
        }

        void Decode(int value, out int type, out int index)
        {
            index = value & 0xFF;        // lower 8 bits
            type = (value >> 8) & 0x3;  // next 2 bits
        }

        public void ChangeSprite(int type, int index)
        {
            photoType = type;
            photoIndex = index;

            if (photoType == 2)
                return; // Cursed

            bool useAlt = configMinimalSpoilerVersion.Value;
            int finalIndex = useAlt ? GetAltPhotoIndex() : index;

            Sprite[] source = photoType == 0
                ? (useAlt ? altGoodPhotos : goodPhotos)
                : (useAlt ? altBadPhotos : badPhotos);

            renderer.sprite = source[finalIndex];
        }

        [ClientRpc]
        public void ChangeSpriteClientRpc(int type, int index)
        {
            ChangeSprite(type, index);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnSomethingServerRpc(ulong hauntedPlayerId)
        {
            if (!IsServer) return;
            SpawnSomething(PlayerFromId(hauntedPlayerId));
        }
    }
}