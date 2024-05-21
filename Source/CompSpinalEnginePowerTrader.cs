using RimWorld;
using Verse;

namespace TheCafFiend
{
    [StaticConstructorOnStartup]
    public class CompSpinalEnginePowerTrader : CompPowerTrader
    {
        public override void SetUpPowerVars()
        {
            base.SetUpPowerVars();
            CompSpinalEngineTrail engineToModify = parent.TryGetComp<CompSpinalEngineTrail>();
            if (engineToModify == null)
            {
                Log.Message("SOS2 spinal engines: SpinalEnginePowerTrader didn't find an engine?");
                return;
            }
            if (engineToModify.fullyFormed == false) //No power tinkering yet!
            {
                return;
            }
            else
            {
                this.PowerOutput = engineToModify.powerUse;
            }
        }
    }
}