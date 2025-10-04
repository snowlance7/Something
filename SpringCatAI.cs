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

// TODO: Stare head rotation is around x: 30
// TODO: StartYPosition: 0.279

namespace Something
{
    internal class SpringCatAI : EnemyAI
    {
        private static ManualLogSource logger = LoggerInstance;

        public static int SpringCatKillIndex = 1;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Transform turnCompass;
        public GameObject ScanNode;
        public Transform MeshTransform;
        public Transform RightHandTransform;
        public Transform LeftHandTransform;
        public Transform ChestTransform;
        public Transform HeadTransform;
        public Transform MouthTransform;
        public GameObject Container;
        public NetworkAnimator networkAnimator;
        public AudioClip GlassBreakSFX;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        BreakerBox? breakerBox;
        bool grabbingTargetPlayer;
        bool grabbingRight;
        bool targetPlayerInChest;

        float timeSinceMeow;
        float timeSinceSwitchLights;

        float timeSinceRecalcPath;

        private Queue<Vector3> etchPath = new Queue<Vector3>();
        private bool followingPath = false;

        // Const
        const float turnSpeed = 15f;
        const float throwForce = 20f;

        // Configs
        float triggerDistance = 5f;
        float meowCooldown = 30f;
        float lightSwitchCooldown = 5f;
        float turnOffLightsDistance = 10f;

        public enum State
        {
            Inactive,
            Active
        }

        public override void Start()
        {
            try
            {
                overlapColliders = new Collider[1];
                thisNetworkObject = NetworkObject;
                thisEnemyIndex = RoundManager.Instance.numberOfEnemiesInScene;
                RoundManager.Instance.numberOfEnemiesInScene++;
                allAINodes = GameObject.FindGameObjectsWithTag("AINode");
                if (!base.IsServer)
                {
                    RoundManager.Instance.SpawnedEnemies.Add(this);
                }
                path1 = new NavMeshPath();
                openDoorSpeedMultiplier = enemyType.doorSpeedMultiplier;
                serverPosition = base.transform.position;
                ventAnimationFinished = true;
            }
            catch (Exception arg)
            {
                logger.LogError($"Error when initializing enemy variables for {base.gameObject.name} : {arg}");
            }

            currentBehaviourStateIndex = (int)State.Inactive;
            breakerBox = UnityEngine.Object.FindObjectOfType<BreakerBox>();
        }

        public override void Update()
        {
            if (currentBehaviourStateIndex == (int)State.Inactive)
            {
                timeSinceMeow += Time.deltaTime;

                if (timeSinceMeow > meowCooldown)
                {
                    creatureVoice.Play();
                    timeSinceMeow = 0;
                }
            }

            if (StartOfRound.Instance.allPlayersDead || inSpecialAnimation) { return; }

            if (targetPlayer != null)
            {
                if (currentBehaviourStateIndex == (int)State.Inactive)
                {
                    turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                    MeshTransform.rotation = Quaternion.Lerp(MeshTransform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), turnSpeed * Time.deltaTime);
                    HeadTransform.rotation = Quaternion.Lerp(HeadTransform.rotation, Quaternion.Euler(new Vector3(turnCompass.eulerAngles.x, turnCompass.eulerAngles.y, turnCompass.eulerAngles.z)), turnSpeed * Time.deltaTime);
                }
                else
                {
                    turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                    //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), turnSpeed * Time.deltaTime);
                    HeadTransform.rotation = Quaternion.Lerp(HeadTransform.rotation, Quaternion.Euler(new Vector3(turnCompass.eulerAngles.x, turnCompass.eulerAngles.y, turnCompass.eulerAngles.z)), turnSpeed * Time.deltaTime);
                }
            }
            else
            {
                HeadTransform.rotation = Quaternion.identity;
            }

            if (!IsServerOrHost) { return; }

            timeSinceSwitchLights += Time.deltaTime;

            if (updateDestinationInterval >= 0f)
            {
                updateDestinationInterval -= Time.deltaTime;
            }
            else
            {
                DoAIInterval();
                updateDestinationInterval = AIIntervalTime + UnityEngine.Random.Range(-0.015f, 0.015f);
            }

            if (targetPlayer == null) { return; }

            if (!followingPath && etchPath.Count > 0)
            {
                Vector3 nextPoint = etchPath.Dequeue();
                agent.SetDestination(nextPoint);
                followingPath = true;
            }

            if (followingPath && !agent.pathPending && agent.remainingDistance < 0.1f)
            {
                followingPath = false;
            }
        }

