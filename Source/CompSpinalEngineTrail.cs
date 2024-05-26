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
                stringBuilder.Append($"Fuel burn rate per second: {cachedFuelUse} of {cachedFuelAllowed} supported");
                stringBuilder.AppendInNewLine($"Thrust: {cachedThrust * 500}");
            }
            else
            {
                stringBuilder.Append(cachedError.Colorize(UnityEngine.Color.red));
            }
            return stringBuilder.ToString();
        }
        public virtual CompProperties_SpinalEngineTrail Properties //Wait is this dirty I just want to be clear it's not hiding hrm
        {
            get { return props as CompProperties_SpinalEngineTrail; }
        }
        public class ReturnValue
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
        private int cachedFuelUse = 0;
        private int cachedPowerUse = 0;
        private int cachedFuelAllowed = 0; // Error below probably never surfaces to a player but I want a fallback
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
        public string CurrentError // Feels a little silly to have a setter just for this, maybe cachedError should just be public? 
        {
            set
            {
                cachedError = value;
            }
        }
        public int PowerUse
        {
            get
            {
                if (parent.TryGetComp<CompSpinalEngineTrail>() == null)
                {
                    Log.Message("SOS2 spinal engines: somehow hit powerUse from a non-compspinalenginetrail?");
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
        public int SupportWeight
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

        public void Reset()
        {
            //Log.Message($"SOS2spinal engines: resetting: thrust {cachedThrust} mass {cachedMass} fueluse {cachedFuelUse} fuelallowed {cachedFuelAllowed} power {cachedPowerUse}");
            cachedThrust = 0;
            cachedMass = 0;
            cachedFuelUse = 0;
            cachedFuelAllowed = 0;
            cachedPowerUse = 0;
            cachedError = "Spinal engine isn't fully formed! Check for engine accelerators, and all three components of the fuel infrastructure!";
        }
        private void calc()
        {
            ReturnValue wasReturned = SOS2SpinalEngines.SpinalRecalc(this);
            if (wasReturned == null) // null basically means something went wrong: Multiblock broken, re-set to zeros
            {
                Reset();
                return;
            }
            else if(wasReturned.fullyFormed == false)
            {
                cachedError = wasReturned.playerError; //cleanest way I could think of to surface errors? 
            }
            else
            {
                Reset(); //I can't see this being needed but eh 
                cachedThrust = (int)(base.Props.thrust * (1 + wasReturned.thrustAmp)); 
                cachedFuelUse = (int)(base.Props.fuelUse * (1 + wasReturned.fuelAmp));
                CompSpinalEnginePowerTrader powerComp = parent.TryGetComp<CompSpinalEnginePowerTrader>(); // removing this makes the game NRE?
                cachedPowerUse += (int)(powerComp.Props.PowerConsumption * (1 + wasReturned.powerUseAmp));// Ok I guess I keep powerComp?!
                //Log.Message($"sos2spinal engines base.powercomp.poweroutput from calc returned poweruse is {wasReturned.powerUseAmp} cached is {cachedPowerUse}");
                powerComp.PowerOutput = 0 - cachedPowerUse; 
                //Log.Message($"sos2spinal engines modified comppowertrader PowerOutput on {parent.GetUniqueLoadID()} to {powerComp.PowerOutput}");
                cachedFuelAllowed += (int)(this.Properties.fuelAllowed * (1 + wasReturned.fuelAllowAmp));
                cachedMass = (int)(wasReturned.supportWeight);
                fullyFormed = true;
            }
        }

    }


}