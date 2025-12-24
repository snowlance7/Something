using UnityEngine;
using UnityEngine.UI;

namespace Something
{
    internal class TestingHUDOverlay : MonoBehaviour
    {
        public static TestingHUDOverlay? Instance;

#pragma warning disable CS8618
        public GameObject toggle1Obj;
        public Text toggle1Label;
        public Toggle toggle1;

        public GameObject toggle2Obj;
        public Text toggle2Label;
        public Toggle toggle2;

        public Text label1;

        public Text label2;

        public Text label3;
#pragma warning restore CS8618

        public void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Update()
        {
            toggle1Obj.SetActive(toggle1Label.text != "");
            toggle2Obj.SetActive(toggle2Label.text != "");
        }
    }
}
