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
    internal class SomethingAI : EnemyAI
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Transform turnCompass;
        public GameObject[] lesserSomethingPrefabs;
        public GameObject littleOnePrefab;
        public AudioClip disappearSFX;
        public AudioClip[] ambientSFX;
        public SpriteRenderer somethingMesh;
        public ScanNodeProperties ScanNode;
        public GameObject BreathingMechanicPrefab;
        System.Random random;
        BreathingBehavior BreathingUI;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        List<GameObject> SpawnedTinySomethings = [];

        bool spawnedAndVisible;
        bool initializedRandomSeed;
        bool hauntingLocalPlayer;
        float timeSinceSpawnLS;
        float nextSpawnTimeLS;
        float timeSinceStare;
        float timeSinceChaseAttempt;
        bool staring;

        // Constants
        const float maxInsanity = 50f;

        // Configs
        int lsMinSpawnTime = 10;
        int lsMaxSpawnTime = 30;
        bool lsUseFixedSpawnAmount = false;
        int lsMinFixedAmount;
        int lsMaxFixedAmount;
        float lsMinAmount = 0.1f;
        float lsMaxAmount = 0.7f;
        bool tsUseFixedSpawnAmount = false;
        int tsMinFixedAmount;
        int tsMaxFixedAmount;
        float tsMinAmount = 0.5f;
        float tsMaxAmount = 0.8f;
        float insanityToStare = 0.3f;
        float stareCooldown = 20f;
        float stareBufferTime = 5f;
        float stareTime = 10f;
        float insanityIncreaseOnLook = 10f;
        float somethingChaseSpeed = 10f;
        float chaseCooldown = 5f;

        public enum State
        {
            Inactive,
            Chasing
        }

        public void SwitchToBehaviorStateCustom(State state)
        {
            logger.LogDebug("Switching to state: " + state);

            switch (state)
            {
                case State.Inactive:

                    break;
                case State.Chasing:

                    break;
                default:
                    break;
            }

            SwitchToBehaviourClientRpc((int)state);
        }

        public override void Start()
        {
            base.Start();
            logger.LogDebug("Something spawned");

            if (!RoundManager.Instance.hasInitializedLevelRandomSeed)
            {
                RoundManager.Instance.InitializeRandomNumberGenerators();
            }
            logger.LogDebug("initialized random number generators");

            StartCoroutine(ChoosePlayerToHauntCoroutine(15f));
            currentBehaviourStateIndex = (int)State.Inactive;
        }

        public override void Update()
        {
            if (!base.IsOwner) { return; }
            if (inSpecialAnimation)
            {
                return;
            }
            if (updateDestinationInterval >= 0f)
            {
                updateDestinationInterval -= Time.deltaTime;
            }
            else
            {
                DoAIInterval();
                updateDestinationInterval = AIIntervalTime + Random.Range(-0.015f, 0.015f);
            }

            if (StartOfRound.Instance.allPlayersDead)
            {
                return;
            };

            if (targetPlayer == null) { return; }

            turnCompass.LookAt(localPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime); // Always look at local player

            float newFear = targetPlayer.insanityLevel / maxInsanity;
            targetPlayer.playersManager.fearLevel = Mathf.Max(targetPlayer.playersManager.fearLevel, newFear); // Change fear based on insanity

            timeSinceSpawnLS += Time.deltaTime;
            timeSinceStare += Time.deltaTime;
            timeSinceChaseAttempt += Time.deltaTime;
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();

            if (StartOfRound.Instance.allPlayersDead)
            {
                return;
            };

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Inactive:
                    agent.speed = 0f;

                    if (targetPlayer == null || !targetPlayer.isInsideFactory) { return; }

                    if (targetPlayer.insanityLevel >= maxInsanity && timeSinceChaseAttempt > chaseCooldown)
                    {
                        timeSinceChaseAttempt = 0f;
                        TryStartChase();
                        return;
                    }

                    if (staring)
                    {
                        if (targetPlayer.HasLineOfSightToPosition(transform.position))
                        {
                            targetPlayer.insanityLevel += insanityIncreaseOnLook;
                            if (targetPlayer.insanityLevel >= maxInsanity)
                            {
                                SwitchToBehaviorStateCustom(State.Chasing);
                                creatureVoice.Play(); // play chase sfx
                                staring = false;
                                timeSinceStare = 0f;
                                return;
                            }

                            timeSinceStare = 0f;
                            staring = false;
                            creatureAnimator.SetTrigger("despawn");
                            creatureSFX.PlayOneShot(disappearSFX);
                            return;
                        }
                    }
                    else if ((maxInsanity * insanityToStare) <= targetPlayer.insanityLevel && timeSinceStare > stareCooldown) // Staring at player
                    {
                        logger.LogDebug("Attempting stare player");
                        staring = true;
                        StarePlayer();
                    }

                    break;

                case (int)State.Chasing:
                    agent.speed = somethingChaseSpeed;

                    if (!targetPlayer.isPlayerAlone || !targetPlayer.isInsideFactory)
                    {
                        StopChase();
                    }

                    SetDestinationToPosition(targetPlayer.transform.position);

                    break;

                default:
                    logger.LogWarning("Invalid state: " + currentBehaviourStateIndex);
                    break;
            }
        }

        void TryStartChase()
        {
            logger.LogDebug("Trying to start chase");
            targetNode = ChoosePositionInFrontOfPlayer(10f);
            if (targetNode == null)
            {
                logger.LogDebug("targetNode is null!");
                return;
            }

            StartCoroutine(FreezePlayerCoroutine(1f));
            Teleport(targetNode.position);
            EnableEnemyMesh(true);
            staring = false;
            inSpecialAnimation = true;
            creatureAnimator.SetTrigger("spawn");
            SwitchToBehaviorStateCustom(State.Chasing);
        }

        void StopChase()
        {
            EnableEnemyMesh(false);
            SwitchToBehaviorStateCustom(State.Inactive);
        }

        IEnumerator FreezePlayerCoroutine(float freezeTime)
        {
            FreezePlayer(targetPlayer, true);
            yield return new WaitForSeconds(freezeTime);
            FreezePlayer(targetPlayer, false);
        }

        void StarePlayer()
        {
            targetNode = ChooseStarePosition(15f);
            if (targetNode == null)
            {
                logger.LogDebug("targetNode is null!");
                timeSinceStare = stareCooldown - stareBufferTime;
                staring = false;
                return;
            }

            Teleport(targetNode.position);
            EnableEnemyMesh(true);
            RoundManager.PlayRandomClip(creatureVoice, ambientSFX);
            StartCoroutine(StopStareAfterDelay(stareTime));
        }

        IEnumerator StopStareAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (spawnedAndVisible && currentBehaviourStateIndex != (int)State.Chasing)
            {
                EnableEnemyMesh(false);
                timeSinceStare = 0f;
                staring = false;
            }
        }

        public Transform? ChooseStarePosition(float minDistance)
        {
            logger.LogDebug("Choosing stare position");
            Transform? result = null;
            logger.LogDebug(allAINodes.Count() + " ai nodes");
            foreach (var node in allAINodes)
            {
                if (node == null) { continue; }
                Vector3 nodePos = node.transform.position + Vector3.up * 0.3f;
                Vector3 playerPos = targetPlayer.playerEye.transform.position;
                float distance = Vector3.Distance(playerPos, nodePos);
                if (distance < minDistance) { continue; }
                if (Physics.Linecast(nodePos, playerPos, StartOfRound.Instance.collidersAndRoomMaskAndDefault, queryTriggerInteraction: QueryTriggerInteraction.Ignore)) { continue; }
                if (targetPlayer.HasLineOfSightToPosition(nodePos)) { continue; }

                mostOptimalDistance = distance;
                result = node.transform;
            }

            logger.LogDebug($"null: {targetNode == null}");
            return result;
        }

        public Transform? ChoosePositionInFrontOfPlayer(float minDistance)
        {
            logger.LogDebug("Choosing position in front of player");
            Transform? result = null;
            logger.LogDebug(allAINodes.Count() + " ai nodes");
            foreach (var node in allAINodes)
            {
                if (node == null) { continue; }
                Vector3 nodePos = node.transform.position + Vector3.up * 0.5f;
                Vector3 playerPos = targetPlayer.playerEye.transform.position;
                float distance = Vector3.Distance(playerPos, nodePos);
                if (distance < minDistance) { continue; }
                if (Physics.Linecast(nodePos, playerPos, StartOfRound.Instance.collidersAndRoomMaskAndDefault, queryTriggerInteraction: QueryTriggerInteraction.Ignore)) { continue; }
                if (!targetPlayer.HasLineOfSightToPosition(nodePos)) { continue; }

                mostOptimalDistance = distance;
                result = node.transform;
            }

            logger.LogDebug($"null: {targetNode == null}");
            return result;
        }

        public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false)
        {
            logger.LogDebug($"EnableEnemyMesh({enable})");
            somethingMesh.enabled = enable;
            ScanNode.enabled = enable;
            spawnedAndVisible = enable;
        }

        Vector3 FindPositionOutOfLOS()
        {
            targetNode = ChooseClosestNodeToPosition(targetPlayer.transform.position, true);
            return RoundManager.Instance.GetNavMeshPosition(targetNode.position);
        }

        IEnumerator ChoosePlayerToHauntCoroutine(float delay)
        {
            logger.LogDebug($"choosing player to haunt in {delay} seconds");
            yield return new WaitForSeconds(delay);
            if (targetPlayer == null)
            {
                ChoosePlayerToHaunt();
            }
        }

        void ChoosePlayerToHaunt()
        {
            logger.LogDebug("starting ChoosePlayerToHaunt()");
            if (!initializedRandomSeed)
            {
                random = new System.Random(StartOfRound.Instance.randomMapSeed + 158);
            }
            float num = 0f;
            float num2 = 0f;
            int num3 = 0;
            int num4 = 0;
            for (int i = 0; i < 4; i++)
            {
                if (StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount > num3)
                {
                    num3 = StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount;
                    num4 = i;
                }
                if (StartOfRound.Instance.allPlayerScripts[i].insanityLevel > num)
                {
                    num = StartOfRound.Instance.allPlayerScripts[i].insanityLevel;
                    num2 = i;
                }
            }
            int[] array = new int[4];
            for (int j = 0; j < 4; j++)
            {
                if (!StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled)
                {
                    array[j] = 0;
                    continue;
                }
                array[j] += 80;
                if (num2 == (float)j && num > 1f)
                {
                    array[j] += 50;
                }
                if (num4 == j)
                {
                    array[j] += 30;
                }
                if (!StartOfRound.Instance.allPlayerScripts[j].hasBeenCriticallyInjured)
                {
                    array[j] += 10;
                }
                if (StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer != null && StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer.scrapValue > 150)
                {
                    array[j] += 30;
                }
            }
            PlayerControllerB hauntingPlayer = StartOfRound.Instance.allPlayerScripts[RoundManager.Instance.GetRandomWeightedIndex(array, random)];
            if (hauntingPlayer.isPlayerDead)
            {
                for (int k = 0; k < StartOfRound.Instance.allPlayerScripts.Length; k++)
                {
                    if (!StartOfRound.Instance.allPlayerScripts[k].isPlayerDead)
                    {
                        hauntingPlayer = StartOfRound.Instance.allPlayerScripts[k];
                        break;
                    }
                }
            }

            if (!IsServerOrHost) { return; }
            ChangeTargetPlayerClientRpc(hauntingPlayer.actualClientId);
        }

        public void Teleport(Vector3 position)
        {
            logger.LogDebug("Teleporting to " + position.ToString());
            position = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit);
            agent.Warp(position);
        }

        void SpawnLittleOnes()
        {
            if (SpawnedTinySomethings.Count > 0)
            {
                foreach(var ts in SpawnedTinySomethings.ToList())
                {
                    Destroy(ts);
                }

                SpawnedTinySomethings.Clear();
            }

            if (!hauntingLocalPlayer) { return; }

            int spawnAmount;

            if (tsUseFixedSpawnAmount)
            {
                spawnAmount = Random.Range(tsMinFixedAmount, tsMaxFixedAmount + 1);
            }
            else
            {
                int minAmount = (int)(allAINodes.Length * tsMinAmount);
                int maxAmount = (int)(allAINodes.Length * tsMaxAmount);
                spawnAmount = Random.Range(minAmount, maxAmount + 1);
            }

            List<GameObject> nodes = allAINodes.ToList();
            for (int i = 0; i < spawnAmount; i++)
            {
                GameObject node = GetRandomAINode(nodes.ToArray());
                nodes.Remove(node);
                
                Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(node.transform.position);
                SpawnedTinySomethings.Add(GameObject.Instantiate(littleOnePrefab, pos, Quaternion.identity));
            }
        }

        GameObject GetRandomAINode(GameObject[] nodes)
        {
            int randIndex = Random.Range(0, nodes.Length);
            return nodes[randIndex];
        }

        public override void OnCollideWithPlayer(Collider other) // This only runs on client collided with
        {
            logger.LogDebug("Collided with player");
            base.OnCollideWithPlayer(other);
            PlayerControllerB? player = MeetsStandardPlayerCollisionConditions(other);
            if (player == null) { return; }
            if (currentBehaviourStateIndex == (int)State.Inactive) { return; }

            player.KillPlayer(Vector3.zero, false);
            KilledPlayerClientRpc();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            foreach (var ts in SpawnedTinySomethings.ToList())
            {
                Destroy(ts);
            }
            if (BreathingUI != null)
            {
                Destroy(BreathingUI);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (BreathingUI != null)
            {
                Destroy(BreathingUI.gameObject);
                BreathingUI = null;
            }
        }

        // Animations

        public void EndSpawnAnimation()
        {
            logger.LogDebug("In EndSpawnAnimation()");
            inSpecialAnimation = false;
            creatureVoice.Play();
        }

        public void EndDespawnAnimation()
        {
            logger.LogDebug("In EndDespawnAnimation");
            inSpecialAnimation = false;
            EnableEnemyMesh(false);
        }

        // RPC's

        [ServerRpc(RequireOwnership = false)]
        public void ChangeTargetPlayerServerRpc(ulong clientId)
        {
            if (!IsServerOrHost) { return; }
            ChangeTargetPlayerClientRpc(clientId);
        }

        [ClientRpc]
        public void ChangeTargetPlayerClientRpc(ulong clientId)
        {
            targetPlayer = PlayerFromId(clientId);
            logger.LogDebug($"Something: Haunting player with playerClientId: {targetPlayer.playerClientId}; actualClientId: {targetPlayer.actualClientId}");
            ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
            hauntingLocalPlayer = GameNetworkManager.Instance.localPlayerController == targetPlayer;

            //SpawnLittleOnes();
            if (!hauntingLocalPlayer) { return; };
            BreathingUI = GameObject.Instantiate(BreathingMechanicPrefab).GetComponent<BreathingBehavior>();
        }

        [ClientRpc]
        public void KilledPlayerClientRpc()
        {
            creatureVoice.PlayOneShot(disappearSFX, 1f);

            if (!IsServerOrHost) { return; }
            NetworkObject.Despawn(true);
        }
    }
}

// TODO: statuses: shakecamera, playerstun, drunkness, fear, insanity