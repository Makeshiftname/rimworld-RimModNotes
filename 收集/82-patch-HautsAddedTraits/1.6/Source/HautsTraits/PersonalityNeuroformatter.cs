using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HautsTraits
{
    /*the Personality Neuroformatter is an uncraftable serum that lets you add a trait of your choice to a pawn. Since not all traits are equal (p much regardless of your personal values as a RW player)
     * not all of them should have the same 'cost'. Thus, PNFs have multiple charges, and some traits cost more charges than others.
     * PNFs cannot grant traits that cannot 'naturally spawn' (i.e. that have a commonality of 0 for the target pawn's gender) or are labeled ExciseTraitExempt.
     * Since there are probably at least thousands of traits out there across all the mods, trait cost is automatically assigned based on commonality: 1 or higher is 1 charge, and every 0.1 lower is +1 charge.
     * However, if you want a specific trait to have a specific cost, you use the SpecificPNFChargeCost DME. I have already used this for all the non-modded traits as well as for all of the HAT traits.*/
    public class SpecificPNFChargeCost : DefModExtension
    {
        public SpecificPNFChargeCost()
        {

        }
        public Dictionary<int, int> chargeCosts;
    }
    public class CompProperties_PNF : CompProperties_ItemCharged
    {
        public CompProperties_PNF()
        {
            this.compClass = typeof(Comp_PNF);
        }
    }
    public class Comp_PNF : Comp_ItemCharged
    {
        public new CompProperties_PNF Props
        {
            get
            {
                return this.props as CompProperties_PNF;
            }
        }
        public override float CostFactor => HVT_Mod.settings.pnfCostFactor;
        public override bool AllowStackWith(Thing other)
        {
            return false;
        }
        public override int InitialCharges()
        {
            return Math.Min(base.InitialCharges(), (int)HVT_Mod.settings.pnfStartingCharges);
        }
    }
    public class JobDriver_UseTraitGiverSerum : JobDriver
    {
        private Pawn Pawn
        {
            get
            {
                return (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
            }
        }
        private Thing Item
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(25, TargetIndex.None);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickAction = delegate ()
            {
                CompUsable compUsable = this.Item.TryGetComp<CompUsable>();
                if (compUsable != null && this.warmupMote == null && compUsable.Props.warmupMote != null)
                {
                    this.warmupMote = MoteMaker.MakeAttachedOverlay(this.Pawn, compUsable.Props.warmupMote, Vector3.zero, 1f, -1f);
                }
                Mote mote = this.warmupMote;
                if (mote == null)
                {
                    return;
                }
                mote.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(new Action(this.DoTraitWindow));
            yield break;
        }
        private void DoTraitWindow()
        {
            Comp_ItemCharged chargeSource = this.Item.TryGetComp<Comp_ItemCharged>();
            if (chargeSource != null)
            {
                TraitSerumWindow window = new TraitSerumWindow(this.Pawn, chargeSource);
                Find.WindowStack.Add(window);
            }
        }
        private Mote warmupMote;
    }
    public class CompProperties_TargetEffectGiveTrait : CompProperties
    {
        public CompProperties_TargetEffectGiveTrait()
        {
            this.compClass = typeof(CompTargetEffect_GiveTrait);
        }
    }
    public class CompTargetEffect_GiveTrait : CompTargetEffect
    {
        public CompProperties_TargetEffectGiveTrait Props
        {
            get
            {
                return (CompProperties_TargetEffectGiveTrait)this.props;
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            if (!user.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly, 1, -1, null, false))
            {
                return;
            }
            Pawn pawn = target as Pawn;
            if (pawn != null)
            {
                if (pawn.story == null || pawn.story.traits == null)
                {
                    return;
                }
                Job job = JobMaker.MakeJob(HVTDefOf.HVT_UseTraitGiverSerum, pawn, this.parent);
                job.count = 1;
                user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            }
        }
    }
    /*data structure used by the TraitSerumWindow, bundling a specific trait with its charge cost. Being able to store all this data in one structure is great
     * because we just create a full list of all the GrantableTraits once per window opening, during its initialization*/
    public class GrantableTrait
    {
        public GrantableTrait(TraitDef traitDef, int degree, Pawn pawn)
        {
            this.traitDef = traitDef;
            this.degree = degree;
            TraitDegreeData tdd = traitDef.DataAtDegree(degree);
            this.chargeCost = this.GetChargeCost(traitDef, degree, pawn.gender);
            this.displayText = tdd.LabelCap + "(" + this.chargeCost + ")";
            this.tooltip = tdd.description.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
        }
        public int GetChargeCost(TraitDef t, int degree, Gender gender)
        {
            SpecificPNFChargeCost spnfcc = t.GetModExtension<SpecificPNFChargeCost>();
            if (spnfcc != null && spnfcc.chargeCosts.ContainsKey(degree))
            {
                return spnfcc.chargeCosts.TryGetValue(degree);
            }
            return (int)Math.Min(Math.Max(10 * (1.1 - (t.GetGenderSpecificCommonality(gender) / t.degreeDatas.Count)), 1), 10);
        }
        public TraitDef traitDef;
        public int degree;
        public int chargeCost;
        public string displayText;
        public string tooltip;
    }
    /*if a trait has this DME, only one of its degrees (randomly chosen) will be offered as an option in the TraitSerumWindow.
     * This is pretty much just for Latent Psychic. If a pawn has ANY PNF Scrambler trait, no trait with this DME will show up in the TraitSerumWindow.*/
    public class PersoneuroformatterScrambler : DefModExtension
    {
        public PersoneuroformatterScrambler()
        {

        }
    }
    public class TraitSerumWindow : Window
    {
        public override void PreOpen()
        {
            base.PreOpen();
            this.grantableTraits.Clear();
            if (this.pawn.story == null)
            {
                return;
            }
            foreach (TraitDef t in DefDatabase<TraitDef>.AllDefsListForReading)
            {
                if (t.GetGenderSpecificCommonality(this.pawn.gender) > 0f && !this.pawn.WorkTagIsDisabled(t.requiredWorkTags))
                {
                    if (t.HasModExtension<PersoneuroformatterScrambler>())
                    {
                        if (this.pawn.story.traits.allTraits.ContainsAny((Trait trait) => trait.def.HasModExtension<PersoneuroformatterScrambler>()))
                        {
                            continue;
                        }
                        if (this.pawn.story.traits.GetTrait(t) == null)
                        {
                            GrantableTrait newGT = new GrantableTrait(t, t.degreeDatas.RandomElement().degree, this.pawn);
                            if (newGT.chargeCost <= this.remainingCharges)
                            {
                                grantableTraits.Add(newGT);
                            }
                        }
                    }
                    else
                    {
                        foreach (TraitDegreeData td in t.degreeDatas)
                        {
                            if (this.pawn.story.traits.GetTrait(t, td.degree) == null)
                            {
                                GrantableTrait newGT = new GrantableTrait(t, td.degree, this.pawn);
                                if (newGT.chargeCost <= this.remainingCharges)
                                {
                                    grantableTraits.Add(newGT);
                                }
                            }
                        }
                    }
                }
            }
            this.grantableTraits.SortBy((GrantableTrait g) => g.chargeCost, (GrantableTrait g) => g.displayText);
        }
        public int GetChargeCost(TraitDef t, int degree)
        {
            SpecificPNFChargeCost spnfcc = t.GetModExtension<SpecificPNFChargeCost>();
            if (spnfcc != null && spnfcc.chargeCosts.ContainsKey(degree))
            {
                return spnfcc.chargeCosts.TryGetValue(degree);
            }
            return (int)Math.Min(Math.Max(10 * (1.1 - (t.GetGenderSpecificCommonality(this.pawn.gender) / t.degreeDatas.Count)), 1), 10);
        }
        public TraitSerumWindow(Pawn pawn, Comp_ItemCharged chargeSource)
        {
            this.pawn = pawn;
            this.forcePause = true;
            this.remainingCharges = chargeSource.RemainingCharges;
            this.chargeSource = chargeSource;
        }
        private float Height
        {
            get
            {
                return CharacterCardUtility.PawnCardSize(this.pawn).y + Window.CloseButSize.y + 4f + this.Margin * 2f;
            }
        }
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, this.Height);
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMax -= 4f + Window.CloseButSize.y;
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width * 0.7f, this.scrollHeight);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect, true);
            float num = 0f;
            Widgets.Label(0f, ref num, viewRect.width, "HVT_TraitSerumLabel".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), default(TipSignal));
            num += 14f;
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect rect = new Rect(0f, num, inRect.width - 30f, 99999f);
            listing_Standard.Begin(rect);
            foreach (GrantableTrait gt in this.grantableTraits)
            {
                bool flag = this.chosenTrait == gt.traitDef && this.chosenTraitDegree == gt.degree;
                bool flag2 = flag;
                listing_Standard.CheckboxLabeled(gt.displayText, ref flag, gt.tooltip);
                if (flag != flag2)
                {
                    if (flag)
                    {
                        this.chosenTrait = gt.traitDef;
                        this.chosenTraitDegree = gt.degree;
                    }
                }
            }
            listing_Standard.End();
            num += listing_Standard.CurHeight + 10f + 4f;
            if (Event.current.type == EventType.Layout)
            {
                this.scrollHeight = Mathf.Max(num, inRect.height);
            }
            Widgets.EndScrollView();
            Rect rect2 = new Rect(0f, inRect.yMax + 4f, inRect.width, Window.CloseButSize.y);
            AcceptanceReport acceptanceReport = this.CanClose();
            if (!acceptanceReport.Accepted)
            {
                TextAnchor anchor = Text.Anchor;
                GameFont font = Text.Font;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                Rect rect3 = rect;
                rect3.xMax = rect2.xMin - 4f;
                Widgets.Label(rect3, acceptanceReport.Reason.Colorize(ColoredText.WarningColor));
                Text.Font = font;
                Text.Anchor = anchor;
            }
            if (Widgets.ButtonText(rect2, "OK".Translate(), true, true, true, null))
            {
                if (acceptanceReport.Accepted)
                {
                    Trait trait = new Trait(this.chosenTrait, this.chosenTraitDegree);
                    this.pawn.story.traits.GainTrait(trait, true);
                    for (int i = 0; i < this.GetChargeCost(trait.def, trait.Degree); i++)
                    {
                        this.chargeSource.UsedOnce();
                    }
                    if (trait.def.DataAtDegree(trait.Degree).skillGains != null)
                    {
                        for (int i = 0; i < trait.def.DataAtDegree(trait.Degree).skillGains.Count; i++)
                        {
                            SkillDef toBoost = trait.def.DataAtDegree(trait.Degree).skillGains[i].skill;
                            this.pawn.skills.GetSkill(toBoost).Level += trait.def.DataAtDegree(trait.Degree).skillGains[i].amount;
                        }
                    }
                    this.Close(true);
                    SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(this.pawn, MaintenanceType.None));
                    Messages.Message("HVT_TraitSerumSuccess".Translate(this.chosenTrait.DataAtDegree(chosenTraitDegree).GetLabelFor(this.pawn).CapitalizeFirst(), this.pawn.Named("PAWN")), this.pawn, MessageTypeDefOf.PositiveEvent, true);
                }
                else
                {
                    Messages.Message(acceptanceReport.Reason, null, MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        private AcceptanceReport CanClose()
        {
            if (this.chosenTrait == null)
            {
                return "HVT_TraitSerumNeedsChoice".Translate();
            }
            else if (isBadTraitCombo(this.chosenTrait, this.pawn))
            {
                return "HVT_TraitSerumLPWokeException".Translate();
            }
            return AcceptanceReport.WasAccepted;
        }
        public bool isBadTraitCombo(TraitDef t, Pawn pawn)
        {
            return false;
        }
        private Pawn pawn;
        private int remainingCharges;
        private TraitDef chosenTrait = null;
        private int chosenTraitDegree = 0;
        private float scrollHeight;
        private Comp_ItemCharged chargeSource;
        private Vector2 scrollPosition;
        private List<GrantableTrait> grantableTraits = new List<GrantableTrait>();
    }
}
