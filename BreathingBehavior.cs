﻿using UnityEngine;
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
        public TextMeshPro BreathingTooltip;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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
        float multiplier = 0.5f;
        float insanityMultiplier = 2.5f;

        public void Update()
        {
            if (showedTooltip || localPlayer.insanityLevel >= insanityToShowTooltip)
            {
                if (configShowBreathingTooltip.Value)
                {
                    BreathingTooltip.text = $"Breath: [{KeyBind}]";
                }
                if (!showedTooltip)
                {
                    HUDManager.Instance.DisplayTip("???", $"Hold [{KeyBind}] to breath", false, true, "SomethingModTip1");
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
