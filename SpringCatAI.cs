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
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        bool grabbingTargetPlayer;
        bool grabbingRight;
        bool targetPlayerInChest;

        // Const
        const float turnSpeed = 15f;
        const float throwForce = 20f;

        // Configs
        float triggerDistance = 5f;
        float distanceToLoseAggro = 20f;

        public enum State
        {
            Inactive,
            Roaming,
            Chasing
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
        }

        public override void Update()
        {
            if (stunnedIndefinitely <= 0)
            {
                if (stunNormalizedTimer >= 0f)
                {
                    stunNormalizedTimer -= Time.deltaTime / enemyType.stunTimeMultiplier;
                }
                else
                {
                    stunnedByPlayer = null;
                    if (postStunInvincibilityTimer >= 0f)
                    {
                        postStunInvincibilityTimer -= Time.deltaTime * 5f;
                    }
                }
            }
            if (!base.IsOwner)
            {
                if (currentSearch.inProgress)
                {
                    StopSearch(currentSearch);
                }
                /*SetClientCalculatingAI(enable: false);
                if (!inSpecialAnimation)
                {
                    if (RoundManager.Instance.currentDungeonType == 4 && Vector3.Distance(base.transform.position, RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint.position) < 1f)
                    {
                        serverPosition += RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint.position - RoundManager.Instance.currentMineshaftElevator.previousElevatorPosition;
                    }
                    base.transform.position = Vector3.SmoothDamp(base.transform.position, serverPosition, ref tempVelocity, syncMovementSpeed);
                    base.transform.eulerAngles = new Vector3(base.transform.eulerAngles.x, Mathf.LerpAngle(base.transform.eulerAngles.y, targetYRotation, 15f * Time.deltaTime), base.transform.eulerAngles.z);
                }*/
                timeSinceSpawn += Time.deltaTime;
                return;
            }
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
                updateDestinationInterval = AIIntervalTime + UnityEngine.Random.Range(-0.015f, 0.015f);
            }

            if (StartOfRound.Instance.allPlayersDead || inSpecialAnimation) { return; }

            if (targetPlayer != null)
            {
                if (currentBehaviourStateIndex == (int)State.Inactive)
                {
                    turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                    MeshTransform.rotation = Quaternion.Lerp(MeshTransform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), turnSpeed * Time.deltaTime);
                }
                else
                {
                    turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), turnSpeed * Time.deltaTime);
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
                            SwitchToBehaviourStateOnLocalClient((int)State.Chasing);
                            BreakOutOfContainmentClientRpc();
                            return;
                        }
                    }

                    break;

                case (int)State.Roaming:

                    if (currentSearch == null || !currentSearch.inProgress)
                    {
                        StartSearch(transform.position);
                    }

                    if (TargetClosestPlayer(1.5f, true))
                    {
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.Chasing);
                    }

                    break;

                case (int)State.Chasing:

                    if (!TargetClosestPlayerInRange(distanceToLoseAggro))
                    {
                        targetPlayer = null;
                        SwitchToBehaviourClientRpc((int)State.Roaming);
                        return;
                    }

                    SetDestinationToPosition(targetPlayer.transform.position);

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

        IEnumerator FreezePlayerCoroutine(float freezeTime)
        {
            FreezePlayer(targetPlayer, true);
            yield return new WaitForSeconds(freezeTime);
            FreezePlayer(targetPlayer, false);
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


        }

        IEnumerator KillTargetPlayerAfterDelay(float delay, int deathAnimation)
        {
            yield return new WaitForSeconds(delay);

            if (targetPlayer != null)
            {
                targetPlayer.KillPlayer(Vector3.zero, true, CauseOfDeath.Inertia, deathAnimation);
            }
        }

        // Animations

        public void SetInSpecialAnimation()
        {
            inSpecialAnimation = true;
        }

        public void UnSetInSpecialAnimation()
        {
            inSpecialAnimation = false;
        }

        public void GrabTargetPlayerRightHand()
        {
            if (targetPlayer == null) { return; }

            inSpecialAnimation = true;
            targetPlayer.playerRigidbody.isKinematic = false;
            grabbingRight = true;
            grabbingTargetPlayer = true;
        }

        public void GrabTargetPlayerLeftHand()
        {
            if (targetPlayer == null) { return; }

            inSpecialAnimation = true;
            targetPlayer.playerRigidbody.isKinematic = false;
            grabbingRight = false;
            grabbingTargetPlayer = true;
        }

        public void DropTargetPlayer()
        {
            if (targetPlayer == null) { return; }

            grabbingTargetPlayer = false;
            targetPlayerInChest = false;
            targetPlayer.playerRigidbody.isKinematic = true;

            if (targetPlayer.isPlayerDead && targetPlayer.deadBody != null)
            {
                targetPlayer.deadBody.attachedTo = null;
                targetPlayer.deadBody.attachedLimb = null;
                targetPlayer.deadBody.secondaryAttachedTo = null;
                targetPlayer.deadBody.secondaryAttachedLimb = null;
                targetPlayer.deadBody.matchPositionExactly = false;
            }
        }

        /*public void ThrowTargetPlayer(Transform throwDirection)
        {
            if (targetPlayer == null) { return; }

            targetPlayer.DropAllHeldItems();
            grabbingTargetPlayer = false;
            targetPlayer.transform.position = throwDirection.position;

            logger.LogDebug("Applying force: " + throwDirection.forward * throwForce);
            targetPlayer.playerRigidbody.velocity = Vector3.zero;
            targetPlayer.externalForceAutoFade += throwDirection.forward * throwForce;
            targetPlayer.playerRigidbody.isKinematic = true;
        }*/

        public void ThrowTargetPlayer(Transform throwDirection) // TODO: Test this
        {
            if (targetPlayer == null) { return; }

            grabbingTargetPlayer = false;
            targetPlayer.playerRigidbody.isKinematic = true;

            if (localPlayer != targetPlayer) { return; }
            localPlayer.KillPlayer(throwDirection.forward * throwForce, true, CauseOfDeath.Inertia);
        }

        public void KillTargetPlayerWithDeathAnimation(int deathAnimation)
        {
            if (targetPlayer == null) { return; }
            if (localPlayer != targetPlayer) { return; }

            localPlayer.KillPlayer(Vector3.zero, true, CauseOfDeath.Mauling, deathAnimation);
        }

        /*public void TearTargetPlayerApart()
        {
            if (targetPlayer == null) { return; }
            if (localPlayer != targetPlayer) { return; }

            localPlayer.KillPlayer(Vector3.zero, true, CauseOfDeath.Mauling, 7);
            PullTargetPlayerBodyApartServerRpc();
        }*/

        public void TearTargetPlayerApart()
        {
            if (targetPlayer == null || targetPlayer.deadBody == null) { logger.LogError("TargetPlayer or dead body is null"); return; }

            targetPlayer.deadBody.attachedTo = RightHandTransform;
            targetPlayer.deadBody.attachedLimb = targetPlayer.deadBody.bodyParts[0];
            targetPlayer.deadBody.secondaryAttachedTo = LeftHandTransform;
            targetPlayer.deadBody.secondaryAttachedLimb = targetPlayer.deadBody.bodyParts[6];
            targetPlayer.deadBody.matchPositionExactly = true;
        }

        public void PullOffTargetPlayerHead()
        {
            if (targetPlayer == null || targetPlayer.deadBody == null) { logger.LogError("TargetPlayer or dead body is null"); return; }

            targetPlayer.deadBody.attachedTo = RightHandTransform;
            targetPlayer.deadBody.attachedLimb = targetPlayer.deadBody.bodyParts[5];
            targetPlayer.deadBody.secondaryAttachedTo = LeftHandTransform;
            targetPlayer.deadBody.secondaryAttachedLimb = targetPlayer.deadBody.bodyParts[0];
            targetPlayer.deadBody.matchPositionExactly = true;
        }

        public void PutTargetPlayerInChest()
        {
            if (targetPlayer == null) { return; }

            inSpecialAnimation = true;
            targetPlayer.playerRigidbody.isKinematic = false;
            targetPlayerInChest = true;
        }

        public void PutTargetPlayerInMouth()
        {
            if (targetPlayer == null || targetPlayer.deadBody == null) { logger.LogError("TargetPlayer or dead body is null"); return; }

            targetPlayer.deadBody.attachedTo = MouthTransform;
            targetPlayer.deadBody.attachedLimb = targetPlayer.deadBody.bodyParts[5];
            targetPlayer.deadBody.matchPositionExactly = true;
        }

        // RPC's

        [ClientRpc]
        void BreakOutOfContainmentClientRpc()
        {
            SwitchToBehaviourStateOnLocalClient((int)State.Chasing);
            Container.SetActive(false);
            MeshTransform.position = Vector3.zero;
            MeshTransform.rotation = Quaternion.identity;
            creatureAnimator.SetTrigger("walk");
            creatureVoice.Play();
        }

        [ServerRpc(RequireOwnership = false)]
        public void PullTargetPlayerBodyApartServerRpc()
        {
            if (!IsServerOrHost) { return; }
            PullTargetPlayerBodyApartClientRpc();
        }

        [ClientRpc]
        public void PullTargetPlayerBodyApartClientRpc()
        {

        }
    }
}