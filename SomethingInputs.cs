using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

namespace Something
{
    internal class SomethingInputs : LcInputActions
    {
        public static SomethingInputs Instance;

        public static void Init()
        {
            Instance = new SomethingInputs();
        }

        [InputAction(KeyboardControl.C, Name = "Calm Down")]
        public InputAction BreathKey { get; set; }
    }
}
