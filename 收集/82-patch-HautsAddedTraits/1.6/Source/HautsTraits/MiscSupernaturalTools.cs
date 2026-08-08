using RimWorld;
using System.Linq;
using Verse;

namespace HautsTraits
{
    /*these are useful for the Royalty content, flagging a trait or gene as woke, or a trait as trans.
     * If it wasn't obvious, I hadn't figured out shorts yet (e.g. Tradeability). I'd definitely SEEN them by the time I started working on the psychic traits
     * but I think I still wasn't sure at the time if they were really that simple or if dnSpy was just fucking with me and occluding vital information as usual.
     * All this to say, were I to recreate these knowing what I know now, those string fields would instead have been a short that goes like Normal, Woke, Trans, Mythic or something like that.*/
    public class SuperPsychicTrait : DefModExtension
    {
        public SuperPsychicTrait()
        {

        }
        public string descKey;
        public string descKeyFantasy;
        public string category;
    }
    public class SuperPsychicGene : DefModExtension
    {
        public SuperPsychicGene()
        {
        }
        public string category;
        public TraitDef correspondingTrait;
    }
    /*used by a couple mythic transes (and also Everliving creepjoiners), this is just luciferium's heal permanent wounds but markedly faster.
     * Faster is still slow, by RimWorld's modern DLC standards (or by modded standards).*/
    public class HediffCompProperties_FastHealPermanentWounds : HediffCompProperties
    {
        public HediffCompProperties_FastHealPermanentWounds()
        {
            this.compClass = typeof(HediffComp_FastHealPermanentWounds);
        }
    }
    public class HediffComp_FastHealPermanentWounds : HediffComp
    {
        public HediffCompProperties_FastHealPermanentWounds Props
        {
            get
            {
                return (HediffCompProperties_FastHealPermanentWounds)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.ResetTicksToHeal();
        }
        private void ResetTicksToHeal()
        {
            this.ticksToHeal = Rand.Range(5, 10) * 60000;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            this.ticksToHeal -= delta;
            if (this.ticksToHeal <= 0)
            {
                HediffComp_HealPermanentWounds.TryHealRandomPermanentWound(base.Pawn, this.parent.LabelCap);
                this.ResetTicksToHeal();
            }
        }
        public static void TryHealRandomPermanentWound(Pawn pawn, string cause)
        {
            Hediff hediff;
            if (!(from hd in pawn.health.hediffSet.hediffs
                  where hd.IsPermanent() || hd.def.chronic
                  select hd).TryRandomElement(out hediff))
            {
                return;
            }
            HealthUtility.Cure(hediff);
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message("MessagePermanentWoundHealed".Translate(cause, pawn.LabelShort, hediff.Label, pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent, true);
            }
        }
        public override void CompExposeData()
        {
            Scribe_Values.Look<int>(ref this.ticksToHeal, "ticksToHeal", 0, false);
        }
        public override string CompDebugString()
        {
            return "ticksToHeal: " + this.ticksToHeal;
        }
        private int ticksToHeal;
    }
}
