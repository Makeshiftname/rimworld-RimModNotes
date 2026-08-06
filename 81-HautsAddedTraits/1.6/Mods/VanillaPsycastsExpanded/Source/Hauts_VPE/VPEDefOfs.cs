using RimWorld;
using Verse;

namespace Hauts_VPE
{
    [DefOf]
    public static class HautsTraitsVPEDefOf
    {
        static HautsTraitsVPEDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HautsTraitsVPEDefOf));
        }
        public static HediffDef HVT_Dominicus;
        public static HediffDef HVT_TabulaRasaAcumen;
        public static HediffDef HVT_TabulaRasaTraitGiver;
        public static BackstoryDef HVT_TabulaRasaChild;
        public static BackstoryDef HVT_TabulaRasaAdult;
    }
}
