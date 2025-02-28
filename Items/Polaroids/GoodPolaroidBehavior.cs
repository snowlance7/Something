using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
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

        int photoIndex = -1;

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

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);

            renderer.enabled = enable;
        }

        public override void LoadItemSaveData(int saveData)
        {
            photoIndex = saveData;
        }

        public override int GetItemDataToSave()
        {
            return photoIndex;
        }

        [ClientRpc]
        public void ChangeSpriteClientRpc(int index, bool spoilerFree)
        {
            photoIndex = index;
            log($"Changing sprite to {photoIndex}: {Photos[photoIndex].name}");
            renderer.sprite = spoilerFree ? AltPhotos[photoIndex] : Photos[photoIndex];
        }
    }
}