        public void LateUpdate()
        {
            if (targetPlayer != null)
            {
                if (grabbingTargetPlayer)
                {
                    targetPlayer.transform.position = grabbingRight ? RightHandTransform.position : LeftHandTransform.position; // TODO: Dont do offset or parenting! Change the transform of the handtransform in unity editor
                    Vector3 chestOffset = targetPlayer.transform.position - targetPlayer.bodyParts[5].position;
                    targetPlayer.transform.position += chestOffset;
                }
                else if (targetPlayerInChest)
                {
                    targetPlayer.transform.position = ChestTransform.position;
                }
            }
        }

        void RecalculatePath()
        {
            if (targetPlayer == null) { return; }
            NavMeshPath rawPath = new NavMeshPath();
            agent.CalculatePath(targetPlayer.transform.position, rawPath);

            etchPath.Clear();

            Vector3 current = transform.position;
            foreach (Vector3 corner in rawPath.corners)
            {
                Vector3 direction = corner - current;

                // Add a horizontal (X) move
                if (Mathf.Abs(direction.x) > 0.01f)
                {
                    Vector3 horizontal = new Vector3(corner.x, current.y, current.z);
                    etchPath.Enqueue(horizontal);
                    current = horizontal;
                }

                // Then a vertical (Z) move
                if (Mathf.Abs(direction.z) > 0.01f)
                {
                    Vector3 vertical = new Vector3(current.x, current.y, corner.z);
                    etchPath.Enqueue(vertical);
                    current = vertical;
                }
            }
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();

            if (StartOfRound.Instance.allPlayersDead || inSpecialAnimation)
            {
                return;
            }

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Inactive:

                    if (TargetClosestPlayerInRange(triggerDistance))
                    {
                        if (Vector3.Distance(transform.position, targetPlayer.transform.position) > triggerDistance)
                        {
                            SwitchToBehaviourStateOnLocalClient((int)State.Active);
                            BreakOutOfContainmentClientRpc();
                            return;
                        }
                    }

                    break;

                case (int)State.Active:

                    if (!TargetClosestPlayer())
                    {
                        targetPlayer = null;
                        agent.ResetPath();
                        return;
                    }

                    if (timeSinceSwitchLights > lightSwitchCooldown)
                    {
                        timeSinceSwitchLights = 0f;
                        TurnOffNearbyLightsClientRpc(turnOffLightsDistance);
                    }

                    //SetDestinationToPosition(targetPlayer.transform.position);
                    RecalculatePath();

                    break;

                default:
                    logger.LogWarning("Invalid state: " + currentBehaviourStateIndex);
                    break;
            }
        }

        bool TargetClosestPlayerInRange(float range)
        {
            if (targetPlayer != null && !PlayerIsTargetable(targetPlayer))
            {
                targetPlayer = null;
            }

            float closestDistance = Mathf.Infinity;
            PlayerControllerB? closestPlayer = null;

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null || !PlayerIsTargetable(player)) { continue; }
                if (Utils.isBeta && Utils.DEBUG_disableTargetting && player.isHostPlayerObject) { continue; }
                float distance = Vector3.Distance(player.transform.position, transform.position);
                if (distance <= range && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }

            if (closestPlayer != null)
            {
                targetPlayer = closestPlayer;
            }

            return targetPlayer != null;
        }

        public new bool TargetClosestPlayer(float bufferDistance = 1.5f, bool requireLineOfSight = false, float viewWidth = 70f)
        {
            mostOptimalDistance = 2000f;
            PlayerControllerB playerControllerB = targetPlayer;
            targetPlayer = null;
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                if (Utils.isBeta && Utils.DEBUG_disableTargetting && StartOfRound.Instance.allPlayerScripts[i].isHostPlayerObject) { continue; }
                if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]) && !PathIsIntersectedByLineOfSight(StartOfRound.Instance.allPlayerScripts[i].transform.position, calculatePathDistance: false, avoidLineOfSight: false) && (!requireLineOfSight || CheckLineOfSightForPosition(StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, viewWidth, 40)))
                {
                    tempDist = Vector3.Distance(base.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
                    if (tempDist < mostOptimalDistance)
                    {
                        mostOptimalDistance = tempDist;
                        targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
                    }
                }
            }
            if (targetPlayer != null && bufferDistance > 0f && playerControllerB != null && Mathf.Abs(mostOptimalDistance - Vector3.Distance(base.transform.position, playerControllerB.transform.position)) < bufferDistance)
            {
                targetPlayer = playerControllerB;
            }
            return targetPlayer != null;
        }

        IEnumerator FreezePlayerCoroutine(float freezeTime)
        {
            Utils.FreezePlayer(targetPlayer, true);
            yield return new WaitForSeconds(freezeTime);
            Utils.FreezePlayer(targetPlayer, false);
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
            if (player == null || player != localPlayer || !player.isPlayerControlled) { return; }

            inSpecialAnimation = true;
            KillPlayerServerRpc(player.actualClientId);
        }

        // Animations

        public void SetInSpecialAnimation() // Animation
        {
            logger.LogDebug("SetInSpecialAnimation");
            inSpecialAnimation = true;
        }

        public void UnSetInSpecialAnimation() // Animation
        {
            logger.LogDebug("UnSetInSpecialAnimation");
            inSpecialAnimation = false;
        }

        // TODO: Test all these
        public void GrabTargetPlayerRightHand() // Animation
        {
            logger.LogDebug("GrabTargetPlayerRightHand");
            if (targetPlayer == null) { logger.LogError("TargetPlayer is null"); return; }

            inSpecialAnimation = true;
            targetPlayer.playerRigidbody.isKinematic = false;
            grabbingRight = true;
            grabbingTargetPlayer = true;
            //targetPlayer.transform.SetParent(RightHandTransform);
        }

        public void GrabTargetPlayerLeftHand() // Animation
        {
            logger.LogDebug("GrabTargetPlayerLeftHand");
            if (targetPlayer == null) { logger.LogError("TargetPlayer is null"); return; }

            inSpecialAnimation = true;
            targetPlayer.playerRigidbody.isKinematic = false;
            grabbingRight = false;
            grabbingTargetPlayer = true;
            //targetPlayer.transform.SetParent(LeftHandTransform);
        }

        public void DropTargetPlayer() // Animation
        {
            logger.LogDebug("DropTargetPlayer");
            if (targetPlayer == null) { logger.LogError("TargetPlayer is null"); return; }

            grabbingTargetPlayer = false;
            targetPlayerInChest = false;
            targetPlayer.playerRigidbody.isKinematic = true;
            targetPlayer.transform.rotation = Quaternion.identity;

            if (targetPlayer.isPlayerDead && targetPlayer.deadBody != null)
            {
                targetPlayer.deadBody.attachedTo = null;
                targetPlayer.deadBody.attachedLimb = null;
                targetPlayer.deadBody.secondaryAttachedTo = null;
                targetPlayer.deadBody.secondaryAttachedLimb = null;
                targetPlayer.deadBody.matchPositionExactly = false;
            }
        }

        public void ThrowTargetPlayer() // Animation
        {
            logger.LogDebug("ThrowTargetPlayer");
            if (targetPlayer == null) { logger.LogError("TargetPlayer is null"); return; }

            targetPlayer.playerRigidbody.isKinematic = true;
            grabbingTargetPlayer = false;

            if (localPlayer != targetPlayer) { return; }
            localPlayer.KillPlayer(LeftHandTransform.forward * throwForce, true, CauseOfDeath.Inertia);
        }

        public void KillTargetPlayerWithDeathAnimation(int deathAnimation) // Animation
        {
            logger.LogDebug("KillTargetPlayerWithDeathAnimation: " + deathAnimation);
            if (targetPlayer == null) { logger.LogError("TargetPlayer is null"); return; }
            //if (localPlayer != targetPlayer) { return; }

            grabbingTargetPlayer = false;
            targetPlayer.playerRigidbody.isKinematic = true;

            if (localPlayer != targetPlayer) { return; }
            localPlayer.KillPlayer(Vector3.zero, true, CauseOfDeath.Mauling, deathAnimation);
        }

        public void TearTargetPlayerApart() // Animation
        {
            logger.LogDebug("TearTargetPlayerApart");
            if (targetPlayer == null) { logger.LogError("TargetPlayer is null"); return; }

            grabbingTargetPlayer = false;
            targetPlayer.playerRigidbody.isKinematic = true;

            if (localPlayer == targetPlayer)
            {
                localPlayer.KillPlayer(Vector3.zero, true, CauseOfDeath.Mauling, 7);
            }

            StartCoroutine(GrabBodyPartsCoroutine(6, 0));
        }

        public void PullOffTargetPlayerHead() // Animation
        {
            logger.LogDebug("PullOffTargetPlayerHead");
            if (targetPlayer == null) { logger.LogError("TargetPlayer is null"); return; }

            grabbingTargetPlayer = false;
            targetPlayer.playerRigidbody.isKinematic = true;

            if (localPlayer == targetPlayer)
            {
                localPlayer.KillPlayer(Vector3.zero, true, CauseOfDeath.Mauling, 1);
            }

            StartCoroutine(GrabBodyPartsCoroutine(0, 5));
        }

        IEnumerator GrabBodyPartsCoroutine(int leftHandPart, int rightHandPart)
        {
            logger.LogDebug($"GrabBodyPartsCoroutine: {leftHandPart}, {rightHandPart}");
            yield return null;
            yield return new WaitUntil(() => (targetPlayer.isPlayerDead && targetPlayer.deadBody != null) || !inSpecialAnimation);
            if (!inSpecialAnimation) { logger.LogError("No longer inspecialanimation"); yield break; }
            logger.LogDebug("Continue GrabBodyPartsCoroutine");

            targetPlayer.deadBody.secondaryAttachedTo = LeftHandTransform;
            targetPlayer.deadBody.secondaryAttachedLimb = targetPlayer.deadBody.bodyParts[leftHandPart];

            targetPlayer.deadBody.attachedTo = RightHandTransform;
            targetPlayer.deadBody.attachedLimb = targetPlayer.deadBody.bodyParts[rightHandPart];

            targetPlayer.deadBody.matchPositionExactly = true;
        }

        public void PutTargetPlayerInChest() // Animation
        {
            logger.LogDebug("PutTargetPlayerInChest");
            if (targetPlayer == null) { logger.LogError("TargetPlayer is null"); return; }

            inSpecialAnimation = true;
            targetPlayer.playerRigidbody.isKinematic = false;
            targetPlayerInChest = true;
        }

        public void PutTargetPlayerInMouth() // Animation
        {
            logger.LogDebug("PutTargetPlayerInMouth");
            if (targetPlayer == null) { logger.LogError("TargetPlayer is null"); return; }

            grabbingTargetPlayer = false;
            targetPlayer.playerRigidbody.isKinematic = true;

            if (localPlayer == targetPlayer)
            {
                localPlayer.KillPlayer(Vector3.zero, true, CauseOfDeath.Mauling, 1);
            }

            StartCoroutine(KeepPlayerInMouthForDelay(targetPlayer, 5f));
        }

        IEnumerator KeepPlayerInMouthForDelay(PlayerControllerB player, float delay)
        {
            logger.LogDebug("KeepPlayerInMouthForDelay");
            yield return null;
            yield return new WaitUntil(() => (targetPlayer.isPlayerDead && targetPlayer.deadBody != null) || !inSpecialAnimation);
            if (!inSpecialAnimation) { logger.LogError("No longer inspecialanimation"); yield break; }

            targetPlayer.deadBody.attachedTo = MouthTransform;
            targetPlayer.deadBody.attachedLimb = targetPlayer.deadBody.bodyParts[5];
            targetPlayer.deadBody.matchPositionExactly = true;

            yield return new WaitForSeconds(delay);

            player.deadBody.attachedTo = null;
            player.deadBody.attachedLimb = null;
            player.deadBody.matchPositionExactly = false;
        }

        // RPC's

        [ServerRpc(RequireOwnership = false)]
        public void KillPlayerServerRpc(ulong clientId)
        {
            if (!IsServerOrHost) { return; }
            int index = Utils.testing ? SpringCatAI.SpringCatKillIndex : UnityEngine.Random.Range(1, 7);
            KillPlayerClientRpc(clientId, $"killPlayer{index}");
        }

        [ClientRpc]
        public void KillPlayerClientRpc(ulong clientId, string animationName)
        {
            targetPlayer = PlayerFromId(clientId);
            creatureAnimator.SetTrigger(animationName);
        }

        [ClientRpc]
        public void BreakOutOfContainmentClientRpc()
        {
            SwitchToBehaviourStateOnLocalClient((int)State.Active);
            Container.SetActive(false);
            MeshTransform.localPosition = Vector3.zero;
            MeshTransform.localRotation = Quaternion.identity;
            creatureAnimator.SetTrigger("walk");
            creatureSFX.Play();
            creatureSFX.PlayOneShot(GlassBreakSFX);
        }

        [ClientRpc]
        public void TurnOffNearbyLightsClientRpc(float distance)
        {
            logger.LogDebug("Turning off nearby lights");
            foreach (var light in RoundManager.Instance.allPoweredLightsAnimators)
            {
                if (Vector3.Distance(transform.position, light.transform.position) <= distance)
                {
                    light.SetBool("on", false);
                }
            }

            if (breakerBox != null)
            {
                creatureSFX.PlayOneShot(breakerBox.switchPowerSFX);
            }
        }
    }
}