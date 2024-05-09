using SaveOurShip2;
using Verse;


namespace TheCafFiend
{
    public class CompSpinalEngineTrail : CompEngineTrail
    {
        public bool fullyFormed = false;
        private int thrustCached = 0;
        private int cachedMass = 0; // add/remove on the *engine* does this itself, this is only amps+caps
        private CompProps_EngineTrail internalProps;
        public override int Thrust
        {
            get
            {
                if (parent.TryGetComp<CompSpinalEngineTrail>() == null) //should never? happen but reflex
                {
                    Log.Message("SOS2 spinal engines: somehow hit thrust from a non-compspinalenginetrail?");
                    return base.Thrust;
                }
                if (fullyFormed)
                {
                    return thrustCached;
                }
                else
                {
                    //Log.Message($"sos2 spinal engines thrust called about a spinal engine, not fully formed");
                    calc();
                    return thrustCached;
                }
            }
        }
        public override CompProps_EngineTrail Props
        {
            get 
            {
                if (parent.TryGetComp<CompSpinalEngineTrail>() == null) //should never? happen but reflex
                {
                    Log.Message("SOS2 spinal engines: somehow hit props from a non-compspinalenginetrail?");
                    return base.Props;
                }
                if (fullyFormed)
                {
                    //Log.Message("sos2se propsgetterfullyFormed");
                    return internalProps;
                }
                else
                {
                    //Log.Message("sos2se propsgetter - calc");
                    calc();
                    return internalProps;
                }
            }
        }
        public int supportWeight
        {
            get
            {
                if (parent.TryGetComp<CompSpinalEngineTrail>() == null) //should never? happen but reflex
                {
                    Log.Message("SOS2 spinal engines: somehow hit supportWeight from a non-compspinalenginetrail?!!!!??");
                    return 0;
                }
                if (fullyFormed)
                {
                    return cachedMass;
                }
                else
                {
                    calc();
                    return cachedMass;
                }
            }
        }

        private void calc()
        {
            float tempAmpBonus = SOS2ExpSpinalEngines.SpinalRecalc(this);
            if (tempAmpBonus == 0) // 0 basically means something went wrong: Multiblock broken, re-set to zeros
            {
                thrustCached = 0;
                cachedMass = 0;
                internalProps = base.Props;
                internalProps.fuelUse = 0;
                return; 

            }
            else
            {
                //Log.Message("sos2spinal engines base.Thrust from calc");
                thrustCached = (int)(base.Props.thrust * (1 + tempAmpBonus)); // Do *not* use .Thrust or the base will end up doing recursion into overidden props
                //Log.Message("sos2spinal engines base.Props from calc");
                internalProps = base.Props;
                internalProps.fuelUse = (int)(base.Props.fuelUse * (1 + tempAmpBonus)); // Already got the bonus, why recalc again?
                // This will break (Well, wrong results) if the ampbonus is not 0.25 for some reason
                // magic numbers: 0.25 to figure out how many amps are attached, amp is 1X5 (5), cap is 3X5 (15), this makes total area
                // regular AddToCache already gets the engine, only assigning 50% of the enginemass because caps extra volume and it costs a ton more
                cachedMass = (int)(((tempAmpBonus / 0.25f) * 5) + 15) * 30;
                fullyFormed = true;
            }
        }

    }
}