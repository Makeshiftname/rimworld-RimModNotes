using HautsFramework;
using Verse;

namespace HautsTraits
{
    //Mighty and Herculean won't change body types for any pawn if the relevant mod setting isn't enabled
    public class ForcedBodyTypeWorker_StrengthTraits : ForcedBodyTypeWorker
    {
        public override bool CanChangeBodyType(Pawn pawn, TraitGrantedStuff tgs)
        {
            return HVT_Mod.settings.strengthTraitsChangeBodyType;
        }
    }
}
