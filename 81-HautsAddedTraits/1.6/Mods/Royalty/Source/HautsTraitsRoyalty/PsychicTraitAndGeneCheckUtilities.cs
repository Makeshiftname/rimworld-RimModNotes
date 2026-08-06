using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HautsTraitsRoyalty
{
    public static class PsychicTraitAndGeneCheckUtility
    {
        //"does this trait or gene apply an offset or factor reduction to psychic sensitivity"
        public static bool IsAntipsychicTrait(TraitDef def, int degree, bool ignoreAwakenedTraits = true)
        {
            if (ignoreAwakenedTraits && awakenings.Contains(def))
            {
                return false;
            }
            if (def.DataAtDegree(degree).statFactors != null)
            {
                foreach (StatModifier sm in def.DataAtDegree(degree).statFactors)
                {
                    if (sm.stat == StatDefOf.PsychicSensitivity && sm.value < 1f)
                    {
                        return true;
                    }
                }
            }
            if (def.DataAtDegree(degree).statOffsets != null)
            {
                foreach (StatModifier sm in def.DataAtDegree(degree).statOffsets)
                {
                    if (sm.stat == StatDefOf.PsychicSensitivity && sm.value < 0f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsAntipsychicGene(GeneDef def, bool ignoreAwakenedGenes = true)
        {
            if (ignoreAwakenedGenes && IsAwakenedPsychicGene(def))
            {
                return false;
            }
            if (def.forcedTraits != null)
            {
                foreach (GeneticTraitData gtd in def.forcedTraits)
                {
                    if (PsychicTraitAndGeneCheckUtility.IsAntipsychicTrait(gtd.def, gtd.degree))
                    {
                        return true;
                    }
                }
            }
            if (def.statFactors != null)
            {
                foreach (StatModifier sm in def.statFactors)
                {
                    if (sm.stat == StatDefOf.PsychicSensitivity && sm.value < 1f)
                    {
                        return true;
                    }
                }
            }
            if (def.statOffsets != null)
            {
                foreach (StatModifier sm in def.statOffsets)
                {
                    if (sm.stat == StatDefOf.PsychicSensitivity && sm.value < 0f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //"are you awakened"
        public static bool IsAwakenedPsychic(Pawn pawn, bool checkGenes = true)
        {
            foreach (Trait x in pawn.story.traits.allTraits)
            {
                if (IsAwakenedTrait(x.def))
                {
                    return true;
                }
            }
            if (checkGenes && (HasAwakenedPsychicGenes(pawn)))
            {
                return true;
            }
            return false;
        }
        public static bool IsAwakenedTrait(TraitDef traitDef)
        {
            if (awakenings.Contains(traitDef))
            {
                return true;
            }
            return false;
        }
        public static bool HasAwakenedPsychicGenes(Pawn pawn)
        {
            if (ModsConfig.BiotechActive && pawn.genes != null && ContainsPsychicGene(pawn.genes))
            {
                return true;
            }
            return false;
        }
        public static bool ContainsPsychicGene(Pawn_GeneTracker genes)
        {
            foreach (Gene g in genes.GenesListForReading)
            {
                if (IsAwakenedPsychicGene(g.def))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsAwakenedPsychicGene(GeneDef geneDef)
        {
            if (wokeGenes.Contains(geneDef))
            {
                return true;
            }
            return false;
        }
        //"are you transcendent"
        public static bool IsTranscendent(Pawn pawn)
        {
            if (pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (IsTranscendentTrait(t.def))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsTranscendentTrait(TraitDef traitDef)
        {
            return regularTranses.Contains(traitDef) || mythicTranses.Contains(traitDef);
        }
        //"are you, specifically, a Mythic transcendence"
        public static bool IsMythicTranscendent(Pawn pawn)
        {
            if (pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (IsMythicTrait(t))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsMythicTrait(Trait trait)
        {
            return mythicTranses.Contains(trait.def);
        }
        //read these private trait lists
        public static List<TraitDef> AwakenedTraitList()
        {
            return awakenings;
        }
        public static List<TraitDef> RegularTranscendentTraitList()
        {
            return regularTranses;
        }
        public static List<TraitDef> MythicTranscendentTraitList()
        {
            return mythicTranses;
        }
        public static List<TraitDef> AllTranscendentTraitList()
        {
            List<TraitDef> blah = regularTranses;
            blah.AddRange(mythicTranses);
            return blah;
        }
        public static List<GeneDef> AwakenedGeneList()
        {
            return wokeGenes;
        }
        public static int AwakenedTraitCount()
        {
            return awakenings.Count;
        }
        public static int TranscendentTraitCount()
        {
            return regularTranses.Count;
        }
        public static int MythicTransTraitCount()
        {
            return mythicTranses.Count;
        }
        //the following tools for modders adding new awakenings or transcendences
        public static void AddAwakeningTrait(TraitDef def)
        {
            awakenings.Add(def);
        }
        public static void AddTranscendentTrait(TraitDef def)
        {
            regularTranses.Add(def);
        }
        public static void AddMythicTranscendentTrait(TraitDef def)
        {
            mythicTranses.Add(def);
        }
        public static void AddWokeGene(GeneDef def)
        {
            wokeGenes.Add(def);
        }
        private static readonly List<TraitDef> awakenings = new List<TraitDef>();
        private static readonly List<GeneDef> wokeGenes = new List<GeneDef>(); 
        private static readonly List<TraitDef> regularTranses = new List<TraitDef>();
        private static readonly List<TraitDef> mythicTranses = new List<TraitDef>();
    }
}
