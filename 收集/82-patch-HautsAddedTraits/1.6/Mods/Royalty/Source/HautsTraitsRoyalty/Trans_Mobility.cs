using RimWorld.Planet;
using Verse;

namespace HautsTraitsRoyalty
{
    //You know the God Mode option to teleport a caravan to its destination? Arctic Terns do that, automatically, to any caravan they're on, whenever it attempts to move. They literally do that, as this is a copy of that code
    public class Hediff_PolarMigration : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsCaravanMember())
            {
                this.pawn.GetCaravan().Tile = this.pawn.GetCaravan().pather.Destination;
                this.pawn.GetCaravan().pather.StopDead();
            }
        }
    }
}
