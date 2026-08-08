using Verse;

namespace HautsTraitsRoyalty
{
    /*unless you have VPE, when a pawn gains a trait tagged with either of these DMEs, it will gain a psycast of the Word or Skip ability category.
     * This is intended to ensure that transcendences that specifically enhance the usage of Words or Skips in some way are not useless, because now the pawn has at least one such ability
     * Obviously, if a pawn already knows all the Word or Skip psycasts that they could have at their level, they won't gain a new one.*/
    public class GrantWordPsycast : DefModExtension
    {
        public GrantWordPsycast() { }
    }
    public class GrantSkipPsycast : DefModExtension
    {
        public GrantSkipPsycast() { }
    }
}
