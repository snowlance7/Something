using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

namespace Something
{
    internal class SomethingInputs : LcInputActions
    {
#pragma warning disable CS8618
        public static SomethingInputs Instance;

        public static void Init()
        {
            Instance = new SomethingInputs();
        }

        [InputAction(KeyboardControl.X, Name = "Breathe")]
        public InputAction BreathKey { get; set; }
#pragma warning restore CS8618
    }
}
