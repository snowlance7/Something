using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using static Something.Plugin;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

namespace Something
{
    internal class SomethingHUDOverlay : MonoBehaviour
    {
#pragma warning disable CS8618
        public Image breathingVisual;
        public AudioClip BreathInSFX;
        public AudioClip BreathOutSFX;
        public Sprite[] JumpscareSprites;
        public GameObject PanelObj;
        public Image PanelImage;
#pragma warning restore CS8618

        float breathProgress
        {
            get
            {
                return breathingVisual.material.GetFloat("_LightSize");
            }
            set
            {
                breathingVisual.material.SetFloat("_LightSize", value);
            }
        }

        string KeyBind
        {
            get
            {
                return InputControlPath.ToHumanReadableString(SomethingInputs.Instance.BreathKey.bindings[0].path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            }
        }

        const float insanityToShowTooltip = 10f;

        bool active;
        bool holdingKey;
        bool showedTooltip;

        // Configs
        const float multiplier = 0.5f;
        const float insanityMultiplier = 2.5f;
        const float fillBreathMax = 0.9f;
        const float fillBreathMin = 0.2f;

        public void Update()
        {
            if (!showedTooltip && localPlayer.insanityLevel >= insanityToShowTooltip)
            {
                HUDManager.Instance.DisplayTip($"Hold [{KeyBind}] to breath", "Breathe to lower insanity and control hallucinations", false, true, "SomethingModBreathingTip");
                showedTooltip = true;
            }

            if (SomethingInputs.Instance.BreathKey.WasPressedThisFrame())
            {
                LoggerInstance.LogDebug("Start Breathing");
                localPlayer.movementAudio.Stop();
                localPlayer.movementAudio.loop = false;
                localPlayer.movementAudio.clip = BreathInSFX;
                localPlayer.movementAudio.Play();
                holdingKey = true;
            }
            else if (SomethingInputs.Instance.BreathKey.WasReleasedThisFrame())
            {
                LoggerInstance.LogDebug("Stop Breathing");
                localPlayer.movementAudio.Stop();
                localPlayer.movementAudio.loop = false;
                localPlayer.movementAudio.clip = BreathOutSFX;
                localPlayer.movementAudio.Play();
                holdingKey = false;
            }

            if (holdingKey)
            {
                if (breathProgress < fillBreathMax)
                {
                    breathProgress = Mathf.Clamp(breathProgress + (Time.deltaTime * multiplier), fillBreathMin, fillBreathMax);
                    if (localPlayer.insanityLevel > 0)
                    {
                        localPlayer.insanityLevel -= Time.deltaTime * insanityMultiplier;
                        LoggerInstance.LogDebug("Insanity: " + localPlayer.insanityLevel);
                    }
                }
            }
            else
            {
                if (breathProgress > fillBreathMin)
                {
                    breathProgress = Mathf.Clamp(breathProgress - (Time.deltaTime * multiplier), fillBreathMin, fillBreathMax);
                }
            }
        }

        public void JumpscarePlayer(float time)
        {
            PanelObj.SetActive(true);
            int index = UnityEngine.Random.Range(0, JumpscareSprites.Length);
            PanelImage.sprite = JumpscareSprites[index];
            StartCoroutine(JumpscarePlayerCoroutine(time));
        }
        
        IEnumerator JumpscarePlayerCoroutine(float time)
        {
            yield return new WaitForSeconds(time);
            PanelObj.SetActive(false);
            Destroy(this.gameObject);
        }
    }
}
