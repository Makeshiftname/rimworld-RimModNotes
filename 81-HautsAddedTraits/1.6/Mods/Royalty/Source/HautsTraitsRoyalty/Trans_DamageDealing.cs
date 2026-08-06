using HarmonyLib;
using HautsFramework;
using MVCF.Comps;
using MVCF.VerbComps;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VEF;
using Verse;
using Verse.Sound;

namespace HautsTraitsRoyalty
{
    //"psy blast" explosions (Fulmar, Hornet) can't damage same-Faction things unless they happen to be hostile to the blaster
    public class DamageWorker_AddInjuryPsyBlast : DamageWorker_AddInjury
    {
        protected override void ExplosionDamageThing(Explosion explosion, Thing t, List<Thing> damagedThings, List<Thing> ignoredThings, IntVec3 cell)
        {
            if (explosion.instigator != null && explosion.instigator.Faction != null && t.Faction != null && explosion.instigator.Faction == t.Faction)
            {
                if (t.HostileTo(explosion.instigator))
                {
                    base.ExplosionDamageThing(explosion, t, damagedThings, ignoredThings, cell);
                }
                return;
            }
            base.ExplosionDamageThing(explosion, t, damagedThings, ignoredThings, cell);
        }
    }
    //Bluejay's JoJo VFX uses a distorted version of something very similar to the effect that plays over corpses when you use a resurrector serum on them
    public class Graphic_StandPower : Graphic_Mote
    {
        protected override bool ForcePropertyBlock
        {
            get
            {
                return true;
            }
        }
        public override void Init(GraphicRequest req)
        {
            this.data = req.graphicData;
            this.path = req.path;
            this.maskPath = req.maskPath;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            this.request = req;
        }
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            Mote mote = (Mote)thing;
            Pawn pawn = mote.link1.Target.Thing as Pawn;
            Pawn pawn2 = ((mote.link1.Target.Thing is Corpse corpse) ? corpse.InnerPawn : null) ?? pawn;
            if (pawn2 == null)
            {
                pawn2 = this.lastPawn;
            }
            Color color = this.color;
            if (ModsConfig.IdeologyActive && pawn2.story != null && pawn2.story.favoriteColor != null)
            {
                color = pawn2.story.favoriteColor.color;
            }
            color.a *= mote.Alpha;
            PawnRenderer renderer = pawn2.Drawer.renderer;
            if ((renderer != null ? renderer.renderTree : null) == null || !renderer.renderTree.Resolved)
            {
                return;
            }
            Rot4 rot2 = (pawn2.GetPosture() == PawnPosture.Standing) ? pawn2.Rotation : renderer.LayingFacing();
            Vector3 vector = pawn2.DrawPos;
            Building_Bed building_Bed = pawn2.CurrentBed();
            if (building_Bed != null)
            {
                Rot4 rotation = building_Bed.Rotation;
                rotation.AsInt += 2;
                vector -= rotation.FacingCell.ToVector3() * (pawn2.story.bodyType.bedOffset + pawn2.Drawer.renderer.BaseHeadOffsetAt(Rot4.South).z);
            }
            bool posture = pawn2.GetPosture() != PawnPosture.Standing;
            vector.y = mote.def.Altitude;
            if (this.lastPawn != pawn2 || this.lastFacing != rot2)
            {
                this.bodyMaterial = this.MakeMatFrom(this.request, renderer.BodyGraphic.MatAt(rot2, null).mainTexture);
            }
            Mesh mesh;
            if (pawn2.RaceProps.Humanlike)
            {
                mesh = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn2).MeshAt(rot2);
            }
            else
            {
                mesh = renderer.BodyGraphic.MeshAt(rot2);
            }
            this.bodyMaterial.SetVector("_pawnCenterWorld", new Vector4(vector.x, vector.z, 0f, 0f));
            this.bodyMaterial.SetVector("_pawnDrawSizeWorld", new Vector4(mesh.bounds.size.x, mesh.bounds.size.z, 0f, 0f));
            this.bodyMaterial.SetFloat(ShaderPropertyIDs.AgeSecs, mote.AgeSecs);
            this.bodyMaterial.SetColor(ShaderPropertyIDs.Color, color);
            Quaternion quaternion = Quaternion.AngleAxis((!posture) ? 0f : renderer.BodyAngle(PawnRenderFlags.None), Vector3.up);
            if (building_Bed == null || building_Bed.def.building.bed_showSleeperBody)
            {
                GenDraw.DrawMeshNowOrLater(mesh, vector, quaternion, this.bodyMaterial, false);
            }
            if (pawn2.RaceProps.Humanlike)
            {
                if (this.lastPawn != pawn2 || this.lastFacing != rot2)
                {
                    this.headMaterial = this.MakeMatFrom(this.request, renderer.HeadGraphic.MatAt(rot2, null).mainTexture);
                }
                Vector3 b = quaternion * renderer.BaseHeadOffsetAt(rot2) + new Vector3(0f, 0.001f, 0f);
                Mesh mesh2 = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn2).MeshAt(rot2);
                this.headMaterial.SetVector("_pawnCenterWorld", new Vector4(vector.x, vector.z, 0f, 0f));
                this.headMaterial.SetVector("_pawnDrawSizeWorld", new Vector4(mesh2.bounds.size.x, mesh.bounds.size.z, 0f, 0f));
                this.headMaterial.SetFloat(ShaderPropertyIDs.AgeSecs, mote.AgeSecs);
                this.headMaterial.SetColor(ShaderPropertyIDs.Color, color);
                GenDraw.DrawMeshNowOrLater(mesh2, vector + b, quaternion, this.headMaterial, false);
            }
            if (pawn2 != null)
            {
                this.lastPawn = pawn2;
            }
            this.lastFacing = rot2;
        }
        private Material MakeMatFrom(GraphicRequest req, Texture mainTex)
        {
            return MaterialPool.MatFrom(new MaterialRequest
            {
                mainTex = mainTex,
                shader = req.shader,
                color = this.color,
                colorTwo = this.colorTwo,
                renderQueue = req.renderQueue,
                shaderParameters = req.shaderParameters
            });
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        private GraphicRequest request;
        private Pawn lastPawn;
        private Rot4 lastFacing;
        private Material bodyMaterial;
        private Material headMaterial;
    }
    //when the Bluejay MVCF turret fires, it grants the Bluejay the mote-having hediff (or just increases its duration). It also throws text, if specified (ORA in this case).
    public class VerbCompProperties_StandPower : VerbCompProperties
    {
        public HediffDef hediff;
        public string text;
    }
    public class VerbComp_StandPower : VerbComp
    {
        public VerbCompProperties_StandPower Props
        {
            get
            {
                return this.props as VerbCompProperties_StandPower;
            }
        }
        public override void Notify_ShotFired()
        {
            base.Notify_ShotFired();
            if (this.parent.Manager != null && this.parent.Manager.Pawn != null)
            {
                if (!this.parent.Manager.Pawn.health.hediffSet.HasHediff(this.Props.hediff))
                {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.hediff, this.parent.Manager.Pawn, null);
                    this.parent.Manager.Pawn.health.AddHediff(hediff);
                }
                else
                {
                    this.parent.Manager.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff).Severity = this.Props.hediff.maxSeverity;
                }
                if (this.Props.text != null)
                {
                    Vector3 loc = new Vector3((float)this.parent.Manager.Pawn.PositionHeld.x + 1f, (float)this.parent.Manager.Pawn.PositionHeld.y, (float)this.parent.Manager.Pawn.PositionHeld.z + 1f);
                    MoteMaker.ThrowText(loc, this.parent.Manager.Pawn.MapHeld, this.Props.text, Color.white, -1f);
                }
            }
        }
    }
    /*Psychic Diaboli generate an explosion of explosionDmg explosionType (fire) every explosionPeriodicity ticks if a hostile pawn is within explosionRadius cells. If also a Pyromaniac, this has a predictable effect on mood.
     * The "drench foes affected by your psycasts in chemfuel" effect is handled by InflictHediffOnHit*/
    public class HediffCompProperties_KaboomBaby : HediffCompProperties_InflictHediffOnHit
    {
        public HediffCompProperties_KaboomBaby()
        {
            this.compClass = typeof(HediffComp_KaboomBaby);
        }
        public float explosionRadius;
        public FloatRange explosionDmg;
        public DamageDef explosionType;
        public int explosionPeriodicity;
    }
    public class HediffComp_KaboomBaby : HediffComp_InflictHediffOnHit
    {
        public new HediffCompProperties_KaboomBaby Props
        {
            get
            {
                return (HediffCompProperties_KaboomBaby)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.explosionPeriodicity, delta) && this.Pawn.Spawned)
            {
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.Props.explosionRadius, true).OfType<Pawn>().Distinct<Pawn>())
                {
                    if (this.Pawn.HostileTo(p))
                    {
                        GenExplosion.DoExplosion(this.Pawn.Position, this.Pawn.Map, this.Props.explosionRadius, this.Props.explosionType, this.Pawn, (int)(this.Props.explosionDmg.RandomInRange), -1f);
                        if (this.Pawn.story != null && this.Pawn.story.traits.HasTrait(TraitDefOf.Pyromaniac))
                        {
                            Pawn_NeedsTracker pnt = this.Pawn.needs;
                            if (pnt != null && pnt.mood != null && pnt.mood.thoughts != null && pnt.mood.thoughts.memories != null)
                            {
                                pnt.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.PyroUsed, null, null);
                            }
                        }
                        break;
                    }
                }
            }
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (victim.Spawned)
            {
                foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(victim.Position, this.Props.explosionRadius, true))
                {
                    if (iv3.IsValid && GenSight.LineOfSight(victim.Position, iv3, victim.Map, true, null, 0, 0) && FilthMaker.TryMakeFilth(iv3, victim.Map, ThingDefOf.Filth_Fuel, 1, FilthSourceFlags.None, true))
                    {
                        continue;
                    }
                }
            }
        }
    }
    /*Hornets have an invisible, controllable, independently-firing MVCF turret. This makes its range scale with both psychic sensitivity and verb range factor
     * It consumes 0.5 neural heat with each shot, and cannot fire if the pawn has less than 1 neural heat remaining.
     */
    public class Hediff_HornetSting : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            HediffComp_ExtendedVerbGiver evg = this.TryGetComp<HediffComp_ExtendedVerbGiver>();
            if (evg != null)
            {
                this.baseRange = evg.VerbTracker.PrimaryVerb.verbProps.range;
            }
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            HediffComp_ExtendedVerbGiver evg = this.TryGetComp<HediffComp_ExtendedVerbGiver>();
            if (evg != null && this.pawn.IsHashIntervalTick(15))
            {
                Verb verb = evg.VerbTracker.PrimaryVerb;
                Type type = verb.verbProps.GetType();
                VerbProperties verbPropsCopy = Activator.CreateInstance(type) as VerbProperties;
                foreach (FieldInfo fieldInfo in type.GetFields())
                {
                    try
                    {
                        FieldInfo field = type.GetField(fieldInfo.Name);
                        field.SetValue(verbPropsCopy, fieldInfo.GetValue(verb.verbProps));
                    }
                    catch { }
                }
                Traverse traverse = Traverse.Create(verbPropsCopy).Field("range");
                traverse.SetValue(Math.Max(this.baseRange, this.baseRange * this.pawn.GetStatValue(StatDefOf.PsychicSensitivity) * this.pawn.GetStatValue(VEFDefOf.VEF_VerbRangeFactor)));
                verb.verbProps = verbPropsCopy;
            }
        }
        private float baseRange = 0;
    }
    public class Verb_Shoot_NeuralHeatAmmo : Verb_Shoot
    {
        public override bool Available()
        {
            if (this.CasterIsPawn && this.CasterPawn.psychicEntropy.EntropyValue < 1f)
            {
                return false;
            }
            return base.Available();
        }
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (this.CasterIsPawn)
            {
                this.CasterPawn.psychicEntropy.TryAddEntropy(-0.5f);
            }
        }
        /*public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caster.Position, this.verbProps.range * this.CasterPawn.GetStatValue(StatDefOf.PsychicSensitivity), Color.white);
        }*/
    }
    /*Toxbuzzards have a DamageRetaliation comp. The only reason we need a derivative is to make the range in which it can fuck over whoever hit the Toxbuzzard scale with psylink level.
     * Even if the pawn has no psylinks (which would be weird, but it isn't impossible) the aura can't drop below its minimum range.*/
    public class HediffCompProperties_WithATasteOfYourLips : HediffCompProperties_DamageRetaliation
    {
        public HediffCompProperties_WithATasteOfYourLips()
        {
            this.compClass = typeof(HediffComp_WithATasteOfYourLips);
        }
    }
    public class HediffComp_WithATasteOfYourLips : HediffComp_DamageRetaliation
    {
        public new HediffCompProperties_WithATasteOfYourLips Props
        {
            get
            {
                return (HediffCompProperties_WithATasteOfYourLips)this.props;
            }
        }
        public override float RetaliationRange => base.RetaliationRange * Math.Max(1f, this.Pawn.GetPsylinkLevel());
    }
    /* |||||ZIZ ZONE|||||
     * akin to the energy beams fired by Orbital Power Targeters, but they deal bomb damage instead. Pawns with the Ziz trait don't take damage from these beams, but instead gain a potent buff*/
    public class PulverizationBeam : OrbitalStrike
    {
        public override void StartStrike()
        {
            base.StartStrike();
            MoteMaker.MakePowerBeamMote(base.Position, base.Map);
        }
        protected override void Tick()
        {
            base.Tick();
            if (base.Destroyed)
            {
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                this.Pulverize();
            }
        }
        private void Pulverize()
        {
            IntVec3 c = (from x in GenRadial.RadialCellsAround(base.Position, 15f, true)
                         where x.InBounds(base.Map)
                         select x).RandomElementByWeight((IntVec3 x) => 1f - Mathf.Min(x.DistanceTo(base.Position) / 15f, 1f) + 0.05f);
            PulverizationBeam.tmpThings.Clear();
            PulverizationBeam.tmpThings.AddRange(c.GetThingList(base.Map));
            for (int i = 0; i < PulverizationBeam.tmpThings.Count; i++)
            {
                Pawn pawn = PulverizationBeam.tmpThings[i] as Pawn;
                if (pawn != null && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitZiz))
                {
                    pawn.health.hediffSet.TryGetHediff(HVTRoyaltyDefOf.HVT_ZizBuff, out Hediff zizBuff);
                    if (zizBuff != null)
                    {
                        zizBuff.Severity = zizBuff.def.maxSeverity;
                    }
                    else
                    {
                        pawn.health.AddHediff(HVTRoyaltyDefOf.HVT_ZizBuff);
                    }
                }
                else
                {
                    BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = null;
                    if (pawn != null)
                    {
                        battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_PowerBeam, this.instigator as Pawn);
                        Find.BattleLog.Add(battleLogEntry_DamageTaken);
                    }
                    PulverizationBeam.tmpThings[i].TakeDamage(new DamageInfo(DamageDefOf.Bomb, DamageDefOf.Bomb.defaultDamage * PulverizationBeam.DamageAmountRange.RandomInRange, DamageDefOf.Bomb.defaultArmorPenetration * 2f, -1f, this.instigator, null, this.weaponDef, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true)).AssociateWithLog(battleLogEntry_DamageTaken);
                }
            }
            PulverizationBeam.tmpThings.Clear();
        }
        public const float Radius = 17f;
        private static readonly FloatRange DamageAmountRange = new FloatRange(0.9f, 1.8f);
        private static List<Thing> tmpThings = new List<Thing>();
    }
    //Ziz have the ability to call down multiple pulverization beams on the target point. The number of beams scales with their psysens.
    public class CompProperties_PulverizationBeam : CompProperties_EffectWithDest
    {
        public int durationTicks;
    }
    public class CompAbilityEffect_PulverizationBeam : CompAbilityEffect_WithDest
    {
        public new CompProperties_PulverizationBeam Props
        {
            get
            {
                return (CompProperties_PulverizationBeam)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (target != null && target.Cell.IsValid)
            {
                int maxBeams = (int)Math.Max(1f, this.parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity));
                for (int i = 0; i < maxBeams; i++)
                {
                    PulverizationBeam powerBeam = (PulverizationBeam)GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_PulverizationBeam, target.Cell, this.parent.pawn.Map, WipeMode.Vanish);
                    powerBeam.duration = this.Props.durationTicks;
                    powerBeam.instigator = this.parent.pawn;
                    powerBeam.StartStrike();
                }
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return this.Caster.MapHeld != null && (this.Caster.PositionHeld.DistanceTo(target.Cell) > 15f || Rand.Value <= 0.5f) && this.Caster.MapHeld.listerThings.ThingsOfDef(HVTRoyaltyDefOf.HVT_PulverizationBeam).Count == 0;
        }
    }
    /*aiCanUse is true for Pulverization Beam, so on the off chance you face an NPC Ziz, they can attack you with it.
     * However, aiCanUse abilities are not targeted all that intelligently (the 'a' in 'ai' probably stands for 'aspirationally'), so here is a built-in functionality that infrequently allows the Ziz to open fire
     * on a target evaluated somewhat more robustly than just "am I attacking a hostile thing this ability could hit?"
     * pastThisPercentDmgLoosenRestrictions: if the Ziz becomes so injured that it's at least this percent of its way to meeting its Lethal Damage Threshold, it will consider ANY hostile to be worth pulverizing.
     *   It's not a bad strategy, since the more they cast the beam, the likelier they are to be hit by it and thus become invulnerable. Also, y'know, doing damage is a great way of mitigating further damage.
     * netMarketValueToFire: otherwise, putting a beam on the target would have to put enough hostile things in its radius that the [sum market value of hostile things in radius] - [sum market value of Faction-having
     *   non-hostile things in radius * friendlyFireConsiderationFactor] exceeds this threshold.*/
    public class CompProperties_AbilityTargetForMassDestruction : CompProperties_AbilityAiScansForTargets
    {
        public float netMarketValueToFire;
        public float pastThisPercentDmgLoosenRestrictions;
        public float friendlyFireConsiderationFactor;
    }
    public class CompAbilityEffect_TargetForMassDestruction : CompAbilityEffect_AiScansForTargets
    {
        public new CompProperties_AbilityTargetForMassDestruction Props
        {
            get
            {
                return (CompProperties_AbilityTargetForMassDestruction)this.props;
            }
        }
        public override bool AdditionalQualifiers(Thing thing)
        {
            if (HautsMiscUtility.MissingHitPointPercentageFor(this.parent.pawn) >= this.Props.pastThisPercentDmgLoosenRestrictions)
            {
                return true;
            }
            float marketValue = thing.MarketValue;
            if ((thing is Pawn || (thing.def.building != null && (thing.def.building.IsTurret || thing.def.building.isTrap))) && GenSight.LineOfSight(this.parent.pawn.Position, thing.Position, thing.Map))
            {
                foreach (Thing t in GenRadial.RadialDistinctThingsAround(thing.Position, thing.Map, this.parent.def.EffectRadius, true))
                {
                    if (this.parent.pawn.HostileTo(t))
                    {
                        marketValue += t.MarketValue;
                    }
                    else if (t.Faction != null)
                    {
                        marketValue -= t.MarketValue * this.Props.friendlyFireConsiderationFactor;
                    }
                }
            }
            return marketValue > this.Props.netMarketValueToFire;
        }
    }
    /*Ziz ALSO have the ability to expend all their charges of Pulverization Beam to instead destroy a target object on the world map. This then causes a Volcanic Winter and makes everyone unhappy with you, but it is sometimes very useful.
     * It is also a good way to fail, or possibly brick, some quests. Target wisely.
     * tilesRadius: in case you want to make it hit other objects near the targeted object, you could do that. For some reason.*/
    public class CompProperties_MassInversion : CompProperties_AbilityEffect
    {
        public CompProperties_MassInversion()
        {
            this.compClass = typeof(CompAbilityEffect_MassInversion);
        }
        public int tilesRadius = 0;
    }
    public class CompAbilityEffect_MassInversion : CompAbilityEffect
    {
        public new CompProperties_MassInversion Props
        {
            get
            {
                return (CompProperties_MassInversion)this.props;
            }
        }
        public override bool Valid(GlobalTargetInfo target, bool throwMessages = false)
        {
            GenDraw.DrawWorldRadiusRing(this.parent.pawn.Tile, 30);
            return base.Valid(target, throwMessages);
        }
        public override bool CanApplyOn(GlobalTargetInfo target)
        {
            if (Find.WorldGrid.TraversalDistanceBetween(this.parent.pawn.Tile, target.Tile, true, int.MaxValue) > 30)
            {
                Messages.Message("HVT_MassInversionOutOfRange".Translate(), null, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (Find.World.worldObjects.WorldObjectAt<WorldObject>(target.Tile) == null)
            {
                Messages.Message("HVT_MassInversionNoTarget".Translate(), null, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return (base.CanApplyOn(target));
        }
        public override bool CanCast
        {
            get
            {

                if (this.parent.pawn.abilities.GetAbility(HVTRoyaltyDefOf.HVT_ZizAbility) != null)
                {
                    Ability oa = this.parent.pawn.abilities.GetAbility(HVTRoyaltyDefOf.HVT_ZizAbility);
                    if (oa.CanCast)
                    {
                        return (int)typeof(Ability).GetField("charges", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(oa) >= oa.def.charges;
                    }
                }
                return false;
            }
        }
        public override void Apply(GlobalTargetInfo target)
        {
            base.Apply(target);
            WorldObject worldObject = Find.World.worldObjects.WorldObjectAt<WorldObject>(target.Tile);
            if (worldObject != null)
            {
                foreach (WorldObject w in Find.World.worldObjects.AllWorldObjects)
                {
                    if (Find.WorldGrid.TraversalDistanceBetween(w.Tile, target.Tile, true, int.MaxValue) <= this.Props.tilesRadius)
                    {
                        MapParent mp = w as MapParent;
                        if (mp != null && mp.HasMap)
                        {
                            IncidentParms parms = new IncidentParms
                            {
                                target = mp.Map
                            };
                            DefDatabase<IncidentDef>.GetNamedSilentFail("VolcanicWinter").Worker.TryExecute(parms);
                            mp.Map.weatherManager.TransitionTo(DefDatabase<WeatherDef>.GetNamed("RainyThunderstorm"));
                        }
                    }
                }
                SoundDefOf.Thunder_OnMap.PlayOneShot(this.parent.pawn);
                WeatherEvent_LightningStrike lightningflash = new WeatherEvent_LightningStrike(this.parent.pawn.Map);
                lightningflash.WeatherEventDraw();
                MapParent mapParent = worldObject as MapParent;
                if (mapParent != null && mapParent.HasMap)
                {
                    foreach (Thing thing in mapParent.Map.spawnedThings)
                    {
                        thing.Destroy(DestroyMode.KillFinalize);
                    }
                }
                string how = ModCompatibilityUtility.IsHighFantasy() ? "HVT_PsychicWMD1Fantasy".Translate(worldObject.Label) : "HVT_PsychicWMD1".Translate(worldObject.Label);
                ChoiceLetter notification = LetterMaker.MakeLetter(
                how, "HVT_PsychicWMD2".Translate(worldObject.Label, this.parent.pawn.Name.ToStringShort, this.parent.pawn.gender.GetObjective()).CapitalizeFirst(), LetterDefOf.Death, null, null, null, null);
                Find.LetterStack.ReceiveLetter(notification, null);
                foreach (Faction faction in Find.FactionManager.AllFactions)
                {
                    if (!faction.IsPlayer && !faction.defeated)
                    {
                        faction.TryAffectGoodwillWith(this.parent.pawn.Faction, -200, true, true, null, null);
                    }
                }
                worldObject.Destroy();
                Find.World.grid.StandardizeTileData();
                if (this.parent.pawn.abilities.GetAbility(HVTRoyaltyDefOf.HVT_ZizAbility) != null)
                {
                    Ability zizBeam = this.parent.pawn.abilities.GetAbility(HVTRoyaltyDefOf.HVT_ZizAbility);
                    typeof(Ability).GetField("charges", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(zizBeam, 0);
                    zizBeam.StartCooldown(zizBeam.def.cooldownTicksRange.RandomInRange);
                    HautsFramework.HautsFramework.HautsActivatePostfix(zizBeam);
                }
            }
        }
    }
}
