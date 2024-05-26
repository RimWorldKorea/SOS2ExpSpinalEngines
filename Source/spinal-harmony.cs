using HarmonyLib;
using SaveOurShip2;
using System.Collections.Generic;
using Verse;

namespace TheCafFiend
{
    [StaticConstructorOnStartup]
    public static class SOS2SpinalEngines
    {
        static SOS2SpinalEngines()
        {
            SOS2ExpSpinal_HarmonyPatch.DoPatching();
            // add any other startup shenanigins
        }
        
        public static CompSpinalEngineTrail.ReturnValue SpinalRecalc(CompSpinalEngineTrail instance) 
        {
            bool foundNonAmp = false;
            bool foundFuelStart = false;
            bool foundFuelEnd = false;
            bool foundNonFuelAmp = false;
            Thing buildingPointer = instance.parent;
            IntVec3 previousThingPos;
            IntVec3 vecMoveCheck;
            CompSpinalEngineTrail.ReturnValue toReturn = new CompSpinalEngineTrail.ReturnValue
            {
                fullyFormed = false
            };
            if (instance.parent.TryGetComp<CompSpinalEngineTrail>() == null)
            {
                return null;
            }
            CompSpinalEngineMount spinalComp = instance.parent.TryGetComp<CompSpinalEngineMount>();
            if (spinalComp == null)
            {
                Log.Message("SOS2 spinal engines: spinalcomp in SpinalRecalc null");
                return null;
            }
            if (instance.parent.Map == null)
            {
                Log.Message("SOS2 spinal engines: parent map in SpinalRecalc was null!");
                return null;
            }
            vecMoveCheck = -1 * buildingPointer.Rotation.FacingCell; // OG SOS2 basically rebuilt FacingCell, someone pointed that out to me
            //Log.Message(string.Format("found rotation in SpincalRecalc, iterating, vec is: {0}", vecMoveCheck));
            previousThingPos = buildingPointer.Position - (2 * vecMoveCheck); //Engines are inverse side from turrets, and engines are lorge
            while (!foundNonAmp)
            {
                previousThingPos -= vecMoveCheck;
                buildingPointer = previousThingPos.GetFirstThingWithComp<CompSpinalEngineMount>(instance.parent.Map);
                if (buildingPointer == null)
                {
                    //Log.Message($"SOS2 spinal engines:  new amp at {previousThingPos} in SpinalRecalc was null!");
                    foundNonAmp = true; //Currently unused in this case but might need it for a later plan
                    break;
                }
                CompSpinalEngineMount ampComp = buildingPointer.TryGetComp<CompSpinalEngineMount>();
                //Log.Message($"SOS2 Spinal Engines: vecs are: pointer, {buildingPointer.Position} Previous {previousThingPos}");
                if (buildingPointer.Rotation.Opposite != instance.parent.Rotation) // Remember, engines vs amps are inverted
                {
                    //Log.Message("SOS2 spinal engines: amps rotation in SpinalRecalc did not match parent rotation");
                    toReturn.playerError = "Backwards spinal component found!";
                    return toReturn;
                }
                //This is... A way, to check I guess. (buildingPointer = prevthingppos.getfirstcomp<spinal> above returns thing, if that is *also* the center, 1-wide, must? be amp) 
                if (buildingPointer.Position == previousThingPos)
                {
                    //Log.Message($"amp found, toreturn.thrustAmp is {toReturn.thrustAmp}");
                    toReturn.thrustAmp += ampComp.Props.thrustAmp;
                    toReturn.fuelAmp += ampComp.Props.fuelUseAmp;
                    toReturn.powerUseAmp += ampComp.Props.powerUseAmp;
                    toReturn.supportWeight += ampComp.Props.supportWeight; //Currently none have a weight set but maybe I want one that does! 
                    toReturn.fuelAllowAmp += ampComp.Props.fuelAllowAmp; // ok for sure none have this and... Will... They? Sounds illegal
                    //ampComp.SetColor(spinalComp.Props.color);
                }
                //found emitter
                else if (buildingPointer.Position == previousThingPos - vecMoveCheck && ampComp.Props.stackEnd)
                {
                    foundNonAmp = true;
                    foundFuelStart = true;
                    toReturn.supportWeight += ampComp.Props.supportWeight; // likely first *real* supportWeight
                }
                //found unaligned, something rather odd
                else
                {
                    //Log.Message($"SOS2 spinal engines: Unaligned in SpinalRecalc: buildingPointer.position: {buildingPointer.Position}, previouspos minus vec: {previousThingPos - vecMoveCheck}");
                    return null;
                }
            }
            //Log.Message($"Found amps on spinal engine: ThrustBoost: {toReturn.thrustAmp}, fuelstart is {foundFuelStart}");
            if (toReturn.thrustAmp ==0)
            {
                toReturn.playerError = "No engine accelerators found attached to engine!";
                return toReturn;
            }

            if (foundFuelStart == false)
            {
                toReturn.playerError = "No fuel support infrastructure found; Ensure accelerators are connected directly into the fuel support end!";
                return toReturn;
            }
            // to get here, foundNonAmp is true, foundFuelStart is true, and it didn't bail earlier on a broken rotation/nonamp
            previousThingPos -= vecMoveCheck; // one extra for the size of the fuelEnd! 
            while (!foundNonFuelAmp)
            {
                //Log.Message($"SOS2 spinal engines: SpinalRecalc: buildingPointer.position: {buildingPointer.Position}, previouspos: {previousThingPos}");
                previousThingPos -= vecMoveCheck;
                buildingPointer = previousThingPos.GetFirstThingWithComp<CompSpinalEngineMount>(instance.parent.Map);
                if (buildingPointer == null)
                {
                    //Log.Message($"SOS2 spinal engines:  new *fuel*amp at previousthingspos in SpinalRecalc was null");
                    break;
                }
                CompSpinalEngineMount ampComp = buildingPointer.TryGetComp<CompSpinalEngineMount>();
                if (buildingPointer.Rotation.Opposite != instance.parent.Rotation) // Remember, engines vs amps are inverted
                {
                    toReturn.playerError = "Backwards spinal component found!";
                    return toReturn;
                }
                if (buildingPointer.Position == previousThingPos && ampComp.Props.fuelStackEnd == false) // tank ends are also 1 wide!
                {
                    //Log.Message($"*fuel*amp found, toreturn.supportweight is {toReturn.supportWeight} pointerpos {buildingPointer.Position} prevpos {previousThingPos}");
                    toReturn.thrustAmp += ampComp.Props.thrustAmp; // This would be weird but just in case I want it later
                    toReturn.fuelAmp += ampComp.Props.fuelUseAmp; // ditto
                    toReturn.powerUseAmp += ampComp.Props.powerUseAmp;
                    toReturn.supportWeight += ampComp.Props.supportWeight;
                    toReturn.fuelAllowAmp += ampComp.Props.fuelAllowAmp;
                }
                else if (buildingPointer.Position == previousThingPos && ampComp.Props.fuelStackEnd == true)
                {
                    //Log.Message($"Found fuel end");
                    foundNonFuelAmp = true;
                    foundFuelEnd = true;
                }
            }
            // Didn't bail trying to build the fuel infra!
            if (foundFuelEnd == false) // Incomplete fuel
            {
                toReturn.playerError = "Fuel infrastructure found, but not a complete set: Is the end missing?";
                return toReturn;
            }
            //Also bails if someone tries to build a cheeky 0-middle engine fuel support. (int) because floating point is the devil
            if ((int)(toReturn.fuelAllowAmp * instance.Properties.fuelAllowed) < (int)(toReturn.fuelAmp * instance.Properties.fuelUse)) 
            {
                toReturn.playerError = $"Insufficent fuel structures to support accelerators: Requires {(int)(toReturn.fuelAmp * instance.Properties.fuelUse)} fuel/second, can supply {(int)(toReturn.fuelAllowAmp * instance.Properties.fuelAllowed)} per second";
                return toReturn;
            }
            // After all of the previous this has? to be a real spinal, right?
            toReturn.fullyFormed = true;
            return toReturn;
        }

