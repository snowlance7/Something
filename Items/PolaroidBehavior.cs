using Dawn.Utils;
using Dusk;
using GameNetcodeStuff;
using SnowyLib;
using Something.Enemies.Something;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;

namespace Something.Items
{
    public class PolaroidBehavior : PhysicsProp
    {
        public AudioSource audioSource = null!;
        public SpriteRenderer renderer = null!;
        public Animator animator = null!;

        public Sprite[] goodPhotos = null!;
        public Sprite[] altGoodPhotos = null!;
        public Sprite[] badPhotos = null!;
        public Sprite[] altBadPhotos = null!;

        bool isLocalPlayerCursed;

        int photoType;
        int photoIndex;

        bool wasHeld;

        int goodWeight => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<int>("Good Weight").Value; // 0
        int badWeight => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<int>("Bad Weight").Value; // 1
        int cursedWeight => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<int>("Cursed Weight").Value; // 2
        float badSomethingChance => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<float>("Bad Something Chance").Value;
        BoundedRange goodValue => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<BoundedRange>("Good Value").Value;
        BoundedRange badValue => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<BoundedRange>("Bad Value").Value;
        BoundedRange cursedValue => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<BoundedRange>("Cursed Value").Value;

        public override void Start()
        {
            base.Start();

            if (!IsServer) { return; }

            onShipLanded.AddListener(OnShipLanded);

            if (wasHeld) return;

            photoType = RoundManager.Instance.GetRandomWeightedIndex([goodWeight, badWeight, cursedWeight]);
            BoundedRange range = cursedValue;

            if (photoType != 2) // not cursed
            {
                range = photoType == 0 ? goodValue : badValue;
                var photos = (photoType == 0) ? goodPhotos : badPhotos;
                photoIndex = UnityEngine.Random.Range(0, photos.Length);
            }

            ChangeSpriteClientRpc(photoType, photoIndex, (int)range.GetRandomInRange(Utils.randomLocal));
        }

        void OnShipLanded() // Listener
        {
            if (isLocalPlayerCursed)
            {
                SpawnSomethingServerRpc(localPlayer.actualClientId);
            }
        }

        public override void EquipItem() // Synced
        {
            base.EquipItem();

            if (!wasHeld)
            {
                bool spawnSomething = photoType == 2 || (photoType == 1 && UnityEngine.Random.Range(0f, 1f) < badSomethingChance);

                if (spawnSomething)
                {
                    if (photoType == 2)
                    {
                        string animName = configMinimalSpoilerVersion.Value ? "playalt" : "play";
                        animator.enabled = true;
                        animator.SetTrigger(animName);
                        if (playerHeldBy == localPlayer)
                        {
                            audioSource.Play();
                            isLocalPlayerCursed = true;
                        }
                    }

                    if (playerHeldBy == localPlayer)
                    {
                        SpawnSomethingServerRpc(playerHeldBy.actualClientId);
                    }
                }
            }

            wasHeld = true;
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
            logger.LogDebug($"Changed sprite: {photoType}-{photoIndex}");
        }

        [ClientRpc]
        public void ChangeSpriteClientRpc(int type, int index, int scrapValue)
        {
            ChangeSprite(type, index);
            SetScrapValue(scrapValue);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnSomethingServerRpc(ulong hauntedPlayerId)
        {
            if (!IsServer) return;
            PlayerControllerB? playerToHaunt = PlayerFromId(hauntedPlayerId);
            if (playerToHaunt == null) { return; }
            if (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }
            if (SomethingContentHandler.Instance == null || SomethingContentHandler.Instance.Something == null) { return; }
            SomethingAI something = (SomethingAI)Utils.SpawnEnemy(SomethingKeys.Something, Vector3.zero, Quaternion.identity)!;
            something.ChangeTargetPlayerClientRpc(playerToHaunt.actualClientId);
        }
    }
}