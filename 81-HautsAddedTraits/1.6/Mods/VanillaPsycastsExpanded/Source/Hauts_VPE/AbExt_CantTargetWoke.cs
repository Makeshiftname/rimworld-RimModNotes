using HautsTraitsRoyalty;
using RimWorld;
using VEF.Abilities;
using Verse;

namespace Hauts_VPE
{
    public class AbilityExtension_CantTargetWoke : AbilityExtension_AbilityMod
    {
        public override bool ValidateTarget(LocalTargetInfo target, VEF.Abilities.Ability ability, bool showMessages = true)
        {
            if (target.Thing != null && target.Thing is Pawn p && p.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(p))
            {
                if (showMessages)
                {
                    Messages.Message("CannotUseAbility".Translate(ability.def.label) + ": " + "HVT_CantTargetWoke".Translate(), p, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }
    }
}
