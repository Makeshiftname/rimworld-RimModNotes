using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsTraitsRoyalty
{
    /*Animalumes respawn at at an anima tree when they die, killing the tree in the process.
     * On a caravan, we arbitrarily assume there's a 10% chance that an anima tree is nearby (regardless of biome or layer; I'm aware that doesn't necessarily make sense) and so therefore they've a 10% chance to rez if they die on caravan
     * Naturally, it gives them the anima scream mood debuff.*/
    public class Hediff_Animalume : HediffWithComps
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            if (this.pawn.Corpse.Map != null)
            {

                List<Thing> animaTrees = new List<Thing>();
                foreach (Thing t in this.pawn.Corpse.Map.listerThings.AllThings)
                {
                    CompSpawnSubplant css = t.TryGetComp<CompSpawnSubplant>();
                    if (css != null)
                    {
                        CompPsylinkable cpl = t.TryGetComp<CompPsylinkable>();
                        if (cpl != null)
                        {
                            animaTrees.Add(t);
                        }
                    }
                }
                if (animaTrees.Count > 0)
                {
                    Thing animaTree = animaTrees.RandomElement<Thing>();
                    IntVec3 destination = animaTree.Position;
                    FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(this.pawn.Corpse, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
                    dataAttachedOverlay.link.detachAfterTicks = 5;
                    this.pawn.Corpse.Map.flecks.CreateFleck(dataAttachedOverlay);
                    FleckMaker.Static(destination, this.pawn.Corpse.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
                    FleckMaker.Static(destination, this.pawn.Corpse.Map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                    SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(this.pawn.Corpse.Position, this.pawn.Corpse.Map, false));
                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(destination, this.pawn.Corpse.Map, false));
                    animaTree.Kill();
                    this.pawn.Corpse.Position = destination;
                    CompAbilityEffect_Teleport.SendSkipUsedSignal(this.pawn.Corpse.Position, this.pawn.Corpse);
                    ResurrectionUtility.TryResurrect(this.pawn.Corpse.InnerPawn);
                }
            } else if (Rand.Value <= 0.1f) {
                if (ResurrectionUtility.TryResurrect(this.pawn.Corpse.InnerPawn))
                {
                    if (PawnUtility.ShouldSendNotificationAbout(this.pawn))
                    {
                        Messages.Message("HVT_CaravanAnimaRez".Translate(this.pawn), null, MessageTypeDefOf.PositiveEvent, true);
                    }
                    if (this.pawn.needs != null && this.pawn.needs.mood != null)
                    {
                        this.pawn.needs.mood.thoughts.memories.TryGainMemory(DefDatabase<ThoughtDef>.GetNamed("AnimaScream"));
                    }
                }
            }
        }
    }
    /*Phoenixes can resurrect anyone, on a pretty short cooldown. This inflicts all hediffDefs on the pawn (addToBrain does ewisott).
     * This works fairly straightforwardly, except that the resurrection doesn't succeed if the victim has hediffDefToExplode and isn't themself a Phoenix. Instead, it causes E X P L O S I O N.
     * Since that hediff takes one day to elapse, and since it obviously only ticks down while the pawn is alive, this is essentially a per-pawn "you must be alive for one day" cooldown.
     * Derivative of AiScansForTargets, it just looks for nearby pawn corpses who don't have the explosion hediff.*/
    public class CompProperties_AbilityResurrectSideEffect : CompProperties_AbilityAiScansForTargets
    {
        public List<HediffDef> hediffDefs;
        public HediffDef hediffDefToExplode;
        public bool addToBrain = true;
    }
    public class CompAbilityEffect_ResurrectSideEffect : CompAbilityEffect_AiScansForTargets
    {
        public new CompProperties_AbilityResurrectSideEffect Props
        {
            get
            {
                return (CompProperties_AbilityResurrectSideEffect)this.props;
            }
        }
        public override float Range
        {
            get
            {
                return Math.Max(base.Range, 10f);
            }
        }
        public override bool AdditionalQualifiers(Thing thing)
        {
            if (this.parent.pawn.Faction != null && thing is Corpse c && c.InnerPawn != null && c.InnerPawn.Faction != null && c.InnerPawn.Faction == this.parent.pawn.Faction && !c.InnerPawn.health.hediffSet.HasHediff(this.Props.hediffDefToExplode))
            {
                return true;
            }
            return false;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn innerPawn = ((Corpse)target.Thing).InnerPawn;
            if (innerPawn.health.hediffSet.HasHediff(this.Props.hediffDefToExplode) && (innerPawn.story == null || !innerPawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitPhoenix)))
            {
                if (PawnUtility.ShouldSendNotificationAbout(innerPawn))
                {
                    if (ModCompatibilityUtility.IsHighFantasy())
                    {
                        Messages.Message("HVT_PhoenixOverloadFantasy".Translate().CapitalizeFirst().Formatted(innerPawn.Named("PAWN")).AdjustedFor(innerPawn, "PAWN", true).Resolve(), innerPawn, MessageTypeDefOf.NegativeEvent, true);
                    }
                    else
                    {
                        Messages.Message("HVT_PhoenixOverload".Translate().CapitalizeFirst().Formatted(innerPawn.Named("PAWN")).AdjustedFor(innerPawn, "PAWN", true).Resolve(), innerPawn, MessageTypeDefOf.NegativeEvent, true);
                    }
                }
                GenExplosion.DoExplosion(target.Cell, target.Thing.Map, 1f * this.parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity), DamageDefOf.Flame, null, 12, -1f, null, null, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f, preExplosionSpawnSingleThingDef: ThingDefOf.Filth_BlastMark);
                target.Thing.Destroy();
                return;
            }
            if (this.Props.hediffDefs != null)
            {
                foreach (HediffDef h in this.Props.hediffDefs)
                {
                    Hediff hediff = HediffMaker.MakeHediff(h, innerPawn, null);
                    if (this.Props.addToBrain)
                    {
                        innerPawn.health.AddHediff(hediff, innerPawn.health.hediffSet.GetBrain());
                    }
                    else
                    {
                        innerPawn.health.AddHediff(hediff);
                    }
                }
            }
            if (ResurrectionUtility.TryResurrect(innerPawn))
            {
                if (PawnUtility.ShouldSendNotificationAbout(innerPawn))
                {
                    Messages.Message("MessagePawnResurrected".Translate(innerPawn), innerPawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    /*The Seraph transcendence "transfers" to a same-faction pawn on death, preferentially one in the same map/caravan. Can't transfer to a psydeaf pawn, other Seraph, or Wraith.
     * If the new pawn isn't awakened, it awakens them (activating the Latent Psychic trait if it's there, and adding one of the old pawn's awakenings)
     * transferredOut is set on death. This prevents the pawn from retaining the Seraph trait after transferrence has occurred - even if it happened in some weird way in which they didn't need to get resurrected,
     *   which is why the Seraph remover is a PostTick and not a Notify_Resurrected override.*/
    public class Hediff_Seraphim : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.transferredOut == true && this.pawn.story != null)
            {
                this.pawn.story.traits.RemoveTrait(this.pawn.story.traits.GetTrait(HVTRoyaltyDefOf.HVT_TTraitSeraph));
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.pawn.Faction != null && this.pawn.story != null && this.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitSeraph))
            {
                List<Pawn> eligiblePawns = new List<Pawn>();
                if (this.pawn.MapHeld != null)
                {
                    foreach (Pawn p in this.pawn.MapHeld.mapPawns.AllPawns)
                    {
                        if (p.Faction != null && p.Faction == this.pawn.Faction && p != this.pawn && p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                } else if (this.pawn.GetCaravan() != null) {
                    foreach (Pawn p in this.pawn.GetCaravan().PawnsListForReading)
                    {
                        if (p.Faction != null && p.Faction == this.pawn.Faction && p != this.pawn && p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                } else {
                    foreach (Map map in Find.Maps)
                    {
                        foreach (Pawn p in map.mapPawns.AllPawns)
                        {
                            if (p.Faction != null && p.Faction == this.pawn.Faction && p != this.pawn && p.story != null)
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    }
                    foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
                    {
                        if (p.Faction != null && p.Faction == this.pawn.Faction && p != this.pawn && p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                }
                if (eligiblePawns.Count > 0)
                {
                    List<Pawn> pawnsToRemove = new List<Pawn>();
                    if (eligiblePawns.Contains(this.pawn))
                    {
                        pawnsToRemove.Add(this.pawn);
                    }
                    foreach (Pawn p in eligiblePawns)
                    {
                        if (p.GetStatValue(StatDefOf.PsychicSensitivity) < 1E-45f || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitSeraph) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWraith))
                        {
                            pawnsToRemove.Add(p);
                        }
                    }
                    foreach (Pawn p in pawnsToRemove)
                    {
                        pawnsToRemove.Remove(p);
                    }
                    if (eligiblePawns.Count > 0)
                    {
                        Pawn newHost = eligiblePawns.RandomElement();
                        if (!PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(newHost))
                        {
                            if (newHost.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                            {
                                AwakeningMethodsUtility.AwakenPsychicTalent(newHost, true, "HVT_WokeningDefault", "HVT_WokeningDefault");
                            }
                            List<TraitDef> awakenings = new List<TraitDef>();
                            foreach (Trait t in this.pawn.story.traits.allTraits)
                            {
                                if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def))
                                {
                                    awakenings.Add(t.def);
                                }
                            }
                            if (awakenings.Count == 0)
                            {
                                if (!PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(newHost))
                                {
                                    AwakeningMethodsUtility.AwakenPsychicTalent(newHost, false, "", "", true);
                                }
                            }
                            else
                            {
                                newHost.story.traits.GainTrait(new Trait(awakenings.RandomElement()));
                            }
                        }
                        newHost.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_TTraitSeraph));
                        this.transferredOut = true;
                        if (newHost.Faction == Faction.OfPlayerSilentFail)
                        {
                            LookTargets lt;
                            if (newHost.Spawned)
                            {
                                lt = newHost;
                            }
                            else
                            {
                                lt = null;
                            }
                            string how = ModCompatibilityUtility.IsHighFantasy() ? "HVT_SeraphTextFantasy".Translate(newHost.Name.ToStringFull, this.pawn.Name.ToStringFull).CapitalizeFirst() : "HVT_SeraphText".Translate(newHost.Name.ToStringFull, this.pawn.Name.ToStringFull).CapitalizeFirst();
                            ChoiceLetter notification = LetterMaker.MakeLetter(
                    "HVT_SeraphLabel".Translate(newHost.Name.ToStringShort), how, LetterDefOf.PositiveEvent, lt, null, null, null);
                            Find.LetterStack.ReceiveLetter(notification, null);

                        }
                    }
                }
            }
        }
        public bool transferredOut = false;
    }
    /*The Wraith transcendence also transfers out, preferrably to a pawn of a DIFFERENT faction in the same map/caravan. That's handled via Harmony patch.
     * This has the same transferredOut thing, but since the Wraith's entire "selfhood" or soul or whatever is gone, it also makes sure to remove all their transcendences and woke traits.
     * In the case that the Wraith had woke genes but no traits - and therefore transferred one of its woke genes into the new host - that gene is stored here as geneToRemove, and it gets thrown out too.
     * The post-Wraith pawn is left as just a Latent Psychic, starting again from the very beginning with a randomized awakening condition.*/
    public class Hediff_Wraithly : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.transferredOut == true && this.pawn.story != null)
            {
                List<Trait> traitsToRemove = new List<Trait>();
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
                    {
                        traitsToRemove.Add(t);
                    }
                }
                foreach (Trait t in traitsToRemove)
                {
                    pawn.story.traits.RemoveTrait(t);
                }
                traitsToRemove.Clear();
                if (ModsConfig.BiotechActive && this.pawn.genes != null && geneToRemove != null)
                {
                    this.pawn.genes.RemoveGene(this.pawn.genes.GetGene(geneToRemove));
                }
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def))
                    {
                        traitsToRemove.Add(t);
                    }
                }
                foreach (Trait t in traitsToRemove)
                {
                    pawn.story.traits.RemoveTrait(t);
                }
                if (!PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(this.pawn))
                {
                    pawn.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_LatentPsychic, PawnGenerator.RandomTraitDegree(HVTRoyaltyDefOf.HVT_LatentPsychic)));
                }
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            this.Severity = 0.002f;
        }
        public bool transferredOut = false;
        public GeneDef geneToRemove = null;
    }
}
