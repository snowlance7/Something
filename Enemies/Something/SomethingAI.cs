using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using static Something.Plugin;

namespace Something.Enemies.Something
{
    internal class SomethingAI : EnemyAI
    {
#pragma warning disable CS8618
        public Transform turnCompass;
        public GameObject lesserSomethingPrefab;
        public GameObject tinySomethingPrefab;
        public AudioClip disappearSFX;
        public AudioClip[] ambientSFX;
        public AudioClip[] attackSFX;
        public SpriteRenderer somethingMesh;
        public GameObject ScanNode;
        public GameObject BreathingMechanicPrefab;

        public GameObject DEBUG_hudOverlay;

        SomethingHUDOverlay hudOverlay;
#pragma warning restore CS8618

        new GameObject[] allAINodes => Utils.insideAINodes;

        bool isHauntingLocalPlayer => targetPlayer == localPlayer;
        bool enemyMeshEnabled;

        float nextSpawnTimeLS;

        float timeWithoutTargetPlayer;

        float timeSinceSpawnLS;
        float timeSinceTryStare;
        float timeSinceChaseAttempt;
        float timeSinceAIUpdate;
        float timeSinceSwitchBehaviour;

        // Configs
        const float maxInsanity = 50f;
        const float lsMinSpawnTime = 10;
        const float lsMaxSpawnTime = 30;
        const float lsAmount = 0.1f;
        const float tsAmount = 0.7f;
        const float stareCooldown = 20f;
        const float stareBufferTime = 5f;
        const float maxStareTime = 10f;
        const float insanityIncreaseOnLook = 10f;
        const float somethingChaseSpeed = 10f;
        const float chaseCooldown = 5f;
        const float insanityPhase1 = 0f;
        const float insanityPhase2 = 0.15f;
        const float insanityPhase3 = 0.3f;
        const float choosePlayerToHauntCooldown = 5f;
        new const float AIIntervalTime = 0.2f;

        public enum State
        {
            Inactive,
            Staring,
            Chasing
        }

        public override void Start()
        {
            base.Start();

            if (Utils.isBeta)
                Instantiate(DEBUG_hudOverlay, Vector3.zero, Quaternion.identity);
        }

        public override void Update()
        {
            if (IsServer)
            {
                if (targetPlayer == null)
                {
                    timeWithoutTargetPlayer += Time.deltaTime;
                }

                if (timeWithoutTargetPlayer > choosePlayerToHauntCooldown && targetPlayer == null)
                {
                    timeWithoutTargetPlayer = 0f;
                    ChoosePlayerToHaunt();
                }
            }

            if (targetPlayer != null && !targetPlayer.isPlayerControlled)
            {
                targetPlayer = null;
            }

            if (!IsOwner)
            {
                SetVisibility(false);
                return;
            }
            else if (targetPlayer != null && !isHauntingLocalPlayer)
            {
                ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
                return;
            }

            if (targetPlayer == null || !targetPlayer.isPlayerControlled) return;

            float newFear = targetPlayer.insanityLevel / maxInsanity;
            targetPlayer.playersManager.fearLevel = Mathf.Max(targetPlayer.playersManager.fearLevel, newFear); // Change fear based on insanity

            timeSinceSpawnLS += Time.deltaTime;
            timeSinceTryStare += Time.deltaTime;
            timeSinceChaseAttempt += Time.deltaTime;
            timeSinceAIUpdate += Time.deltaTime;
            timeSinceSwitchBehaviour += Time.deltaTime;

            if (timeSinceAIUpdate > AIIntervalTime)
            {
                timeSinceAIUpdate = 0f;
                DoAIInterval();
            }
        }

        public void LateUpdate()
        {

            turnCompass.LookAt(localPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), turnCompassSpeedGlobal * Time.deltaTime); // Always look at local player
        }

