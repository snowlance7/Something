using Dawn.Utils;
using Dusk;
using GameNetcodeStuff;
using SnowyLib;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;
using static Something.Configs;
using Something.Enemies;

namespace Something.Items
{
    public class PolaroidBehavior : PhysicsProp // TODO: Add functionality for putting polaroids on the wall of the ship TODO: Functioning polaroid camera and photo album that saves on your pc and is transferable? TODO: Funtionality for a folder representing the photo album. it has a ui and you can flip through them, select which photos to take out into the lethal company world, and hang on the ship and works like a import/export for polaroid photos
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

        public override void Start()
        {
            base.Start();

            if (!IsServer) { return; }

            Utils.OnShipLanded.AddListener(OnShipLanded);

            if (hasBeenHeld) return;

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

        public override void EquipItem() // SYNCED
        {
            if (!hasBeenHeld)
            {
                bool spawnSomething = photoType == 2 || (photoType == 1 && UnityEngine.Random.Range(0f, 1f) < 0.5f);

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

                    if (IsServer)
                        SpawnSomethingOnServer(playerHeldBy);
                }
            }

            base.EquipItem();
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);
            renderer.enabled = enable;
        }

        public override void LoadItemSaveData(int saveData)
        {
            hasBeenHeld = true; // TODO: Test this
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

        void SpawnSomethingOnServer(PlayerControllerB playerToHaunt)
        {
            if (!IsServer) return;
            if (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }
            if (SomethingContentHandler.Instance == null || SomethingContentHandler.Instance.Something == null) { return; }
            SomethingAI something = (SomethingAI)Utils.SpawnEnemy(SomethingKeys.Something, Vector3.zero, Quaternion.identity)!;
            something.ChangeTargetPlayerClientRpc(playerToHaunt.actualClientId);
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
            if (!IsServer) { return; }
            PlayerControllerB? playerToHaunt = PlayerFromId(hauntedPlayerId);
            if (playerToHaunt == null) { return; }
            SpawnSomethingOnServer(playerToHaunt);
        }
    }
}