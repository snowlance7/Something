using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

namespace Something
{
    internal class SomethingInputs : LcInputActions
    {
        public static SomethingInputs Instance = new SomethingInputs();

        public string BreathKey_BindingDisplayString =>  BreathKey.GetBindingDisplayString(StartOfRound.Instance.localPlayerUsingController ? 1 : 0);

        [InputAction(KeyboardControl.C, Name = "Breathe")]
        public InputAction BreathKey { get; set; } = null!;
    }
}
