using SaveOurShip2;
using Verse;

namespace TheCafFiend
{
    [StaticConstructorOnStartup]
    public class CompSpinalEngineMount : CompSpinalMount
    {
        public new CompProperties_SpinalEngineMount Props //My classism is weak, TODO: Check if this is nasty/busted
        {
            get
            {
                return (CompProperties_SpinalEngineMount)props;
            }
        }
    }
}
