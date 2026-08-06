using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HautsTraits
{
    /*the IEDs created by a Sabotage raid are not conventional IEDs. These take a long while to go off (or much faster when touched), and they can't be uninstalled or deconstructed. You gotta defuse them,
     * which is a task that goes faster with higher Construction speed, and it has a success chance = 5% per Construction skill level. Success causes it to be destroyed in a HARMLESS smoke explosion.
     * Failure causes a second roll, and if that one succeeds, the wick stops. If both rolls fail, well, try try again! Or just run away.
     * The SabotageExplosive DME does two things (both handled in Harmony): makes triggering the trap by stepping on it shorten the wick to its normal timer length (i.e. really short),
     * and makes the wick being lit not cause anyone other than player pawns to freak out. That's necessary to prevent the saboteurs from just running around like headless chickens, which is a behavior
     * that makes sense when raiders have just triggered one of YOUR traps and are about to meet the archotechs, but is markedly less sensible when they were ludonarratively the people who deployed the
     * explosives and purposely lit the wicks.*/
    public class SabotageExplosive : DefModExtension
    {
        public SabotageExplosive()
        {

        }
    }
    public class CompProperties_SabExp : CompProperties_Explosive
    {
        public CompProperties_SabExp()
        {
            this.compClass = typeof(CompSabExp);
        }
    }
    [StaticConstructorOnStartup]
    public class CompSabExp : CompExplosive
    {
        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.Faction == Faction.OfPlayerSilentFail && this.wickTicksLeft > this.Props.wickTicks.max)
            {
                this.StopWick();
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = "HVT_DisarmGizmo".Translate(),
                defaultDesc = "HVT_DisarmDesc".Translate(),
                icon = CompSabExp.DisarmTexture,
                action = delegate
                {
                    Find.Targeter.BeginTargeting(TargetingParameters.ForColonist(), delegate (LocalTargetInfo target)
                    {
                        Pawn pawn = target.Pawn;
                        if (pawn == null)
                        {
                            return;
                        }
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(HVTDefOf.HVT_DisarmExplosive, this.parent), new JobTag?(JobTag.Misc), false);
                    }, delegate (LocalTargetInfo target)
                    {
                        Pawn pawn2 = target.Pawn;
                        if (pawn2 != null && pawn2.IsColonistPlayerControlled)
                        {
                            GenDraw.DrawTargetHighlight(target);
                        }
                    }, (LocalTargetInfo target) => this.ValidateTechie(target).Accepted, null, null, CompSabExp.DisarmTexture, true, delegate (LocalTargetInfo target)
                    {
                        AcceptanceReport acceptanceReport2 = this.ValidateTechie(target);
                        Pawn pawn3 = target.Pawn;
                        if (pawn3 != null && pawn3.IsColonistPlayerControlled && !acceptanceReport2.Accepted)
                        {
                            Widgets.MouseAttachedLabel(("HVT_CannotChooseDisarmer".Translate() + ": " + acceptanceReport2.Reason.CapitalizeFirst()).Colorize(ColorLibrary.RedReadable), 0f, 0f, null);
                            return;
                        }
                        Widgets.MouseAttachedLabel("HVT_CommandChooseDisarmer".Translate(), 0f, 0f, null);
                    }, null);
                }
            };
            yield return command_Action;
            yield break;
        }
        private AcceptanceReport ValidateTechie(LocalTargetInfo target)
        {
            Pawn pawn = target.Thing as Pawn;
            if (pawn == null)
            {
                return false;
            }
            if (!pawn.CanReach(this.parent, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                return "NoPath".Translate();
            }
            if (pawn.IsSubhuman || pawn.WorkTagIsDisabled(WorkTags.Constructing) || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return "IncapableOfDeconstruction".Translate();
            }
            return true;
        }
        private static readonly Texture2D DisarmTexture = ContentFinder<Texture2D>.Get("UI/Commands/Hack", true);
    }
    public class JobDriver_DisarmExplosive : JobDriver
    {
        protected Thing Target
        {
            get
            {
                return this.job.targetA.Thing;
            }
        }
        protected Building Building
        {
            get
            {
                return (Building)this.Target.GetInnerIfMinified();
            }
        }
        protected DesignationDef Designation
        {
            get
            {
                return DesignationDefOf.Deconstruct;
            }
        }
        protected float TotalNeededWork
        {
            get
            {
                return Mathf.Clamp(this.Building.GetStatValue(StatDefOf.WorkToBuild, true, -1), 600f, 3000f);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft", 0f, false);
            Scribe_Values.Look<float>(ref this.totalNeededWork, "totalNeededWork", 0f, false);
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Target, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (this.Designation != null)
            {
                this.FailOnThingMissingDesignation(TargetIndex.A, this.Designation);
            }
            this.FailOnForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false);
            Toil doWork = ToilMaker.MakeToil("MakeNewToils").FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            doWork.initAction = delegate
            {
                doWork.handlingFacing = true;
                doWork.tickAction = delegate
                {
                    doWork.actor.rotationTracker.FaceTarget(this.job.GetTarget(TargetIndex.A));
                };
                this.totalNeededWork = this.TotalNeededWork;
                this.workLeft = this.totalNeededWork;
            };
            doWork.tickIntervalAction = delegate (int delta)
            {
                this.workLeft -= this.pawn.GetStatValue(StatDefOf.ConstructionSpeed, true, -1) * 1.7f * (float)delta;
                this.TickActionInterval(delta);
                if (this.workLeft <= 0f)
                {
                    doWork.actor.jobs.curDriver.ReadyForNextToil();
                }
            };
            doWork.defaultCompleteMode = ToilCompleteMode.Never;
            doWork.WithProgressBar(TargetIndex.A, () => 1f - this.workLeft / this.totalNeededWork, false, -0.5f, false);
            doWork.activeSkill = () => SkillDefOf.Construction;
            yield return doWork;
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                this.FinishedRemoving();
                base.Map.designationManager.RemoveAllDesignationsOn(this.Target, false);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            yield break;
        }
        protected virtual void FinishedRemoving()
        {
            float chance = 0.1f;
            if (this.pawn.skills != null)
            {
                chance = this.pawn.skills.GetSkill(SkillDefOf.Construction).Level * 0.05f;
            }
            else if (this.pawn.IsColonyMech)
            {
                chance = this.pawn.RaceProps.mechFixedSkillLevel * 0.05f;
            }
            if (Rand.Chance(chance))
            {
                if (this.Target.Faction != null)
                {
                    GenExplosion.DoExplosion(this.Building.Position, this.Building.Map, 2f, DamageDefOf.Smoke, null, -1, -1f, null, null, null, null, null, 0f, 1, new GasType?(GasType.BlindSmoke), null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f, null, null, null, null);
                    this.Target.Faction.Notify_BuildingRemoved(this.Building, this.pawn);
                    this.Target.Destroy(DestroyMode.Deconstruct);
                    this.pawn.records.Increment(RecordDefOf.ThingsDeconstructed);
                }
            }
            else if (Rand.Chance(chance))
            {
                CompSabExp ce = this.Target.TryGetComp<CompSabExp>();
                if (ce != null)
                {
                    ce.StopWick();
                }
            }
        }
        protected void TickActionInterval(int delta)
        {
            if (this.pawn.skills != null && this.Building.def.CostListAdjusted(this.Building.Stuff, true).Count > 0)
            {
                this.pawn.skills.Learn(SkillDefOf.Construction, 0.25f * (float)delta, false, false);
            }
        }
        private float workLeft;
        private float totalNeededWork;
    }
}