        public override void DoAIInterval()
        {
            UpdateTestingHUD();

            if (inSpecialAnimation || targetPlayer == null)
            {
                StopAgent();
                return;
            }

            SetVisibility(currentBehaviourStateIndex != (int)State.Inactive && targetPlayer.isPlayerControlled && isHauntingLocalPlayer);

            if (agent.enabled && moveTowardsDestination)
            {
                agent.SetDestination(destination);
            }

            if (timeSinceSpawnLS > nextSpawnTimeLS && maxInsanity * insanityPhase1 <= targetPlayer.insanityLevel)
            {
                timeSinceSpawnLS = 0f;
                nextSpawnTimeLS = UnityEngine.Random.Range(lsMinSpawnTime, lsMaxSpawnTime);
                SpawnLittleOnes();

                if (maxInsanity * insanityPhase2 <= targetPlayer.insanityLevel)
                {
                    SpawnLesserSomethings();
                }
            }

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Inactive:
                    agent.speed = 0f;

                    if (!targetPlayer.isInsideFactory) { return; }

                    if (targetPlayer.insanityLevel >= maxInsanity && timeSinceChaseAttempt > chaseCooldown && targetPlayer.isPlayerAlone)
                    {
                        timeSinceChaseAttempt = 0f;
                        logger.LogDebug("Trying to start chase");
                        targetNode = ChoosePositionInFrontOfPlayer(5f);
                        if (targetNode != null)
                        {
                            StartCoroutine(FreezePlayerCoroutine(targetPlayer, 3f));
                            Teleport(targetNode.position);
                            SetVisibility(true);
                            inSpecialAnimation = true;
                            creatureAnimator.SetTrigger("spawn");
                            SwitchToBehaviourStateOnLocalClient((int)State.Chasing);
                            return;
                        }
                    }

                    if (maxInsanity * insanityPhase3 <= targetPlayer.insanityLevel && timeSinceTryStare > stareCooldown) // Staring at player
                    {
                        logger.LogDebug("Attempting stare player");

                        targetNode = TryFindingHauntPosition();
                        if (targetNode != null)
                        {
                            Teleport(targetNode.position);
                            RoundManager.PlayRandomClip(creatureVoice, ambientSFX);
                            SwitchToBehaviourStateOnLocalClient((int)State.Staring);
                            return;
                        }

                        timeSinceTryStare = stareCooldown - stareBufferTime;
                    }

                    break;

                case (int)State.Staring:

                    if (targetPlayer.HasLineOfSightToPosition(transform.position))
                    {
                        timeSinceTryStare = 0f;
                        targetPlayer.insanityLevel += insanityIncreaseOnLook;

                        if (targetPlayer.insanityLevel >= maxInsanity)
                        {
                            SwitchToBehaviourStateOnLocalClient((int)State.Chasing);
                            creatureVoice.Play(); // play chase sfx
                            return;
                        }

                        inSpecialAnimation = true;
                        creatureAnimator.SetTrigger("despawn");
                        creatureSFX.PlayOneShot(disappearSFX);
                        SwitchToBehaviourStateOnLocalClient((int)State.Inactive);
                        return;
                    }

                    if (timeSinceSwitchBehaviour > maxStareTime)
                    {
                        SwitchToBehaviourStateOnLocalClient((int)State.Inactive);
                        return;
                    }

                    break;

                case (int)State.Chasing:
                    agent.speed = somethingChaseSpeed;
                    
                    if (!targetPlayer.isPlayerAlone || !targetPlayer.isInsideFactory)
                    {
                        creatureVoice.Stop();
                        SwitchToBehaviourStateOnLocalClient((int)State.Inactive);
                        return;
                    }

                    SetDestinationToPosition(targetPlayer.transform.position);

                    break;

                default:
                    logger.LogWarning("Invalid state: " + currentBehaviourStateIndex);
                    break;
            }
        }

        public void StopAgent()
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            agent.velocity = Vector3.zero;
        }

        void UpdateTestingHUD()
        {
            if (Utils.isBeta && TestingHUDOverlay.Instance != null) // TestingHUD
            {
                TestingHUDOverlay.Instance.label1.text = ((State)currentBehaviourStateIndex).ToString();

                TestingHUDOverlay.Instance.label2.text = "TargetPlayer: " + targetPlayer?.playerUsername;

                TestingHUDOverlay.Instance.label3.text = "Insanity: " + localPlayer.insanityLevel;

                //TestingHUDOverlay.Instance.toggle1.isOn = isOutside;
                //TestingHUDOverlay.Instance.toggle1Label.text = "isOutside";

                TestingHUDOverlay.Instance.toggle2.isOn = inSpecialAnimation;
                TestingHUDOverlay.Instance.toggle2Label.text = "inSpecialAnimation";
            }
        }

        IEnumerator FreezePlayerCoroutine(PlayerControllerB player, float freezeTime)
        {
            Utils.FreezePlayer(player, true);
            yield return new WaitForSeconds(freezeTime);
            Utils.FreezePlayer(player, false);
        }

        Transform? TryFindingHauntPosition(bool mustBeInLOS = true)
        {
            if (targetPlayer.isInsideFactory)
            {
                for (int i = 0; i < allAINodes.Length; i++)
                {
                    if ((!mustBeInLOS || !Physics.Linecast(targetPlayer.gameplayCamera.transform.position, allAINodes[i].transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) && !targetPlayer.HasLineOfSightToPosition(allAINodes[i].transform.position, 80f, 100, 8f))
                    {
                        return allAINodes[i].transform;
                    }
                }
            }
            return null;
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
                Vector3 playerPos = targetPlayer.gameplayCamera.transform.position;
                float distance = Vector3.Distance(playerPos, nodePos);
                if (distance < minDistance) { continue; }
                if (Physics.Linecast(nodePos, playerPos, StartOfRound.Instance.collidersAndRoomMaskAndDefault/*, queryTriggerInteraction: QueryTriggerInteraction.Ignore*/)) { continue; }
                if (!targetPlayer.HasLineOfSightToPosition(nodePos)) { continue; }

                mostOptimalDistance = distance;
                result = node.transform;
            }

            logger.LogDebug("null: {targetNode == null}");
            return result;
        }

        public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false) { }

        public void SetVisibility(bool enable)
        {
            if (enemyMeshEnabled == enable) { return; }
            logger.LogDebug($"SetVisibility({enable})");
            somethingMesh.enabled = enable;
            ScanNode.SetActive(enable);
            enemyMeshEnabled = enable;
        }

        Vector3 FindPositionOutOfLOS()
        {
            targetNode = ChooseClosestNodeToPosition(targetPlayer.transform.position, true);
            return RoundManager.Instance.GetNavMeshPosition(targetNode.position);
        }

        void ChoosePlayerToHaunt()
        {
            if (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }
            logger.LogDebug("Starting ChoosePlayerToHaunt()");

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
                    logger.LogDebug("{StartOfRound.Instance.allPlayerScripts[j].playerUsername}: {playerScores[j]}");
                    continue;
                }
                playerScores[j] += 80;
                if (highestInsanityPlayerIndex == j && highestInsanity > 1f)
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

                logger.LogDebug("{StartOfRound.Instance.allPlayerScripts[j].playerUsername}: {playerScores[j]}");
            }
            PlayerControllerB hauntingPlayer = StartOfRound.Instance.allPlayerScripts[RoundManager.Instance.GetRandomWeightedIndex(playerScores)];
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

            logger.LogDebug($"Something: Haunting player with playerClientId: {hauntingPlayer.playerClientId}; actualClientId: {hauntingPlayer.actualClientId}");
            targetPlayer = hauntingPlayer;
            ChangeTargetPlayerClientRpc(hauntingPlayer.actualClientId);
        }

        public void Teleport(Vector3 position)
        {
            logger.LogDebug("Teleporting to " + position.ToString());
            position = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit);
            agent.Warp(position);
        }

        void SpawnLesserSomethings()
        {
            int debugSpawnAmount = 0;

            int spawnAmount = (int)(allAINodes.Length * lsAmount);

            List<GameObject> nodes = allAINodes.ToList();
            for (int i = 0; i < spawnAmount; i++)
            {
                if (nodes.Count <= 0) { break; }
                GameObject node = nodes.GetRandom();
                nodes.Remove(node);

                if (node == null || targetPlayer.HasLineOfSightToPosition(node.transform.position))
                {
                    i--;
                    continue;
                }

                Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(node.transform.position, 10f, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
                LesserSomethingAI ls = Instantiate(lesserSomethingPrefab, pos, Quaternion.identity).GetComponent<LesserSomethingAI>();
                ls.spawnNode = node.transform;
                ls.destroyTime = nextSpawnTimeLS;
                ls.targetPlayer = targetPlayer;
                debugSpawnAmount++;
            }

            logger.LogDebug($"Spawned {debugSpawnAmount}/{allAINodes.Length} lesser_somethings which will self destruct in {nextSpawnTimeLS} seconds");
        }

        void SpawnLittleOnes()
        {
            int spawnAmount = (int)(allAINodes.Length * tsAmount);

            for (int i = 0; i < spawnAmount; i++)
            {
                GameObject? node = allAINodes.GetRandom();

                if (node == null || targetPlayer.HasLineOfSightToPosition(node.transform.position))
                {
                    i--;
                    continue;
                }

                Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(node.transform.position, 10f, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
                TinySomethingAI ts = Instantiate(tinySomethingPrefab, pos, Quaternion.identity).GetComponent<TinySomethingAI>();
                ts.destroyTime = nextSpawnTimeLS;
            }
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);
            if (inSpecialAnimation) { return; }
            if (currentBehaviourStateIndex != (int)State.Chasing) { return; }
            if (!other.gameObject.TryGetComponent(out PlayerControllerB player)) { return; }
            if (player == null || player != localPlayer) { return; }

            inSpecialAnimation = true;

            IEnumerator KillPlayerCoroutine()
            {
                logger.LogDebug("In KillPlayerCoroutine()");
                yield return null;
                StartCoroutine(FreezePlayerCoroutine(localPlayer, 3f));
                SetVisibility(false);
                creatureVoice.Stop();
                hudOverlay.audioSource.volume = 1f;
                RoundManager.PlayRandomClip(hudOverlay.audioSource, attackSFX);
                hudOverlay.JumpscarePlayer(3f, true);

                yield return new WaitForSeconds(3f);
                localPlayer.KillPlayer(Vector3.zero, false);
                KillPlayerClientRpc();
            }

            StartCoroutine(KillPlayerCoroutine());
        }

        void ResetHallucinations()
        {
            logger.LogDebug("Resetting Hallucinations");
            foreach (var ts in TinySomethingAI.Instances.ToList())
            {
                Destroy(ts.gameObject);
            }

            if (hudOverlay != null)
            {
                Destroy(hudOverlay.gameObject);
            }

            creatureVoice.Stop();
        }

        public override void OnDestroy()
        {
            ResetHallucinations();
            base.OnDestroy();
        }

        public void OnFinishSpawnAnimation() // Animation
        {
            if (!IsOwner) return;
            logger.LogDebug("OnFinishSpawnAnimation()");
            inSpecialAnimation = false;
            creatureVoice.Play();
            Utils.FreezePlayer(targetPlayer, false);
        }

        public void OnFinishDespawnAnimation() // Animation
        {
            if (!IsOwner) return;
            logger.LogDebug("OnFinishDespawnAnimation");
            inSpecialAnimation = false;
            creatureVoice.Stop();
            SetVisibility(false);
        }

        public new void SwitchToBehaviourStateOnLocalClient(int stateIndex)
        {
            if (currentBehaviourStateIndex == stateIndex) { return; }

            previousBehaviourStateIndex = currentBehaviourStateIndex;
            currentBehaviourStateIndex = stateIndex;
            currentBehaviourState = enemyBehaviourStates[stateIndex];
            timeSinceSwitchBehaviour = 0f;
        }

        // RPC's

        [ClientRpc]
        public void ChangeTargetPlayerClientRpc(ulong clientId)
        {
            ResetHallucinations();
            SwitchToBehaviourStateOnLocalClient((int)State.Inactive);
            PlayerControllerB player = PlayerFromId(clientId);
            player.insanityLevel = 0f;
            targetPlayer = player;
            logger.LogDebug($"Something: Haunting player with playerClientId: {targetPlayer.playerClientId}; actualClientId: {targetPlayer.actualClientId}");
            ChangeOwnershipOfEnemy(targetPlayer.actualClientId);

            if (IsServer)
            {
                NetworkObject.ChangeOwnership(targetPlayer.actualClientId);
            }
            
            if (!isHauntingLocalPlayer) { return; };
            hudOverlay = Instantiate(BreathingMechanicPrefab).GetComponent<SomethingHUDOverlay>();
        }

        [ClientRpc]
        public void KillPlayerClientRpc()
        {
            creatureVoice.PlayOneShot(disappearSFX, 1f);
            targetPlayer = null;
        }
    }
}