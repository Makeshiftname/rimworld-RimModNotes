using RimWorld;
using System.Runtime.Remoting.Messaging;
using Verse;

namespace HautsTraits
{
    //conformist triggers every 2 days, setting ideo to the faction's majority ideo
    public class Hediff_Conform : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(120000, delta) && this.pawn.Faction != null && this.pawn.Faction.ideos != null && this.pawn.Faction.ideos.PrimaryIdeo != null && ModsConfig.IdeologyActive && !this.pawn.IsMutant)
            {
                Ideo ideo = this.pawn.Faction.ideos.PrimaryIdeo;
                bool changedIdeo = false;
                if (pawn.ideo.Ideo != ideo)
                {
                    changedIdeo = true;
                }
                pawn.ideo.SetIdeo(ideo);
                if (Current.ProgramState == ProgramState.Playing && changedIdeo && pawn.Faction == Faction.OfPlayer)
                {
                    Messages.Message("HVT_PeriodicConformation".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    //persecution complex' hidden hediff changes severity when not in their faction's ideo majority
    public class HediffCompProperties_IdeoMajoritySeverity : HediffCompProperties
    {
        public HediffCompProperties_IdeoMajoritySeverity()
        {
            this.compClass = typeof(HediffComp_IdeoMajoritySeverity);
        }
        public float severityWhileInMajority = 0.001f;
        public float severityWhileInMinority = 1f;
    }
    public class HediffComp_IdeoMajoritySeverity : HediffComp
    {
        public HediffCompProperties_IdeoMajoritySeverity Props
        {
            get
            {
                return (HediffCompProperties_IdeoMajoritySeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (!ModsConfig.IdeologyActive || this.Pawn.ideo == null)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (!this.Pawn.ShouldHaveIdeo || (this.Pawn.Faction != null && this.Pawn.ideo != null && this.Pawn.Ideo != null && this.Pawn.Faction.ideos != null && this.Pawn.Faction.ideos.PrimaryIdeo != null && this.Pawn.Ideo == this.Pawn.Faction.ideos.PrimaryIdeo))
            {
                this.parent.Severity = this.Props.severityWhileInMajority;
            }
            else
            {
                this.parent.Severity = this.Props.severityWhileInMinority;
            }
        }
    }
}
