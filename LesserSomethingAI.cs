using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using static Something.Plugin;

namespace Something
{
    internal class LesserSomethingAI : MonoBehaviour
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Transform turnCompass;
        public NavMeshAgent agent;
        public AudioSource creatureSFX;
        public AudioClip[] ambientSFX;
        public AudioClip[] alertSFX;
        public AudioClip[] attackSFX;
        public float AITimeInterval;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        float timeSinceAIInterval;

        public void Update()
        {
            timeSinceAIInterval += Time.deltaTime;

            turnCompass.LookAt(localPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime);
        }

        public void OnTriggerEnter(Collider other)
        {

        }
    }
}