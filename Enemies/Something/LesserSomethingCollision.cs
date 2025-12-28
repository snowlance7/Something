using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Something.Plugin;

namespace Something.Enemies.Something
{
    internal class LesserSomethingCollision : MonoBehaviour
    {
#pragma warning disable CS8618
        public LesserSomethingAI mainScript;
#pragma warning restore CS8618

        public void OnTriggerEnter(Collider other)
        {
            mainScript.OnCollision(other);
        }
    }
}