        public static Building EngineFromSpinal(Building SpinalObject) // derived from spinalrecalc but too different to combine
        {
            IntVec3 previousThingPos;
            IntVec3 vecMoveCheck;
            bool foundNonAmp = false;
            Thing buildingPointer = SpinalObject;
            vecMoveCheck = -1 * buildingPointer.Rotation.FacingCell; 
            if (buildingPointer.Map == null)
            {
                Log.Message("SOS2 spinal engines: parent map on trying to find EngineFromSpinal was null!"); // Something went rather sideways, is this log.error? 
                return null;
            }
            // return the engine if this building is, the engine
            if (SpinalObject.TryGetComp<CompSpinalEngineTrail>() != null)
            {
                return SpinalObject;
            }
            //Log.Message($"found rotation on a spinal component, iterating, vecMoveCheck is: {vecMoveCheck}");
            previousThingPos = buildingPointer.Position;
            do
            {
                if (buildingPointer.TryGetComp<CompSpinalMount>().Props.stackEnd)
                {
                    previousThingPos -= vecMoveCheck; // Jump one more if it's a tankend, to reach the amp
                }
                previousThingPos -= vecMoveCheck;
                //Log.Message($"SOS2 Spinal Engines: Climbing spinal to find engine, looking at: {previousThingPos}");
                //Log.Message($"SOS2 Spinal Engines: previousThingPos is {previousThingPos} and BuildingPointer.Position is {buildingPointer.Position}");
                buildingPointer = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(buildingPointer.Map);
                if (buildingPointer == null) //usually just first disconnected builds
                {
                    return null;
                }

                //Log.Message($"SOS2 Spinal Engine: BuildingPointer.position is {buildingPointer.Position} and previousthingpos - vec - vec is {previousThingPos - vecMoveCheck - vecMoveCheck} pointerdefname is {buildingPointer.def.defName}");
                //Log.Message($"SOS2 Spinal Engines: pointer rot {buildingPointer.Rotation}, original spinalobject is {SpinalObject.def.defName}, rot {SpinalObject.Rotation}");
                if (buildingPointer.Rotation != SpinalObject.Rotation && buildingPointer.TryGetComp<CompSpinalEngineTrail>() == null) // Engines are backwards! 
                {
                    //Log.Message("SOS2 spinal engines: amps rotation in EngineFromSpinal did not match parent rotation");
                    break;
                }
                else if (buildingPointer.Position == previousThingPos)
                {
                    // Log.Message(string.Format("amp found, ampcount is {0}", AmplifierCount));
                    // Left in case I need this block later, but seems unlikely: Don't care how many are found here, don't setcolour till checking from an engine
                }
                //found engine (Note the double -vecMoveCheck!)
                else if (previousThingPos - vecMoveCheck - vecMoveCheck == buildingPointer.Position && (buildingPointer.TryGetComp<CompSpinalEngineTrail>() != null))
                {
                    //Log.Message("SOS2 spinal engines: EngineFromSpinal found an engine and is about to return it");
                    return (Building)buildingPointer;
                }
                //found unaligned
                else
                {
                    //Log.Message($"SOS2 spinal engines: Found unaligned in EngineFromSpinal amp.position: {buildingPointer.Position}, previouspos minus vec: {previousThingPos - vecMoveCheck}");
                    foundNonAmp = true;
                }

            } while (!foundNonAmp);
            return null; // Only? reachable if no additional spinal comps found?
        }
    }

