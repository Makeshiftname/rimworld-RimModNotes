using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using VEF.Abilities;
using Verse;

namespace Hauts_VPE
{
    //Words of Ennui, Verve, and Genius put all the targeted pawns into a single GiveHediffFromMenu menu
    public class AbilityExtension_GiveHediffFromMenu : AbilityExtension_AbilityMod
    {
        public override void Cast(GlobalTargetInfo[] targets, VEF.Abilities.Ability ability)
        {
            if (targets.NullOrEmpty())
            {
                return;
            }
            List<Pawn> pawns = new List<Pawn>();
            foreach (GlobalTargetInfo gti in targets)
            {
                if (gti.Thing != null && gti.Thing is Pawn p)
                {
                    pawns.Add(p);
                }
            }
            if (ability.pawn.IsPlayerControlled)
            {
                Find.WindowStack.Add(new Dialog_GiveHediffFromMenu(null, pawns, ability.pawn, ability.pawn, this.props.hediffs, this.props.menuString, this.props.menuStringPlural, this.props.removeExistingOptionsFromPawn, this.props.removeThisAfterGrantingOption, ability.GetDurationForPawn(),this.targetDurationScalar));
            } else {
                HediffDef hd = this.props.hediffs.RandomElement();
                foreach (Pawn p in pawns)
                {
                    HautsMiscUtility.AddHediffFromMenu(hd, p, ability.GetDurationForPawn(),this.targetDurationScalar, ability.pawn, ability.pawn, this.props.removeExistingOptionsFromPawn ? this.props.hediffs : null);
                    if (this.props.removeThisAfterGrantingOption != null)
                    {
                        foreach (Hediff h in p.health.hediffSet.hediffs)
                        {
                            if (h.def == this.props.removeThisAfterGrantingOption)
                            {
                                p.health.RemoveHediff(h);
                                break;
                            }
                        }
                    }
                }
            }
        }
        public CompProperties_AbilityGiveHediffFromMenu props;
        public StatDef targetDurationScalar;
    }
}
