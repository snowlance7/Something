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

        public PlayerControllerB targetPlayer;
        Coroutine wanderRoutine;
        public Transform spawnNode;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        float timeSinceAIInterval;
        float timeSpawned;
        bool chasing;
        float stareTime;
        public bool init;
        public float destroyTime;

        // Configs
        float idleMinInterval = 3f;
        float idleMaxInterval = 10f;
        float maxStareTime = 1f;
        int damage = 2;
        float insanityMultiplier = 1f;
        float insanityChaseMultiplier = 1.2f;
        float chaseSpeed = 1.5f;
        float wanderSpeed = 1f;
        float insanityOnCollision = 5f;

        public void Start()
        {
            wanderRoutine = StartCoroutine(WanderingCoroutine());
        }

        public void Update()
        {
            if (!init) { return; }
            timeSinceAIInterval += Time.deltaTime;
            timeSpawned += Time.deltaTime;

            if (timeSinceAIInterval >= AITimeInterval)
            {
                timeSinceAIInterval = 0f;
                DoAIInterval();
            }

            turnCompass.LookAt(localPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime);
        }

        public void DoAIInterval()
        {
            bool inLOS = targetPlayer.HasLineOfSightToPosition(transform.position);

            if (!inLOS && timeSpawned > destroyTime)
            {
                Destroy(this.gameObject);
                return;
            }

            if (chasing)
            {
                agent.speed = chaseSpeed;

                agent.SetDestination(targetPlayer.transform.position);

                if (inLOS)
                {
                    targetPlayer.insanityLevel += AITimeInterval * insanityChaseMultiplier;
                }
            }
            else
            {
                agent.speed = wanderSpeed;

                if (!inLOS) { return; }

                targetPlayer.insanityLevel += AITimeInterval * insanityMultiplier;
                stareTime += AITimeInterval;

                if (stareTime >= maxStareTime)
                {
                    StopCoroutine(wanderRoutine);
                    chasing = true;
                    RoundManager.PlayRandomClip(creatureSFX, alertSFX);
                    return;
                }
            }
        }

        IEnumerator WanderingCoroutine()
        {
            yield return null;

            yield return new WaitUntil(() => init);

            while (true)
            {
                float timeStuck = 0f;
                float idleTime = UnityEngine.Random.Range(idleMinInterval, idleMaxInterval);
                Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadius(spawnNode.position, 3, RoundManager.Instance.navHit);
                agent.SetDestination(position);
                while (true)
                {
                    yield return new WaitForSeconds(AITimeInterval);
                    if (!agent.hasPath || timeStuck > 1f)
                    {
                        RoundManager.PlayRandomClip(creatureSFX, ambientSFX);
                        yield return new WaitForSeconds(idleTime);
                        break;
                    }

                    if (agent.velocity == Vector3.zero)
                    {
                        timeStuck += AITimeInterval;
                    }
                }
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || !other.gameObject.TryGetComponent(out PlayerControllerB player)) { return; }
            if (player != localPlayer) { return; }
            logger.LogDebug("Player collided with lesser something");
            localPlayer.DamagePlayer(damage);
            localPlayer.insanityLevel += insanityOnCollision;
            RoundManager.PlayRandomClip(localPlayer.statusEffectAudio, attackSFX);
            GameObject.Destroy(this.gameObject);
        }
    }
}