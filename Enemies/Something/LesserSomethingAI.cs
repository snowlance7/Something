using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static Something.Plugin;

// TODO: Make screen darken around edges and give drunk effect when touching them

namespace Something.Enemies.Something
{
    internal class LesserSomethingAI : MonoBehaviour
    {
        public static List<LesserSomethingAI> Instances { get; private set; } = new List<LesserSomethingAI>();

#pragma warning disable CS8618
        public Transform turnCompass;
        public NavMeshAgent agent;
        public AudioSource audioSource;
        public AudioClip[] ambientSFX;
        public AudioClip[] alertSFX;
        public AudioClip[] attackSFX;
        public GameObject[] variants;

        [HideInInspector]
        public PlayerControllerB targetPlayer;
        [HideInInspector]
        public Transform spawnNode;

        Coroutine wanderRoutine;
#pragma warning restore CS8618

        float timeSinceAIInterval;
        float timeSpawned;
        bool chasing;
        float stareTime;
        int variantIndex;

        [HideInInspector]
        public float destroyTime;

        const float AITimeInterval = 0.2f;

        // Configs
        const float idleMinInterval = 3f;
        const float idleMaxInterval = 10f;
        const float maxStareTime = 1f;
        //const int damage = 2;
        const float insanityMultiplier = 1f;
        const float insanityChaseMultiplier = 1.2f;
        const float chaseSpeed = 1.5f;
        const float wanderSpeed = 1f;
        const float insanityOnCollision = 5f;

        public void Start()
        {
            Instances.Add(this);
            variantIndex = UnityEngine.Random.Range(0, variants.Length);
            variants[variantIndex].SetActive(true);
            wanderRoutine = StartCoroutine(WanderingCoroutine());
        }

        public void OnDestroy()
        {
            Instances.Remove(this);
        }

        public void Update()
        {
            if (targetPlayer == null) { return; }
            timeSinceAIInterval += Time.deltaTime;
            timeSpawned += Time.deltaTime;

            if (timeSinceAIInterval >= AITimeInterval)
            {
                timeSinceAIInterval = 0f;
                DoAIInterval();
            }
        }

        public void LateUpdate()
        {
            if (targetPlayer == null) { return; }
            turnCompass.LookAt(localPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime);
        }

        public void DoAIInterval()
        {
            bool inLOS = targetPlayer.HasLineOfSightToPosition(transform.position);

            if (!inLOS && timeSpawned > destroyTime)
            {
                Destroy(gameObject);
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
                    RoundManager.PlayRandomClip(audioSource, alertSFX);
                    return;
                }
            }
        }

        IEnumerator WanderingCoroutine()
        {
            yield return null;

            yield return new WaitUntil(() => targetPlayer != null);

            while (spawnNode != null)
            {
                float timeStopped = 0f;
                float idleTime = Random.Range(idleMinInterval, idleMaxInterval);
                Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadius(spawnNode.position, 3, RoundManager.Instance.navHit);
                agent.SetDestination(position);
                while (true)
                {
                    yield return new WaitForSeconds(AITimeInterval);
                    if (!agent.hasPath || timeStopped > 1f)
                    {
                        RoundManager.PlayRandomClip(audioSource, ambientSFX);
                        yield return new WaitForSeconds(idleTime);
                        break;
                    }

                    if (agent.velocity == Vector3.zero)
                    {
                        timeStopped += AITimeInterval;
                    }
                }
            }
        }

        public void OnCollision(Collider other)
        {
            if (!other.CompareTag("Player") || !other.gameObject.TryGetComponent(out PlayerControllerB player)) { return; }
            if (player != localPlayer) { return; }
            logger.LogDebug("Player collided with lesser something");
            //localPlayer.DamagePlayer(damage);
            localPlayer.insanityLevel += insanityOnCollision;
            RoundManager.PlayRandomClip(localPlayer.statusEffectAudio, attackSFX);
            Destroy(gameObject);
        }
    }
}