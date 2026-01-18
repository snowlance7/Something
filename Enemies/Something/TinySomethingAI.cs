using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using static Something.Plugin;

namespace Something.Enemies.Something
{
    internal class TinySomethingAI : MonoBehaviour
    {
        public static List<TinySomethingAI> Instances { get; private set; } = [];

#pragma warning disable CS8618
        public Transform turnCompass;
        public AudioSource creatureSFX;
        public SpriteRenderer renderer;
#pragma warning restore CS8618

        float timeSpawned;
        public float destroyTime = 10f;

        public void Start()
        {
            Instances.Add(this);
        }
        public void OnDestroy()
        {
            Instances.Remove(this);
        }

        public void Update()
        {
            timeSpawned += Time.deltaTime;

            turnCompass.LookAt(localPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), turnCompassSpeedGlobal * Time.deltaTime);

            if (timeSpawned > destroyTime && !renderer.isVisible)
            {
                Destroy(this.gameObject);
                return;
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || !other.gameObject.TryGetComponent(out PlayerControllerB player)) { return; }
            if (player != localPlayer) { return; }
            logger.LogDebug("Player stepped on little one");
            localPlayer.insanityLevel++;
            creatureSFX.pitch = Random.Range(0.90f, 1.06f);
            creatureSFX.Play();
            Destroy(this.gameObject, 0.5f);
        }
    }
}