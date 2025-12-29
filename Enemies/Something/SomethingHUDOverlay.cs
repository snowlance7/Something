using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Something.Plugin;

namespace Something.Enemies.Something
{
    internal class SomethingHUDOverlay : MonoBehaviour
    {
#pragma warning disable CS8618
        public AudioSource audioSource;
        public AudioClip breathInSFX;
        public AudioClip breathOutSFX;
        public Sprite[] jumpscareSprites;
        public Sprite[] altJumpscareSprites;
        public GameObject panelObj;
        public Image panelImage;
        public Image breathVisual;
        [HideInInspector]
        public Material breathMat;
#pragma warning restore CS8618

        static readonly int LightSizeId = Shader.PropertyToID("_Inset");

        string KeyBind
        {
            get
            {
                return InputControlPath.ToHumanReadableString(SomethingInputs.Instance.BreathKey.bindings[0].path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            }
        }

        bool holdingKey;
        bool showedTooltip;
        float breathProgress;

        // Configs
        const float breathMultiplier = 0.5f;
        float insanityMultiplier = 2.5f;
        const float insanityToShowTooltip = 25f;

        const float fillBreathMin = -0.5f;
        const float fillBreathMax = 0.15f;

        public void Start()
        {
            breathProgress = fillBreathMin;
            breathMat = breathVisual.material;
        }

        public void Update()
        {
            // Tooltip (one-time)
            if (!showedTooltip && localPlayer.insanityLevel >= insanityToShowTooltip)
            {
                HUDManager.Instance.DisplayTip(
                    $"Hold [{KeyBind}] to breath",
                    "Breathe to lower insanity and control hallucinations",
                    false, true, "SomethingModBreathingTip"
                );
                showedTooltip = true;
            }

            // Input edges (press/release)
            bool pressed = SomethingInputs.Instance.BreathKey.WasPressedThisFrame();
            bool released = SomethingInputs.Instance.BreathKey.WasReleasedThisFrame();

            if (pressed || released)
            {
                holdingKey = pressed;

                logger.LogDebug(holdingKey ? "Start Breathing" : "Stop Breathing");

                audioSource.Stop();
                audioSource.clip = holdingKey ? breathInSFX : breathOutSFX;
                audioSource.volume = 0.5f;
                audioSource.Play();
            }

            // Breath progress (single path)
            float target = holdingKey ? fillBreathMax : fillBreathMin;
            float prev = breathProgress;
            breathProgress = Mathf.MoveTowards(breathProgress, target, Time.deltaTime * breathMultiplier);

            // Insanity drain only while actively filling
            if (holdingKey && breathProgress > prev && localPlayer.insanityLevel > 0f)
            {
                insanityMultiplier = localPlayer.isWalking || localPlayer.isSprinting ? 2f : 3f;
                localPlayer.insanityLevel = Mathf.Max(0f, localPlayer.insanityLevel - Time.deltaTime * insanityMultiplier);
                logger.LogDebug("Insanity: " + localPlayer.insanityLevel);
            }

            breathMat.SetFloat(LightSizeId, breathProgress);
        }


        public void JumpscarePlayer(float time, bool destroy = false)
        {
            panelObj.SetActive(true);
            Sprite[] sprites = configMinimalSpoilerVersion.Value ? altJumpscareSprites : jumpscareSprites;
            int index = UnityEngine.Random.Range(0, sprites.Length);
            panelImage.sprite = sprites[index];

            IEnumerator JumpscarePlayerCoroutine(float time)
            {
                yield return new WaitForSeconds(time);
                panelObj.SetActive(false);
                if (destroy)
                    Destroy(gameObject);
            }

            StartCoroutine(JumpscarePlayerCoroutine(time));
        }
    }
}
