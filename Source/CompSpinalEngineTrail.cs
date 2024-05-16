using SaveOurShip2;
using Verse;

namespace TheCafFiend
{
    [StaticConstructorOnStartup]
    public class CompSpinalEngineTrail : CompEngineTrail
    {
        private int cachedThrust = 0;
        private int cachedMass = 0; // add/remove on the *engine* does this itself, this is only amps+caps
        public bool fullyFormed = false;
        private float supportWeightMulti = 0.66f;
        private int cachedFuelUse = 0;
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
                    //Log.Message($"sos2 spinal engines thrust called about a spinal engine, returning cached thrust {thrustCached}");
                    return cachedThrust;
                }
                else
                {
                    //Log.Message($"sos2 spinal engines thrust called about a spinal engine, not fully formed");
                    calc();
                    return cachedThrust;
                }
            }
        }
        public override int FuelUse
        {
            get
            {
                if (parent.TryGetComp<CompSpinalEngineTrail>() == null) //should never? happen but reflex
                {
                    Log.Message("SOS2 spinal engines: somehow hit FuelUse from a non-compspinalenginetrail?");
                    return base.FuelUse;
                }
                if (fullyFormed)
                {
                    //Log.Message($"sos2 spinal engines thrust called about a spinal engine, returning cached fuelUse {cachedFuelUse}");
                    return cachedFuelUse;
                }
                else
                {
                    //Log.Message($"sos2 spinal engines fuel called about a spinal engine, not fully formed");
                    calc();
                    return cachedFuelUse;
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
                    return 0;// is this a log.error? Something went squirelly 
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
                cachedThrust = 0;
                cachedMass = 0;
                cachedFuelUse = 0;
                return;

            }
            else
            {
                //Log.Message("sos2spinal engines base.Thrust from calc");
                cachedThrust = (int)(base.Props.thrust * (1 + tempAmpBonus)); // Do *not* use .Thrust or the base will end up doing recursion into overidden props
                //Log.Message("sos2spinal engines base.Props from calc");
                cachedFuelUse = base.Props.fuelUse;
                cachedFuelUse = (int)(base.Props.fuelUse * (1 + tempAmpBonus));
                //Log.Message($"base fueluse reads {base.Props.fuelUse} Fueluse is: {cachedFuelUse}");
                // This will break (Well, wrong results) if the ampbonus is not 0.25 for some reason
                // magic numbers: 0.25 to figure out how many amps are attached, amp is 1X5 (5), cap is 3X5 (15), this makes total area
                // regular AddToCache already gets the engine, only assigning supportWeightMulti of the enginemass because caps extra volume and it costs a ton more
                cachedMass = (int)(((((tempAmpBonus / 0.25f) * 5) + 15) * 60)*supportWeightMulti);
                fullyFormed = true;
            }
        }

    }
}