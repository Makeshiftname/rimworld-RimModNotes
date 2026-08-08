using HautsFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace HautsTraitsRoyalty
{
    //since LP is actually six traits in a trenchcoat, the regular ForcedTrait scenario part will only give you one awakening type for all affected pawns. This gives random awakening types instead
    public class ScenPart_ForcedLatentPsychic : ScenPart_PawnModifier
    {
        public override string Summary(Scenario scen)
        {
            if (ModCompatibilityUtility.IsHighFantasy())
            {
                return "HVT_ForcedLatentPsychicFantasy".Translate();
            }
            return "HVT_ForcedLatentPsychic".Translate();
        }
        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 2f);
            base.DoPawnModifierEditInterface(scenPartRect.BottomPartPixels(ScenPart.RowHeight * 2f));
        }
        protected override void ModifyPawnPostGenerate(Pawn pawn, bool redressed)
        {
            if (pawn.story == null || pawn.story.traits == null)
            {
                return;
            }
            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                return;
            }
            int degree = (int)Rand.Range(1, 6);
            if (degree == 1 && pawn.skills != null)
            {
                foreach (SkillRecord sr in pawn.skills.skills)
                {
                    if (sr.Level >= 15)
                    {
                        degree = (int)Rand.Range(2, 6);
                        break;
                    }
                }
            }
            pawn.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_LatentPsychic, degree, true), false);
            if (!pawn.story.traits.allTraits.NullOrEmpty())
            {
                for (int i = pawn.story.traits.allTraits.Count - 1; i >= 0; i--)
                {
                    Trait t = pawn.story.traits.allTraits[i];
                    if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
                    {
                        pawn.story.traits.RemoveTrait(t);
                    }
                }
                for (int i = pawn.story.traits.allTraits.Count - 1; i >= 0; i--)
                {
                    Trait t = pawn.story.traits.allTraits[i];
                    if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def))
                    {
                        pawn.story.traits.RemoveTrait(t);
                    }
                }
            }
        }
    }
    //ditto, just without having to deal with the front-end pretense that these are all really the same trait
    public class ScenPart_ForcedAwakenedPsychic : ScenPart_PawnModifier
    {
        public override string Summary(Scenario scen)
        {
            if (ModCompatibilityUtility.IsHighFantasy())
            {
                return "HVT_ForcedAwakenedPsychicFantasy".Translate();
            }
            return "HVT_ForcedAwakenedPsychic".Translate();
        }
        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 2f);
            base.DoPawnModifierEditInterface(scenPartRect.BottomPartPixels(ScenPart.RowHeight * 2f));
        }
        protected override void ModifyPawnPostGenerate(Pawn pawn, bool redressed)
        {
            if (pawn.story == null || pawn.story.traits == null || pawn.IsMutant)
            {
                return;
            }
            if (!PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
            {
                AwakeningMethodsUtility.AwakenPsychicTalent(pawn, false, "", "", true);
            }
            if (!pawn.story.traits.allTraits.NullOrEmpty())
            {
                for (int i = pawn.story.traits.allTraits.Count - 1; i >= 0; i--)
                {
                    Trait t = pawn.story.traits.allTraits[i];
                    if (t.def == HVTRoyaltyDefOf.HVT_LatentPsychic)
                    {
                        pawn.story.traits.RemoveTrait(t);
                    }
                }
            }
        }
    }
}
