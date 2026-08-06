using HautsFramework;
using HautsTraitsRoyalty;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using VEF.Abilities;
using Verse;

namespace Hauts_VPE
{
    public class AbilityExtension_ExciseTrait : AbilityExtension_AbilityMod
    {
        public override void Cast(GlobalTargetInfo[] targets, VEF.Abilities.Ability ability)
        {
            base.Cast(targets, ability);
            foreach (GlobalTargetInfo globalTargetInfo in targets)
            {
                Pawn pawn = globalTargetInfo.Thing as Pawn;
                if (pawn.story != null && pawn.story.traits.allTraits.Count > 0)
                {
                    List<Trait> allTakeableTraits = new List<Trait>();
                    foreach (Trait t in pawn.story.traits.allTraits)
                    {
                        if (t.def.exclusionTags != null && t.def.exclusionTags.Contains("SexualOrientation"))
                        {
                            continue;
                        }
                        if (t.def != HVTRoyaltyDefOf.HVT_LatentPsychic && !PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def) && !PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def) && !TraitModExtensionUtility.IsExciseTraitExempt(t.def, false))
                        {
                            allTakeableTraits.Add(t);
                        }
                    }
                    if (allTakeableTraits.Count > 0)
                    {
                        Trait traitToRemove = allTakeableTraits.RandomElement();
                        pawn.story.traits.RemoveTrait(traitToRemove, true);
                        pawn.story.traits.RecalculateSuppression();
                    }
                    else
                    {
                        Messages.Message("HVT_ExciseTraitExemptionFailure".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, true);
                    }
                }
                else
                {
                    Messages.Message("HVT_ExciseTraitTraitlessFailure".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, true);
                }
            }
        }
    }
}
