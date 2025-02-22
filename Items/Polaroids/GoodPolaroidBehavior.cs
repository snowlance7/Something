using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;

namespace Something.Items.Polaroids
{
    public class GoodPolaroidBehavior : PhysicsProp
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GameObject SomethingPrefab;
        public SpriteRenderer renderer;
        public Sprite[] Photos;
        public Sprite[] AltPhotos;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        int photoIndex;

        public override void Start()
        {
            base.Start();
            if (!IsServerOrHost) { return; }
            
            int index = configSpoilerFreeVersion.Value ? Random.Range(0, AltPhotos.Length) : Random.Range(0, Photos.Length);
            ChangeSpriteClientRpc(index, configSpoilerFreeVersion.Value);
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);

            renderer.enabled = enable;
        }

        public override void LoadItemSaveData(int saveData)
        {
            if (!IsServerOrHost) { return; }
            ChangeSpriteClientRpc(saveData, configSpoilerFreeVersion.Value);
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
    }
}