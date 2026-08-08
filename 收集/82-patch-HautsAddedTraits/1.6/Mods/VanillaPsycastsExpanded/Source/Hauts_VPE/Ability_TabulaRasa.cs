using HautsFramework;
using HautsTraits;
using HautsTraitsRoyalty;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using VanillaPsycastsExpanded;
using Verse;

namespace Hauts_VPE
{
    public class Ability_TabulaRasa : Ability_TargetCorpse
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            foreach (GlobalTargetInfo globalTargetInfo in targets)
            {
                Corpse corpse = globalTargetInfo.Thing as Corpse;
                Pawn pawn = corpse.InnerPawn;
                if (ResurrectionUtility.TryResurrectWithSideEffects(pawn))
                {
                    if (pawn.story != null)
                    {
                        List<Trait> traitsToRemove = new List<Trait>();
                        foreach (Trait t in pawn.story.traits.allTraits)
                        {
                            if (!TraitModExtensionUtility.IsExciseTraitExempt(t.def, false) || PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def) || PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
                            {
                                traitsToRemove.Add(t);
                            }
                        }
                        foreach (Trait t in traitsToRemove)
                        {
                            pawn.story.traits.RemoveTrait(t);
                        }
                        if (this.pawn.Faction != null && pawn.Faction != null)
                        {
                            pawn.SetFaction(this.pawn.Faction);
                        }
                        if (ModsConfig.IdeologyActive && this.pawn.ideo != null && pawn.ideo != null)
                        {
                            pawn.ideo.SetIdeo(this.pawn.ideo.Ideo);
                        }
                        if (pawn.story.GetBackstory(BackstorySlot.Childhood) != null)
                        {
                            pawn.story.Childhood = HautsTraitsVPEDefOf.HVT_TabulaRasaChild;
                        }
                        if (pawn.story.GetBackstory(BackstorySlot.Adulthood) != null)
                        {
                            pawn.story.Adulthood = HautsTraitsVPEDefOf.HVT_TabulaRasaAdult;
                        }
                    }
                    if (pawn.skills != null)
                    {
                        foreach (SkillRecord sr in pawn.skills.skills)
                        {
                            sr.Level = 0;
                            int randPassion = (int)Math.Ceiling(Rand.Value * 5);
                            if (randPassion <= 2)
                            {
                                sr.passion = Passion.None;
                            } else if (randPassion <= 4) {
                                sr.passion = Passion.Minor;
                            } else {
                                sr.passion = Passion.Major;
                            }
                        }
                        pawn.skills.Notify_SkillDisablesChanged();
                    }
                    if (!this.pawn.health.hediffSet.HasHediff(HautsTraitsVPEDefOf.HVT_Dominicus))
                    {
                        Hediff_Catarina newPawnLink = (Hediff_Catarina)HediffMaker.MakeHediff(HautsTraitsVPEDefOf.HVT_Dominicus, this.pawn, null);
                        this.pawn.health.AddHediff(newPawnLink);
                        newPawnLink.thoseRisen.Add(pawn);
                    } else {
                        Hediff_Catarina pawnLink = (Hediff_Catarina)this.pawn.health.hediffSet.GetFirstHediffOfDef(HautsTraitsVPEDefOf.HVT_Dominicus);
                        pawnLink.thoseRisen.Add(pawn);
                    }
                    pawn.Notify_DisabledWorkTypesChanged();
                    Hediff hediff = HediffMaker.MakeHediff(HautsTraitsVPEDefOf.HVT_TabulaRasaAcumen, pawn, null);
                    HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
                    hediffComp_Disappears.ticksToDisappear = (int)(120000 * this.pawn.GetStatValue(StatDefOf.PsychicSensitivity));
                    pawn.health.AddHediff(hediff, null, null, null);
                    Hediff_TraitGiver hediff2 = (Hediff_TraitGiver)HediffMaker.MakeHediff(HautsTraitsVPEDefOf.HVT_TabulaRasaTraitGiver, pawn, null);
                    pawn.health.AddHediff(hediff2, null, null, null);
                    hediff2.resurrector = this.pawn;
                }
            }
        }
    }
    public class Hediff_Catarina : Hediff
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            foreach (Pawn p in thoseRisen)
            {
                if (p.health.hediffSet.HasHediff(HautsTraitsVPEDefOf.HVT_TabulaRasaTraitGiver))
                {
                    Hediff_TraitGiver traitGiver = (Hediff_TraitGiver)p.health.hediffSet.GetFirstHediffOfDef(HautsTraitsVPEDefOf.HVT_TabulaRasaTraitGiver);
                    traitGiver.resurrector = null;
                }
                p.Kill(null);
            }
            this.pawn.health.RemoveHediff(this);
        }
        public List<Pawn> thoseRisen = new List<Pawn>();
    }
    public class Hediff_TraitGiver : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.pawn.story == null || this.pawn.DevelopmentalStage.Baby() || this.pawn.DevelopmentalStage.Newborn())
            {
                this.Severity = -1f;
            }
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (this.resurrector != null && this.resurrector.health.hediffSet.HasHediff(HautsTraitsVPEDefOf.HVT_Dominicus))
            {
                Hediff_Catarina pawnLink = (Hediff_Catarina)this.resurrector.health.hediffSet.GetFirstHediffOfDef(HautsTraitsVPEDefOf.HVT_Dominicus);
                pawnLink.thoseRisen.Remove(this.pawn);
            }
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(600, delta) && this.pawn.story != null)
            {
                int traitCount = 0;
                foreach (Trait t in this.pawn.story.traits.TraitsSorted)
                {
                    if (!TraitModExtensionUtility.IsExciseTraitExempt(t.def))
                    {
                        traitCount++;
                    }
                }
                if (Rand.MTBEventOccurs(2, 60000f, 600f))
                {
                    TraitDef toGain = null;
                    foreach (TraitDef td in DefDatabase<TraitDef>.AllDefs.InRandomOrder())
                    {
                        if (td.GetGenderSpecificCommonality(this.pawn.gender) > 0f && !TraitModExtensionUtility.IsExciseTraitExempt(td, false) && !this.pawn.story.traits.HasTrait(td))
                        {
                            bool toAdd = true;
                            foreach (Trait t in this.pawn.story.traits.allTraits)
                            {
                                if (t.def.ConflictsWith(td))
                                {
                                    toAdd = false;
                                }
                            }
                            foreach (SkillRecord sr in this.pawn.skills.skills)
                            {
                                if ((sr.TotallyDisabled && td.RequiresPassion(sr.def)) || (sr.passion != Passion.None && td.ConflictsWithPassion(sr.def)))
                                {
                                    toAdd = false;
                                }
                            }
                            if (toAdd)
                            {
                                toGain = td;
                            }
                        }
                    }
                    if (toGain != null)
                    {
                        Trait trait = new Trait(toGain, PawnGenerator.RandomTraitDegree(toGain), false);
                        this.pawn.story.traits.GainTrait(trait);
                        traitCount++;
                        TaggedString message;
                        if (ModCompatibilityUtility.IsHighFantasy())
                        {
                            message = "HVT_TabulaRasadFantasy".Translate(this.pawn.Name.ToStringShort, trait.Label);
                        } else {
                            message = "HVT_TabulaRasad".Translate(this.pawn.Name.ToStringShort, trait.Label);
                        }
                        if (traitCount >= HVT_Mod.settings.traitsMax)
                        {
                            message += "HVT_EndTabulaRasa".Translate(this.pawn.Name.ToStringShort);
                        }
                        Messages.Message(message, this.pawn, MessageTypeDefOf.NeutralEvent, true);
                    }
                }
                if (traitCount >= HVT_Mod.settings.traitsMax)
                {
                    this.Severity = -1f;
                }
            }
        }
        public Pawn resurrector = null;
    }
}
