using BepInEx.Logging;
using GameNetcodeStuff;
using System;
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
        public AudioClip[] attackSFX;
        public SpriteRenderer somethingMesh;
        public GameObject ScanNode;
        public GameObject BreathingMechanicPrefab;
        System.Random random;
        BreathingBehavior BreathingUI;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        List<GameObject> SpawnedTinySomethings = [];

        bool enemyMeshEnabled;
        bool initializedRandomSeed;
        bool hauntingLocalPlayer;
        float timeSinceSpawnLS;
        float nextSpawnTimeLS;
        float timeSinceStare;
        float timeSinceChaseAttempt;
        bool staring;

        bool choosingNewPlayerToHaunt = true;

        // Constants
        const float maxInsanity = 50f;

        // Configs
        float lsMinSpawnTime = 10;
        float lsMaxSpawnTime = 30;
        float lsAmount = 0.1f;
        float tsAmount = 0.5f;
        float insanityPhase3 = 0.3f;
        float stareCooldown = 20f;
        float stareBufferTime = 5f;
        float stareTime = 10f;
        float insanityIncreaseOnLook = 10f;
        float somethingChaseSpeed = 10f;
        float chaseCooldown = 5f;
        float insanityPhase1 = 0f;
        float insanityPhase2 = 0.1f;

        public enum State
        {
            Inactive,
            Chasing
        }

        public override void Start()
        {
            log("Something spawned");
            base.Start();



            if (!RoundManager.Instance.hasInitializedLevelRandomSeed)
            {
                RoundManager.Instance.InitializeRandomNumberGenerators();
            }
            log("initialized random number generators");

            currentBehaviourStateIndex = (int)State.Inactive;
            StartCoroutine(ChoosePlayerToHauntCoroutine(5f));
        }

        public override void Update()
        {
            base.Update();

            if (StartOfRound.Instance.allPlayersDead) { return; }

            if (IsServerOrHost && targetPlayer != null && !targetPlayer.isPlayerControlled && !choosingNewPlayerToHaunt)
            {
                choosingNewPlayerToHaunt = true;
                ChooseNewPlayerToHauntClientRpc();
                return;
            }
            else if (!base.IsOwner)
            {
                if (enemyMeshEnabled)
                {
                    EnableEnemyMesh(false);
                }
                return;
            }
            else if (targetPlayer != null && localPlayer != targetPlayer)
            {
                ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
            }

            if (targetPlayer == null
                || targetPlayer.isPlayerDead
                || targetPlayer.disconnectedMidGame
                || inSpecialAnimation
                || choosingNewPlayerToHaunt)
            {
                return;
            }

            if (!base.IsOwner) { return; }

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

            if (!base.IsOwner) { return; }

            if (StartOfRound.Instance.allPlayersDead
                || targetPlayer == null
                || targetPlayer.isPlayerDead
                || targetPlayer.disconnectedMidGame
                || inSpecialAnimation
                || choosingNewPlayerToHaunt)
            {
                return;
            }

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Inactive:
                    agent.speed = 0f;

                    if (!targetPlayer.isInsideFactory) { return; }

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
                                SwitchToBehaviourServerRpc((int)State.Chasing);
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
                    else if ((maxInsanity * insanityPhase3) <= targetPlayer.insanityLevel && timeSinceStare > stareCooldown) // Staring at player
                    {
                        log("Attempting stare player");
                        staring = true;
                        StarePlayer();
                    }

                    if (timeSinceSpawnLS > nextSpawnTimeLS && (maxInsanity * insanityPhase1) <= targetPlayer.insanityLevel)
                    {
                        timeSinceSpawnLS = 0f;
                        nextSpawnTimeLS = UnityEngine.Random.Range(lsMinSpawnTime, lsMaxSpawnTime);
                        SpawnLittleOnes(false);

                        if ((maxInsanity * insanityPhase2) <= targetPlayer.insanityLevel)
                        {
                            SpawnLesserSomethings();
                        }
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
            log("Trying to start chase");
            targetNode = ChoosePositionInFrontOfPlayer(5f);
            if (targetNode == null)
            {
                log("targetNode is null!");
                return;
            }

            StartCoroutine(FreezePlayerCoroutine(3f));
            Teleport(targetNode.position);
            EnableEnemyMesh(true);
            staring = false;
            inSpecialAnimation = true;
            creatureAnimator.SetTrigger("spawn");
            SwitchToBehaviourServerRpc((int)State.Chasing);
        }

        void StopChase()
        {
            creatureVoice.Stop();
            EnableEnemyMesh(false);
            SwitchToBehaviourServerRpc((int)State.Inactive);
        }

        IEnumerator FreezePlayerCoroutine(float freezeTime)
        {
            FreezePlayer(targetPlayer, true);
            yield return new WaitForSeconds(freezeTime);
            FreezePlayer(targetPlayer, false);
        }

        void StarePlayer()
        {
            targetNode = TryFindingHauntPosition();
            if (targetNode == null)
            {
                log("targetNode is null!");
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
            if (enemyMeshEnabled && currentBehaviourStateIndex != (int)State.Chasing)
            {
                EnableEnemyMesh(false);
                timeSinceStare = 0f;
                staring = false;
            }
        }

        private Transform? TryFindingHauntPosition(bool mustBeInLOS = true)
        {
            if (targetPlayer.isInsideFactory)
            {
                for (int i = 0; i < allAINodes.Length; i++)
                {
                    if ((!mustBeInLOS || !Physics.Linecast(targetPlayer.gameplayCamera.transform.position, allAINodes[i].transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) && !targetPlayer.HasLineOfSightToPosition(allAINodes[i].transform.position, 80f, 100, 8f))
                    {
                        Debug.DrawLine(targetPlayer.gameplayCamera.transform.position, allAINodes[i].transform.position, Color.green, 2f);
                        Debug.Log($"Player distance to haunt position: {Vector3.Distance(targetPlayer.transform.position, allAINodes[i].transform.position)}");
                        return allAINodes[i].transform;
                    }
                }
            }
            return null;
        }

        public Transform? ChoosePositionInFrontOfPlayer(float minDistance)
        {
            log("Choosing position in front of player");
            Transform? result = null;
            log(allAINodes.Count() + " ai nodes");
            foreach (var node in allAINodes)
            {
                if (node == null) { continue; }
                Vector3 nodePos = node.transform.position + Vector3.up * 0.5f;
                Vector3 playerPos = targetPlayer.gameplayCamera.transform.position;
                float distance = Vector3.Distance(playerPos, nodePos);
                if (distance < minDistance) { continue; }
                if (Physics.Linecast(nodePos, playerPos, StartOfRound.Instance.collidersAndRoomMaskAndDefault/*, queryTriggerInteraction: QueryTriggerInteraction.Ignore*/)) { continue; }
                if (!targetPlayer.HasLineOfSightToPosition(nodePos)) { continue; }

                mostOptimalDistance = distance;
                result = node.transform;
            }

            log($"null: {targetNode == null}");
            return result;
        }

        public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false)
        {
            log($"EnableEnemyMesh({enable})");
            somethingMesh.enabled = enable;
            ScanNode.SetActive(enable);
            enemyMeshEnabled = enable;
        }

        Vector3 FindPositionOutOfLOS()
        {
            targetNode = ChooseClosestNodeToPosition(targetPlayer.transform.position, true);
            return RoundManager.Instance.GetNavMeshPosition(targetNode.position);
        }

        IEnumerator ChoosePlayerToHauntCoroutine(float delay)
        {
            choosingNewPlayerToHaunt = true;
            log($"choosing player to haunt in {delay} seconds");
            yield return new WaitForSeconds(delay);
            if (targetPlayer == null)
            {
                ChoosePlayerToHaunt();
            }
            choosingNewPlayerToHaunt = false;
        }

        void ChoosePlayerToHaunt()
        {
            log("starting ChoosePlayerToHaunt()");
            if (!initializedRandomSeed)
            {
                int seed = StartOfRound.Instance.randomMapSeed + 158;
                log("Assigning new random with seed: " + seed);
                random = new System.Random(seed);
            }
            float highestInsanity = 0f;
            float highestInsanityPlayerIndex = 0f;
            int maxTurns = 0;
            int mostTurnsPlayerIndex = 0;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount > maxTurns)
                {
                    maxTurns = StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount;
                    mostTurnsPlayerIndex = i;
                }
                if (StartOfRound.Instance.allPlayerScripts[i].insanityLevel > highestInsanity)
                {
                    highestInsanity = StartOfRound.Instance.allPlayerScripts[i].insanityLevel;
                    highestInsanityPlayerIndex = i;
                }
            }
            int[] playerScores = new int[StartOfRound.Instance.allPlayerScripts.Length];
            for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
            {
                if (!StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled)
                {
                    playerScores[j] = 0;
                    log($"{StartOfRound.Instance.allPlayerScripts[j].playerUsername}: {playerScores[j]}");
                    continue;
                }
                playerScores[j] += 80;
                if (highestInsanityPlayerIndex == (float)j && highestInsanity > 1f)
                {
                    playerScores[j] += 50;
                }
                if (mostTurnsPlayerIndex == j)
                {
                    playerScores[j] += 30;
                }
                if (!StartOfRound.Instance.allPlayerScripts[j].hasBeenCriticallyInjured)
                {
                    playerScores[j] += 10;
                }
                if (StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer != null && StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer.scrapValue > 150)
                {
                    playerScores[j] += 30;
                }

                log($"{StartOfRound.Instance.allPlayerScripts[j].playerUsername}: {playerScores[j]}");
            }
            PlayerControllerB hauntingPlayer = StartOfRound.Instance.allPlayerScripts[RoundManager.Instance.GetRandomWeightedIndex(playerScores, random)];
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

            Debug.Log($"Something: Haunting player with playerClientId: {hauntingPlayer.playerClientId}; actualClientId: {hauntingPlayer.actualClientId}");
            ChangeOwnershipOfEnemy(hauntingPlayer.actualClientId);
            hauntingLocalPlayer = GameNetworkManager.Instance.localPlayerController == hauntingPlayer;
            hauntingPlayer.insanityLevel = 0f;
            targetPlayer = hauntingPlayer;

            if (IsServerOrHost)
            {
                NetworkObject.ChangeOwnership(targetPlayer.actualClientId);
            }

            SpawnLittleOnes(true);
            if (!hauntingLocalPlayer) { return; };
            BreathingUI = GameObject.Instantiate(BreathingMechanicPrefab).GetComponent<BreathingBehavior>();
        }

        public void Teleport(Vector3 position)
        {
            log("Teleporting to " + position.ToString());
            position = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit);
            agent.Warp(position);
        }

        void SpawnLesserSomethings()
        {
            int debugSpawnAmount = 0;

            /*int minAmount = (int)(allAINodes.Length * lsMinAmount);
            int maxAmount = (int)(allAINodes.Length * lsMaxAmount);
            int spawnAmount = Random.Range(minAmount, maxAmount + 1);*/

            int spawnAmount = (int)(allAINodes.Length * lsAmount);

            List<GameObject> nodes = allAINodes.ToList();
            for (int i = 0; i < spawnAmount; i++)
            {
                if (nodes.Count <= 0) { break; }
                GameObject node = GetRandomAINode(nodes);
                nodes.Remove(node);

                if (node == null || targetPlayer.HasLineOfSightToPosition(node.transform.position))
                {
                    i--;
                    continue;
                }

                int lsIndex = UnityEngine.Random.Range(0, lesserSomethingPrefabs.Length);

                Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(node.transform.position, 10f, RoundManager.Instance.navHit, random);
                LesserSomethingAI ls = GameObject.Instantiate(lesserSomethingPrefabs[lsIndex], pos, Quaternion.identity).GetComponent<LesserSomethingAI>();
                ls.spawnNode = node.transform;
                ls.targetPlayer = targetPlayer;
                ls.destroyTime = nextSpawnTimeLS;
                ls.init = true;
                //UnityEngine.GameObject.Destroy(ls.gameObject, lsMaxSpawnTime);
                debugSpawnAmount++;
            }

            log($"Spawned {debugSpawnAmount}/{allAINodes.Length} lesser_somethings which will self destruct in {nextSpawnTimeLS} seconds");
        }

        void SpawnLittleOnes(bool reset)
        {
            if (SpawnedTinySomethings.Count > 0)
            {
                foreach(GameObject ts in SpawnedTinySomethings.ToList())
                {
                    if (ts == null) { SpawnedTinySomethings.Remove(ts); continue; }
                    if (!reset && targetPlayer.HasLineOfSightToPosition(ts.transform.position)) { continue; }
                    SpawnedTinySomethings.Remove(ts);
                    Destroy(ts);
                }

                if (reset) { SpawnedTinySomethings.Clear(); }
            }

            if (!hauntingLocalPlayer) { return; }

            /*int minAmount = (int)(allAINodes.Length * tsMinAmount);
            int maxAmount = (int)(allAINodes.Length * tsMaxAmount);
            int spawnAmount = Random.Range(minAmount, maxAmount + 1);*/

            int spawnAmount = (int)(allAINodes.Length * tsAmount);

            List<GameObject> nodes = allAINodes.ToList();
            for (int i = 0; i < spawnAmount; i++)
            {
                GameObject node = GetRandomAINode(nodes);
                nodes.Remove(node);

                if (node == null || targetPlayer.HasLineOfSightToPosition(node.transform.position))
                {
                    i--;
                    continue;
                }

                Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(node.transform.position, 10f, RoundManager.Instance.navHit, random);
                SpawnedTinySomethings.Add(GameObject.Instantiate(littleOnePrefab, pos, Quaternion.identity));
            }
        }

        GameObject GetRandomAINode(List<GameObject> nodes)
        {
            int randIndex = UnityEngine.Random.Range(0, nodes.Count);
            return nodes[randIndex];
        }

        public override void OnCollideWithPlayer(Collider other) // This only runs on client collided with
        {
            base.OnCollideWithPlayer(other);
            if (inSpecialAnimation) { return; }
            if (currentBehaviourStateIndex == (int)State.Inactive) { return; }
            if (!other.gameObject.TryGetComponent(out PlayerControllerB player)) { return; }
            if (player == null || player != localPlayer) { return; }

            inSpecialAnimation = true;
            StartCoroutine(KillPlayerCoroutine());
        }

        IEnumerator KillPlayerCoroutine()
        {
            log("In KillPlayerCoroutine()");
            yield return null;
            StartCoroutine(FreezePlayerCoroutine(3f));
            EnableEnemyMesh(false);
            creatureVoice.Stop();
            RoundManager.PlayRandomClip(targetPlayer.movementAudio, attackSFX);
            BreathingUI.JumpscarePlayer(3f);

            yield return new WaitForSeconds(3f);
            localPlayer.KillPlayer(Vector3.zero, false);
            PlayDisappearSFXClientRpc();
        }

        void ResetHallucinations()
        {
            log("Destroying little ones");
            foreach (var ts in SpawnedTinySomethings.ToList())
            {
                Destroy(ts);
            }

            log("Destroying breathing UI");
            if (BreathingUI != null)
            {
                Destroy(BreathingUI.gameObject);
            }

            EnableEnemyMesh(false);
            creatureVoice.Stop();
        }

        public override void OnDestroy()
        {
            ResetHallucinations();
            base.OnDestroy();
        }

        // Animations

        public void EndSpawnAnimation()
        {
            log("In EndSpawnAnimation()");
            inSpecialAnimation = false;
            creatureVoice.Play();
            FreezePlayer(targetPlayer, false);
        }

        public void EndDespawnAnimation()
        {
            log("In EndDespawnAnimation");
            creatureVoice.Stop();
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
            ResetHallucinations();
            PlayerControllerB player = PlayerFromId(clientId);
            player.insanityLevel = 0f;
            targetPlayer = player;
            log($"Something: Haunting player with playerClientId: {targetPlayer.playerClientId}; actualClientId: {targetPlayer.actualClientId}");
            ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
            hauntingLocalPlayer = localPlayer == targetPlayer;

            if (IsServerOrHost)
            {
                NetworkObject.ChangeOwnership(targetPlayer.actualClientId);
            }

            SpawnLittleOnes(true);
            if (!hauntingLocalPlayer) { return; };
            BreathingUI = GameObject.Instantiate(BreathingMechanicPrefab).GetComponent<BreathingBehavior>();
            choosingNewPlayerToHaunt = false;
        }

        [ClientRpc]
        public void PlayDisappearSFXClientRpc()
        {
            creatureVoice.PlayOneShot(disappearSFX, 1f);
        }

        [ClientRpc]
        public void ChooseNewPlayerToHauntClientRpc()
        {
            choosingNewPlayerToHaunt = true;
            ResetHallucinations();
            targetPlayer = null;
            SwitchToBehaviourStateOnLocalClient((int)State.Inactive);
            StartCoroutine(ChoosePlayerToHauntCoroutine(5f));
        }
    }
}