    public class SOS2ExpSpinal_HarmonyPatch
    {
        public static void DoPatching()
        {
            var harmony = new Harmony("TheCafFiend.SOS2ExpSpinal"); 
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(SaveOurShip2.SpaceShipCache), nameof(SaveOurShip2.SpaceShipCache.AddToCache), MethodType.Normal)] 
    public class PatchSpinalSpawn // Must be a prefix to check shipcache *current* state, not post-building-add 
    {
        public static bool Prefix(Building b, ref int ___EngineMass, ref float ___ThrustRaw, HashSet<Building> ___Buildings, ref List<CompEngineTrail> ___Engines) //prefix due to DoSpawn running addcache on every single cell :/
        {
            Building argBuilding = b; 
            // Log.Message($"SOS2 Spinal Engines: name of added is: {argBuilding.def.defName}");
            if (___Buildings.Contains(argBuilding))
            {
                return true; //building is already in cache, presumably re-running on other cells
            }
            if (argBuilding.TryGetComp<CompSpinalEngineMount>() == null) 
            {
                //Log.Message("SOS2ExpSpinalEngines SpinalComp null");
                return true; //Not spinal-related, bail ASAP
            }
            Building foundEngine = TheCafFiend.SOS2SpinalEngines.EngineFromSpinal(argBuilding);
            if (foundEngine == null)
            {
                //Log.Message("SOS2 SpinalEngines EngineFromSpinal null");
                return true; //amp/cap but no engine
            }
            CompSpinalEngineTrail foundEngineComp = foundEngine.TryGetComp<CompSpinalEngineTrail>();
            if (foundEngineComp == null)
            {
                Log.Error($"SOS2Spinal engines foundEngineComp is null! foundengine {foundEngine.def.defName}");
                return true; // I guess? true? 
            }
            if (argBuilding.TryGetComp<CompSpinalEngineTrail>() != null) // Built building is engine itself
            {
                ___EngineMass += foundEngineComp.SupportWeight; //regular add will soon pull thrust: Just completed spinal, so add mass
                return true;
            }
            if (foundEngineComp.fullyFormed == false)
            {
                //only reached with a spinal component that isn't an engine but found an engine in EngineFromSpinal (aka amp/capacitor)
                if (___Engines.Contains(foundEngineComp) && ___Buildings.Contains(foundEngine)) // Building is support, engine already added
                {
                    ___EngineMass += foundEngineComp.SupportWeight; //calling triggers the spinal recalc and if successfull, sets fullyFormed
                    ___ThrustRaw += foundEngineComp.Thrust; //Direct manipulation of the cache: Best I can think of but is there better?
                    return true;
                }
                if (foundEngineComp.Thrust != 0) // If every check above bails this will actually *cause* the recalc
                {
                    if (!___Engines.Contains(foundEngineComp) && ___Buildings.Contains(foundEngine)) // is engine purged from list BUT still in shipcache? Broken spinal support
                    {
                        //Log.Message($"SOS2spinal engines: re-adding engine to internal engine list after spinal supports were removed earlier");
                        ___Engines.Add(foundEngineComp);
                        ___EngineMass += foundEngineComp.SupportWeight;
                        ___ThrustRaw += foundEngineComp.Thrust;
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SaveOurShip2.SpaceShipCache), nameof(SaveOurShip2.SpaceShipCache.RemoveFromCache), MethodType.Normal)]
    public class PatchSpinalDeSpawn
    {
        public static void Postfix(Building b, ref int ___EngineMass, ref float ___ThrustRaw, ref List<CompEngineTrail> ___Engines)
        {
            Building argBuilding = b;

            if (argBuilding.TryGetComp<CompSpinalEngineMount>() == null)
            {
                //Log.Message("SOS2ExpSpinalEngines SpinalComp null");
                return; //Not spinal-related, bail ASAP
            }
            Building foundEngine = SOS2SpinalEngines.EngineFromSpinal(argBuilding);
            if (foundEngine == null)
            {
                //Log.Message("SOS2ExpSpinalEngines EngineFromSpinal null");
                return;
            }
            CompSpinalEngineTrail foundEngineComp = foundEngine.TryGetComp<CompSpinalEngineTrail>();
            if (foundEngineComp == null)
            {
                Log.Error("SOS2Spinal engines foundEngineComp is null!");
                return;
            }
            if (foundEngineComp.fullyFormed == true) // *was* a complete spinal engine, now time to cleanup
            {
                if (argBuilding.TryGetComp<CompSpinalEngineTrail>() != null) // Built building is engine itself
                {
                    ___EngineMass -= foundEngineComp.SupportWeight; //regular remove would have already pulled thrust: Just broke spinal, so remove mass
                    return;
                }
                // Log.Message($"SOS2 Spinal Engines: fully formed spinal with a component busted, remove thrust {foundEngineComp.Thrust}");
                //only reached removing a spinal component that isn't an engine but found an engine in EngineFromSpinal (aka amp/capacitor)
                ___EngineMass -= foundEngineComp.SupportWeight;
                ___ThrustRaw -= foundEngineComp.Thrust;
                foundEngineComp.Off();
                ___Engines.Remove(foundEngineComp); // Otherwise broken spinals in combat let engine still (visually) fire to no effect (also used to track for re-added parts)
                foundEngineComp.Reset(); //something was removed, start from scratch!
                foundEngineComp.CurrentError = $"A supporting {argBuilding.def.label} of the spinal engine was removed!"; //Only use of this setter hrm
                foundEngineComp.fullyFormed = false;
            }
        }
    }
}