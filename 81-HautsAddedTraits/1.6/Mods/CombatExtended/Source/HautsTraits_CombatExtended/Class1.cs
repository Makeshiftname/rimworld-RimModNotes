using CombatExtended;
using Verse;

namespace HautsTraits_CombatExtended
{
    [StaticConstructorOnStartup]
    public static class HautsTraits_CombatExtended
    {
    }
    public class Verb_Shoot_NeuralHeatAmmo_CE : Verb_ShootCE
    {
        public override bool Available()
        {
            if (this.CasterIsPawn && this.CasterPawn.psychicEntropy.EntropyValue < 1f)
            {
                return false;
            }
            return base.Available();
        }
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (this.CasterIsPawn)
            {
                this.CasterPawn.psychicEntropy.TryAddEntropy(-0.5f);
            }
        }
    }
}
