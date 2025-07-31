﻿using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;
using static Something.Plugin;

namespace Something
{
    internal class RabbitAI : EnemyAI
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Transform turnCompass;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public enum State
        {
            Roaming
        }

        public override void Start()
        {
            logger.LogDebug("Rabbit spawned");
            base.Start();

            currentBehaviourStateIndex = (int)State.Roaming;
            StartSearch(transform.position);
        }

        public override void Update()
        {
            base.Update();

            turnCompass.LookAt(localPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime); // Always look at local player

            if (localPlayer.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.25f, 80f, 5, 1f))
            {
                localPlayer.JumpToFearLevel(1f);
            }
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

            if (playerWhoHit != null && playerWhoHit == localPlayer)
            {
                playerWhoHit.KillPlayer(Vector3.zero, true, CauseOfDeath.Mauling, 7);
            }
        }
    }
}