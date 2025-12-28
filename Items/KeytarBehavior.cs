using GameNetcodeStuff;
using UnityEngine;

namespace Something.Items
{
    internal class KeytarBehavior : PhysicsProp
    {
#pragma warning disable CS8618
        public AudioSource ItemAudio;
        public AudioClip[] songs;

        PlayerControllerB previousPlayerHeldBy;
#pragma warning restore CS8618

        int songIndex;

        // reelingUp
        public override void EquipItem()
        {
            base.EquipItem();
            previousPlayerHeldBy = playerHeldBy;
            previousPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", true);
        }

        public override void DiscardItem()
        {
            previousPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", false);
            base.DiscardItem();
        }

        public override void PocketItem()
        {
            previousPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", false);
            base.PocketItem();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (buttonDown)
            {
                ItemAudio.Stop();
                ItemAudio.clip = songs[songIndex];
                ItemAudio.Play();

                if (songIndex >= songs.Length - 1)
                {
                    songIndex = 0;
                }
                else
                {
                    songIndex++;
                }
            }
        }
    }
}