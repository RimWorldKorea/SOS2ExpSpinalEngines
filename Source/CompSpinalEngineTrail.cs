using RimWorld;
using SaveOurShip2;
using System.Text;
using Verse;

namespace TheCafFiend
{
    [StaticConstructorOnStartup]
    public class CompSpinalEngineTrail : CompEngineTrail
    {
        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (fullyFormed)
            {
                stringBuilder.Append($"Thrust: {cachedThrust * 500} Fuel Use: {cachedFuelUse}/s");
            }
            else
            {
                stringBuilder.Append(cachedError);
            }
            return stringBuilder.ToString();
        }
        public virtual CompProperties_SpinalEngineTrail Properties //Wait is this dirty I just want to be clear it's not hiding etc hrm
        {
            get { return props as CompProperties_SpinalEngineTrail; }
        }
        public class returnValue
        {
            public float thrustAmp;
            public float fuelAmp;
            public float fuelAllowAmp;
            public float powerUseAmp;
            public int supportWeight;
            public string playerError;
            public bool fullyFormed;
        }
        private int cachedThrust = 0;
        private int cachedMass = 0; // add/remove on the *engine* does this itself, this is only amps+caps
        public bool fullyFormed = false;
        //private float supportWeightMulti = 0.66f;
        private int cachedFuelUse = 0;
        private int cachedPowerUse = 0;
        private int cachedFuelAllowed = 0;
        private string cachedError = "Spinal engine isn't fully formed! Check for engine accelerators, and all three components of the fuel infrastructure!";
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
        public string currentError
        {
            set
            {
                cachedError = value;
            }
        }
        public int powerUse
        {
            get
            {
                if (parent.TryGetComp<CompSpinalEngineTrail>() == null)
                {
                    return (int)(parent.TryGetComp<CompPowerTrader>().Props.PowerConsumption); // pretty? Sure this cannot happen but in case
                }
                if (fullyFormed)
                { 
                    return 0 - cachedPowerUse; 
                }
                else
                {
                    return (int)(parent.TryGetComp<CompPowerTrader>().Props.PowerConsumption);
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
            returnValue wasReturned = SOS2SpinalEngines.SpinalRecalc(this);
            if (wasReturned == null) // null basically means something went wrong: Multiblock broken, re-set to zeros
            {
                cachedThrust = 0;
                cachedMass = 0;
                cachedFuelUse = 0;
                cachedFuelAllowed = 0;
                cachedPowerUse = 0;
                cachedError = "Spinal engine isn't fully formed! Check for engine accelerators, and all three components of the fuel infrastructure!";
                return;

            }
            else if(wasReturned.fullyFormed == false)
            {
                cachedError = wasReturned.playerError;
            }
            else
            {
                cachedThrust = (int)(base.Props.thrust * (1 + wasReturned.thrustAmp)); 
                cachedFuelUse = (int)(base.Props.fuelUse * (1 + wasReturned.fuelAmp));
                CompSpinalEnginePowerTrader powerComp = parent.TryGetComp<CompSpinalEnginePowerTrader>();
                cachedPowerUse += (int)(powerComp.Props.PowerConsumption * (1 + wasReturned.powerUseAmp));
                Log.Message($"sos2spinal engines base.powercomp.poweroutput from calc returned poweruse is {wasReturned.powerUseAmp} cached is {cachedPowerUse}");
                powerComp.PowerOutput = 0 - cachedPowerUse; 
                Log.Message($"sos2spinal engines modified comppowertrader PowerOutput on {parent.GetUniqueLoadID()} to {powerComp.PowerOutput}");
                cachedFuelAllowed += (int)(this.Properties.fuelAllowed * (1 + wasReturned.fuelAllowAmp)); // No current need to record it? Player info card?
                //Log.Message($"base fueluse reads {base.Props.fuelUse} Fueluse is: {cachedFuelUse}");
                // This will break (Well, wrong results) if the ampbonus is not 0.25 for some reason
                // magic numbers: 0.25 to figure out how many amps are attached, amp is 1X5 (5), cap is 3X5 (15), this makes total area
                // regular AddToCache already gets the engine, only assigning supportWeightMulti of the enginemass because caps extra volume and it costs a ton more
                cachedMass = (int)(wasReturned.supportWeight);
                fullyFormed = true;
            }
        }

    }


}