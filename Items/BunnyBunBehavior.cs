namespace Something.Items
{
    internal class BunnyBunBehavior : PhysicsProp
    {
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            //int rockBaby = buttonDown ? 1 : 0;
            //playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", rockBaby);
        }

        public override void GrabItem()
        {
            base.GrabItem();
            //playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 0);
        }
    }
}
