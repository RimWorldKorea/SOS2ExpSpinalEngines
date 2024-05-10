using Verse;

namespace TheCafFiend
{
    public class CompProperties_SpinalEngineTrail : SaveOurShip2.CompProps_EngineTrail
    {
        int baseFuelUse = 0;
        public CompProperties_SpinalEngineTrail()
        {
            this.compClass = typeof(CompSpinalEngineTrail);
        }
    }
}