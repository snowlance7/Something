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
        public Material breathMat;

        // Configs
        const float multiplier = 0.5f;
        const float insanityMultiplier = 2.5f;
        const float fillBreathMax = 0.15f;
        const float fillBreathMin = -0.5f;
        const float insanityToShowTooltip = 25f;

        public void Start()
        {
            breathProgress = fillBreathMin;
            breathMat = breathVisual.material;
        }

        /*public void Update()
        {
            if (!showedTooltip && localPlayer.insanityLevel >= insanityToShowTooltip)
            {
                HUDManager.Instance.DisplayTip($"Hold [{KeyBind}] to breath", "Breathe to lower insanity and control hallucinations", false, true, "SomethingModBreathingTip");
                showedTooltip = true;
            }

            if (SomethingInputs.Instance.BreathKey.WasPressedThisFrame())
            {
                logger.LogDebug("Start Breathing");
                audioSource.Stop();
                audioSource.clip = breathInSFX;
                audioSource.Play();
                holdingKey = true;
            }
            else if (SomethingInputs.Instance.BreathKey.WasReleasedThisFrame())
            {
                logger.LogDebug("Stop Breathing");
                audioSource.Stop();
                audioSource.clip = breathOutSFX;
                audioSource.Play();
                holdingKey = false;
            }

            if (holdingKey)
            {
                if (breathProgress < fillBreathMax)
                {
                    breathProgress = Mathf.Clamp(breathProgress + Time.deltaTime * multiplier, fillBreathMin, fillBreathMax);
                    if (localPlayer.insanityLevel > 0)
                    {
                        localPlayer.insanityLevel -= Time.deltaTime * insanityMultiplier;
                        logger.LogDebug("Insanity: " + localPlayer.insanityLevel);
                    }
                }
            }
            else
            {
                if (breathProgress > fillBreathMin)
                {
                    breathProgress = Mathf.Clamp(breathProgress - Time.deltaTime * multiplier, fillBreathMin, fillBreathMax);
                }
            }

            breathMat.SetFloat(LightSizeId, breathProgress);
        }*/
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
                audioSource.Play();
            }

            // Breath progress (single path)
            float target = holdingKey ? fillBreathMax : fillBreathMin;
            float prev = breathProgress;
            breathProgress = Mathf.MoveTowards(breathProgress, target, Time.deltaTime * multiplier);

            // Insanity drain only while actively filling
            if (holdingKey && breathProgress > prev && localPlayer.insanityLevel > 0f)
            {
                localPlayer.insanityLevel = Mathf.Max(0f, localPlayer.insanityLevel - Time.deltaTime * insanityMultiplier);
                logger.LogDebug("Insanity: " + localPlayer.insanityLevel);
            }

            breathMat.SetFloat(LightSizeId, breathProgress);
        }


        public void JumpscarePlayer(float time)
        {
            panelObj.SetActive(true);
            Sprite[] sprites = configMinimalSpoilerVersion.Value ? altJumpscareSprites : jumpscareSprites;
            int index = UnityEngine.Random.Range(0, sprites.Length);
            panelImage.sprite = sprites[index];

            IEnumerator JumpscarePlayerCoroutine(float time)
            {
                yield return new WaitForSeconds(time);
                panelObj.SetActive(false);
                Destroy(gameObject);
            }

            StartCoroutine(JumpscarePlayerCoroutine(time));
        }
    }
}
