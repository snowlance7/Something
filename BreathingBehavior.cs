using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using static Something.Plugin;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Something
{
    internal class BreathingBehavior : MonoBehaviour
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Slider BreathSlider;
        public GameObject SliderObj;
        public AudioClip BreathInSFX;
        public AudioClip BreathOutSFX;
        public Sprite[] JumpscareSprites;
        public GameObject PanelObj;
        public UnityEngine.UI.Image PanelImage;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        const float insanityToShowTooltip = 10f;

        bool active;
        bool holdingKey;
        string keyBind = "";
        bool showedTooltip;

        // Configs
        float multiplier = 0.5f;
        float insanityMultiplier = 2.5f;

        public void Start()
        {
            keyBind = InputControlPath.ToHumanReadableString(SomethingInputs.Instance.BreathKey.bindings[0].path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        public void Update()
        {
            if (showedTooltip || localPlayer.insanityLevel >= insanityToShowTooltip)
            {
                HUDManager.Instance.ChangeControlTip(HUDManager.Instance.controlTipLines.Length - 1, $"Breath [{keyBind}]");
                if (!showedTooltip)
                {
                    HUDManager.Instance.DisplayTip("???", $"Hold [{keyBind}] to breath", false, true, "SomethingTip");
                }
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
                if (BreathSlider.value < 1f)
                {
                    BreathSlider.value += Time.deltaTime * multiplier;
                    if (localPlayer.insanityLevel > 0)
                    {
                        localPlayer.insanityLevel -= Time.deltaTime * insanityMultiplier;
                        LoggerInstance.LogDebug("Insanity: " + localPlayer.insanityLevel);
                    }
                }

                if (BreathSlider.value > 0 && !active)
                {
                    SliderObj.SetActive(true);
                    active = true;
                }
            }
            else
            {
                if (BreathSlider.value > 0)
                {
                    BreathSlider.value -= Time.deltaTime * multiplier;
                }

                if (BreathSlider.value <= 0 && active == true)
                {
                    SliderObj.SetActive(false);
                    active = false;
                }
            }
        }

        public void JumpscarePlayer()
        {
            PanelObj.SetActive(true);
            int index = UnityEngine.Random.Range(0, JumpscareSprites.Length);
            PanelImage.sprite = JumpscareSprites[index];
        }
    }
}
