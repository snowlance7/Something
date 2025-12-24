using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;
using static Something.Plugin;

namespace Something
{
    internal class TinySomethingAI : MonoBehaviour
    {
        private static ManualLogSource logger = Plugin.logger;

#pragma warning disable CS8618
        public Transform turnCompass;
        public AudioSource creatureSFX;
#pragma warning restore CS8618

        public void Update()
        {
            turnCompass.LookAt(localPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime);
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || !other.gameObject.TryGetComponent(out PlayerControllerB player)) { return; }
            if (player != localPlayer) { return; }
            logger.LogDebug("Player stepped on little one");
            localPlayer.insanityLevel++;
            creatureSFX.Play();
            GameObject.Destroy(this.gameObject, 0.5f);
        }
    }
}