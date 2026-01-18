using GameNetcodeStuff;
using UnityEngine;
using static Something.Plugin;

namespace Something.Enemies
{
    internal class RabbitAI : EnemyAI
    {
#pragma warning disable CS8618
        public Transform turnCompass;
#pragma warning restore CS8618

        public override void Start()
        {
            logger.LogDebug("Rabbit spawned");
            base.Start();

            StartSearch(transform.position);
        }

        public override void Update()
        {
            base.Update();

            if (localPlayer.HasLineOfSightToPosition(transform.position + Vector3.up * 0.25f, 80f, 5, 1f))
            {
                localPlayer.JumpToFearLevel(1f);
            }
        }

        public void LateUpdate()
        {
            turnCompass.LookAt(localPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), turnCompassSpeedGlobal * Time.deltaTime); // Always look at local player
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