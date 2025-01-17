using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using static Something.Plugin;

namespace Something
{
    internal class BreathingBehavior : MonoBehaviour
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Slider BreathSlider;
        public GameObject SliderObj;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        bool active;
        const float multiplier = 0.5f;
        const float insanityMultiplier = 2f;
        bool holdingKey;

        public void Update()
        {
            if (SomethingInputs.Instance.BreathKey.WasPressedThisFrame()/* && localPlayer.CheckConditionsForEmote()*/)
            {
                LoggerInstance.LogDebug("Start Breathing");
                holdingKey = true;
            }
            else if (SomethingInputs.Instance.BreathKey.WasReleasedThisFrame())
            {
                LoggerInstance.LogDebug("Stop Breathing");
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
    }
}
