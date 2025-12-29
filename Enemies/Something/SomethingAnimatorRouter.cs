using UnityEngine;

namespace Something.Enemies.Something
{
    internal class SomethingAnimatorRouter : MonoBehaviour
    {
#pragma warning disable CS8618
        public SomethingAI mainScript;
#pragma warning restore CS8618

        public void OnFinishSpawnAnimation() // Animation
        {
            mainScript.OnFinishSpawnAnimation();
        }

        public void OnFinishDespawnAnimation() // Animation
        {
            mainScript.OnFinishDespawnAnimation();
        }
    }
}
