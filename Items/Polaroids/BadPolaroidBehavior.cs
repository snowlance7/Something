using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;

namespace Something.Items.Polaroids
{
    public class BadPolaroidBehavior : PhysicsProp
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MeshRenderer Renderer;
        public Texture2D[] Photos;
        Material uniqueMaterial;
        public GameObject SomethingPrefab;
        public AudioSource ItemSFX;
        public AudioClip[] clips;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public override void Start()
        {
            if (!IsServerOrHost) { return; }

            int index = Random.Range(0, Photos.Length);
            ChangePhotoClientRpc(index);
        }

        [ClientRpc]
        public void ChangePhotoClientRpc(int index)
        {
            uniqueMaterial = new Material(Renderer.material);
            uniqueMaterial.mainTexture = Photos[index];
            Renderer.material = uniqueMaterial;
        }
    }
}