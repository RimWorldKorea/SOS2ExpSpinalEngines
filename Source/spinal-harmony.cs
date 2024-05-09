using HarmonyLib;
using SaveOurShip2;
using System.Collections.Generic;
using Verse;

namespace TheCafFiend
{
    [StaticConstructorOnStartup]
    public static class SOS2ExpSpinalEngines
    {
        static SOS2ExpSpinalEngines()
        {
            SOS2ExpSpinal_HarmonyPatch.DoPatching();
            // add any other startup shenanigins
            // Log.Message("SOS2ExpSpinal static constructor");
        }
        
        public static float SpinalRecalc(CompEngineTrail instance) //stolen from Building_ShipTurret but modified heavily
        {
            int amplifierCount = 0;
            float ampBoost = 0;
            bool foundNonAmp = false;
            Thing buildingPointer = instance.parent;
            IntVec3 previousThingPos;
            IntVec3 vecMoveCheck;
            if (instance.parent.TryGetComp<CompSpinalEngineTrail>() != null)
            {
                CompSpinalMount spinalComp = instance.parent.TryGetComp<CompSpinalMount>();
                if (spinalComp == null)
                {
                    Log.Message("SOS2 spinal engines: spinalcomp in SpinalRecalc null");
                    return 0;
                }
                if (instance.parent.Map == null)
                {
                    Log.Message("SOS2 spinal engines:  parent map in SpinalRecalc was null!");
                    return 0;
                }
                vecMoveCheck = -1 * buildingPointer.Rotation.FacingCell; // OG SOS2 basically rebuilt FacingCell, someone pointed that out to me
                //Log.Message(string.Format("found rotation in SpincalRecalc, iterating, vec is: {0}", vecMoveCheck));
                previousThingPos = buildingPointer.Position - (2 * vecMoveCheck); //Engines are inverse side from turrets, and engines are lorge
                while (!foundNonAmp)
                {
                    previousThingPos -= vecMoveCheck;
                    buildingPointer = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(instance.parent.Map);
                    if (buildingPointer == null)
                    {
                        Log.Message("SOS2 spinal engines:  new amp at previousthingspos in SpinalRecalc was null!");
                        return 0; 
                    }
                    // Log.Message("Amp not null");
                    CompSpinalMount ampComp = buildingPointer.TryGetComp<CompSpinalMount>();
                    //Log.Message($"vecs are: amp, {buildingPointer.Position} Previous {previousThingPos}");
                    if (buildingPointer.Rotation.Opposite != instance.parent.Rotation) // Remember, engines vs amps are inverted
                    {
                        Log.Message("SOS2 spinal engines: amps rotation in SpinalRecalc did not match parent rotation");
                        amplifierCount = -1;
                        break;
                    }
                    //This is... A way, to check I guess. (buildingPointer = prevthingppos.getfirstcomp<spinal> above returns thing, if that is *also* the center, 1-wide, must? be amp) 
                    if (buildingPointer.Position == previousThingPos)
                    {
                        amplifierCount += 1;
                        //Log.Message(string.Format("amp found, ampcount is {0}", amplifierCount));
                        ampBoost += ampComp.Props.ampAmount;
                        ampComp.SetColor(spinalComp.Props.color);
                    }
                    //found emitter
                    //Log.Message(string.Format("Looking for stackEnd results at {0}, result of: {1}", ampComp, ampComp.Props.stackEnd));
                    else if (buildingPointer.Position == previousThingPos - vecMoveCheck && ampComp.Props.stackEnd)
                    {
                        // removed + 1 amp count because initializing to -1 feels kind of illegal
                        foundNonAmp = true;
                    }
                    //found unaligned
                    else
                    {
                        Log.Message($"SOS2 spinal engines: Unaligned in SpinalRecalc: buildingPointer.position: {buildingPointer.Position}, previouspos minus vec: {previousThingPos - vecMoveCheck}");
                        amplifierCount = -1;
                        foundNonAmp = true;
                    }
                }
                //Log.Message(string.Format("Found amps on spinal engine: {0}, boost: {1}", amplifierCount, ampBoost));

                if (0 < ampBoost)
                {
                    return ampBoost;
                }
            }
            return 0; 
        }

        public static Building EngineFromSpinal(Building SpinalObject) // derived from spinalrecalc but a bit too different to combine?
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
            if (buildingPointer.TryGetComp<CompSpinalMount>().Props.stackEnd)
            {
                previousThingPos -= vecMoveCheck; // Jump one more if it's a capacitor, to reach the amp
            }
            do
            {
                previousThingPos -= vecMoveCheck;
                //Log.Message($"Climbing spinal to find engine, looking at: {previousThingPos}");
                //Log.Message($"previousThingPos is {previousThingPos} and amp.Position is {amp.Position} and amp.Map.Biome is {amp.Map.Biome}");
                buildingPointer = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(buildingPointer.Map);
                if (buildingPointer == null)
                {
                    Log.Message("SOS2 spinal engines: amp in find EngineFromSpinal was null!");
                    return null;
                }

                //Log.Message($"amp.position is {amp.Position} and previousthingpos - vec - vec is {previousThingPos - vecMoveCheck - vecMoveCheck} ampdefname is {amp.def.defName}");
                if (buildingPointer.Rotation != SpinalObject.Rotation && buildingPointer.def.defName != "Ship_Engine_Spinal") // Engines are backwards! 
                {
                    Log.Message("SOS2 spinal engines: amps rotation in EngineFromSpinal did not match parent rotation");
                    break;
                }
                else if (buildingPointer.Position == previousThingPos)
                {
                    // Log.Message(string.Format("amp found, ampcount is {0}", AmplifierCount));
                    // ampComp.SetColor(spinalComp.Props.color);
                    // Left in case I need this block later, but seems unlikely: Don't care how many are found here, don't setcolour till checking from an engine
                }
                //found engine (Note the double -vecMoveCheck!)
                else if (previousThingPos - vecMoveCheck - vecMoveCheck == buildingPointer.Position && buildingPointer.def.defName == "Ship_Engine_Spinal") // Is there a better check?
                {
                    //Log.Message("SOS2 spinal engines: EngineFromSpinal found an engine and is about to return it");
                    return (Building)buildingPointer;
                }
                //found unaligned
                else
                {
                    Log.Message($"SOS2 spinal engines: Found unaligned in EngineFromSpinal amp.position: {buildingPointer.Position}, previouspos minus vec: {previousThingPos - vecMoveCheck}");
                    foundNonAmp = true;
                }

            } while (!foundNonAmp);
            return null; // Only? reachable if no additional spinal comps found in next step
        }
    }

    public class SOS2ExpSpinal_HarmonyPatch
    {
        public static void DoPatching()
        {
            // Log.Message("SOS2EXPSpinalEngines DoPatching called");
            var harmony = new Harmony("TheCafFiend.SOS2ExpSpinal"); 
            harmony.PatchAll();
        }
    }
    // Removed in order to just make an extended CompEngineTrail because honestly it seems a lot cleaner
    /*[HarmonyPatch(typeof(CompEngineTrail), nameof(CompEngineTrail.Thrust), MethodType.Getter)]
    public class PatchEngineThrust
    {
        public static void Postfix(ref int __result, CompEngineTrail __instance)
        {
            // Log.Message("SOS2ExpSpinalEngine PostFix from PatchEngineThrust called");
            if (__instance.parent.def.defName == "Ship_Engine_Spinal") // This feels filthy I bet there is a better way TODO
            {
                float TempAmpBonus = SOS2ExpSpinalEngines.SpinalRecalc(__instance);
                if (TempAmpBonus != 0)
                {
                    Log.Message($"Thrust of spinal returned was: {__result * (1 + TempAmpBonus)}");
                    __result = (int)(__result * (1 + TempAmpBonus));
                }
                else // Incomplete spinal, no thrust for you
                {
                    __result = 0;
                }
            } // postfix so no "else", already correct value
                
        }
    } */
    //Cannot? just extend Props because it's not marked virtual so I guess reflection it is. 
    /* got props marked as virtual (thanks SonicTHI!) so removed this nasty, not-correctly-working-anyhow, bit 
    [HarmonyPatch(typeof(CompEngineTrail), nameof(CompEngineTrail.Props), MethodType.Getter)]
    public class PatchEngineFuel
    {
        public static void Postfix(ref CompProps_EngineTrail __result, CompEngineTrail __instance)
        {
            // Log.Message("SOS2ExpSE Postfix enginetrail props called");
            if (__instance.parent.def.defName == "Ship_Engine_Spinal") // This feels filthy I bet there is a better way TODO
            {
                float tempAmpBonus = SOS2ExpSpinalEngines.SpinalRecalc(__instance);
                if (tempAmpBonus != 0)
                {
                    Log.Message($"Spinal Engine fuel (OG {__result.fuelUse}) postfixed value (Bonus {tempAmpBonus}) is: {__result.fuelUse * (1 + tempAmpBonus)}");
                    __result.fuelUse = (int)(__result.fuelUse * (1 + tempAmpBonus));
                }
                else // Incomplete spinal engine, no fuel use
                {
                     __result.fuelUse = 0;
                }
            } // postfix so no "else", already correct value
        }
    } */
    /*[HarmonyPatch(typeof(SaveOurShip2.DoSpawn), nameof(SaveOurShip2.DoSpawn.OnSpawn), MethodType.Normal)] // postfixing a postfix feels wrong 
    public class PatchSpinalSpawn
    {
        // quick and ugly info gathering
        public static bool Prefix(object[] __args)
        {
            if (__args[0] == null || __args[1] == null)
            {
                Log.Error("Did something change in SOS2 DoSpawn? Invalid for patching from SOS2ExpSpinalEngines");
                return false;
            }
            if ((bool)__args[2]) //onload
                return true;
            Building argBuilding = (Building)__args[0];
            var mapComp = argBuilding.Map.GetComponent<ShipMapComp>();
            int shipIndex = mapComp.ShipIndexOnVec(argBuilding.Position);
            SpaceShipCache shipToOperateOn = mapComp.ShipsOnMap[shipIndex];
            Log.Message($"postfixed postfix DoSpawn PatchSpinalSpawn buildingname is {argBuilding.def.defName}");
            Log.Message($"sos2expse adding, ship weight: {shipToOperateOn.MassSum}, ship thrust {shipToOperateOn.ThrustRaw} ratio {shipToOperateOn.ThrustRatio}");
            return true;
        }
        public static void Postfix(object[] __args)
        {
            //Log.Message("SOS2ExpSE Postfix Postfix DoSpawn called");
            // Can't say I care for all this but OnSpawn is static so I gotta steal the args and rebuild half of it apparently
            if (__args[0] == null || __args[1] == null || __args[2] == null)
            {
                Log.Error("Did something change in SOS2OnSpawn? Invalid for patching from SOS2ExpSpinalEngines");
                return;
            }
            if ((bool)__args[2]) //onspawn
                return;
            Map argMap = (Map)__args[1];
            Building argBuilding = (Building)__args[0];
            if (argBuilding.TryGetComp<CompSpinalMount>() == null)
            {
                //Log.Message("SOS2ExpSpinalEngines SpinalComp null");
                return; //Not spinal-related, bail ASAP
            }
            var mapComp = argMap.GetComponent<ShipMapComp>();
            if (mapComp.CacheOff || ShipInteriorMod2.MoveShipFlag || mapComp.ShipsOnMap.NullOrEmpty())
            {
                return;
            }
            foreach (IntVec3 vec in GenAdj.CellsOccupiedBy(argBuilding))
            { 
                // Log.Message($"part checking for on ship at {vec}");
                int shipIndex = mapComp.ShipIndexOnVec(vec);
                if (shipIndex == -1)
                {
                    continue; // Might not be an error, may just be partly on ship, or not on at all, check other parts
                }
                //Log.Message($"{shipIndex}");
                SpaceShipCache shipToOperateOn = mapComp.ShipsOnMap[shipIndex];
                Log.Message($"sos2expse postfix adding, ship weight: {shipToOperateOn.MassSum}, ship thrust {shipToOperateOn.ThrustRaw}, ratio {shipToOperateOn.ThrustRatio}");
                //Log.Message("Found a spinal component in postpostfix");
                Building foundEngine = TheCafFiend.SOS2ExpSpinalEngines.EngineFromSpinal(argBuilding);
                if (foundEngine == null)
                {
                    Log.Message("SOS2ExpSpinalEngines EngineFromSpinal null");
                    return;
                }
                // Log.Message("Adding thrust for spinal after climb and find");
                CompEngineTrail foundEngineComp = foundEngine.TryGetComp<CompSpinalEngineTrail>();
                if (foundEngineComp == null)
                {
                    Log.Error("SOS2Spinal engines foundEngineComp is null!");
                }
                // This will break (Well, wrong results) if the ampbonus is not 0.25 for some reason
                // magic numbers: 0.25 to figure out how many amps are attached, amp is 1X5 (5), cap is 3X5 (15), this makes total area
                // regular AddToCache already gets the engine, only assigning 50% of the enginemass because caps extra and it costs a ton more
                int spinalEngineMass = (int)(((SOS2ExpSpinalEngines.SpinalRecalc(foundEngineComp) / 0.25f) * 5) + 15) * 30;
                if (argBuilding.TryGetComp<CompSpinalEngineTrail>() != null) // Built building is engine itself
                {
                   /* Log.Message($"Postfixed postfix DoSpawn PatchSpinalSpawn buildingname is {argBuilding.def.defName}");
                    if (shipToOperateOn.Buildings.Contains(argBuilding))
                    {
                        Log.Message("shipCache contains spinal engine still");
                    }
                    Log.Message($"compenginetrail thrust is: {argBuilding.TryGetComp<CompEngineTrail>().Thrust}");
                    Log.Message($"compshipcachepart is: {argBuilding.TryGetComp<CompShipCachePart>()}, shippart is: {argBuilding.def.building.shipPart}");
                    Log.Message($"sos2se spawn postfix bailing due to found engine"); 
                    shipToOperateOn.EngineMass += spinalEngineMass;
                    Log.Message($"sos2expse postfix added now, ship weight: {shipToOperateOn.MassSum}, ship thrust {shipToOperateOn.ThrustRaw}, ratio {shipToOperateOn.ThrustRatio}");
                    return;
                }
                // If we made it this far, the addition was a spinal engine component but *not* the engine itself so we need special handling
                //Everything but the thrust and the additional EngineMass in the cache should be already in a good state
                shipToOperateOn.ThrustRaw += foundEngineComp.Thrust;
                shipToOperateOn.EngineMass += spinalEngineMass;
                Log.Message($"sos2expse postfix added now, ship weight: {shipToOperateOn.MassSum}, ship thrust {shipToOperateOn.ThrustRaw}, ratio {shipToOperateOn.ThrustRatio}");
                return; // Don't check other cells, causes bugs
            }
        }   

    }*/

    [HarmonyPatch(typeof(SaveOurShip2.SpaceShipCache), nameof(SaveOurShip2.SpaceShipCache.AddToCache), MethodType.Normal)] 
    public class PatchSpinalSpawn
    {
        public static bool Prefix(Building b, ref int ___EngineMass, ref float ___ThrustRaw, HashSet<Building> ___Buildings) //prefix due to DoSpawn running addcache on every single cell :/
        {
            Building argBuilding = b; 
            // Log.Message($"name of added is: {argBuilding.def.defName}");
            if (___Buildings.Contains(argBuilding))
            {
                return true; //building is already in cache, presumably re-running on other cells
            }
            if (argBuilding.TryGetComp<CompSpinalMount>() == null)
            {
                //Log.Message("SOS2ExpSpinalEngines SpinalComp null");
                return true; //Not spinal-related, bail ASAP
            }
            Building foundEngine = TheCafFiend.SOS2ExpSpinalEngines.EngineFromSpinal(argBuilding);
            if (foundEngine == null)
            {
                Log.Message("SOS2 SpinalEngines EngineFromSpinal null");
                return true;
            }
            CompSpinalEngineTrail foundEngineComp = foundEngine.TryGetComp<CompSpinalEngineTrail>();
            if (foundEngineComp == null)
            {
                Log.Error("SOS2Spinal engines foundEngineComp is null!");
            }
            // Log.Message($"About to check for engine/not, then add mass (can't check number, will form) to EngineMass {___EngineMass} foundcomp {foundEngineComp.fullyFormed}");
            if (argBuilding.TryGetComp<CompSpinalEngineTrail>() != null) // Built building is engine itself
            {
                ___EngineMass += foundEngineComp.supportWeight; //regular add would have already pulled thrust: Just completed spinal, so add mass
            }
            else if (!foundEngineComp.fullyFormed) // Attempts (by requesting weight/thrust) to build entire spinal, thus only runs once on mapload (successfully)
            {
                //only reached with a spinal component that isn't an engine but found an engine in EngineFromSpinal (aka amp/capacitor)
                ___EngineMass += foundEngineComp.supportWeight; //calling triggers the spinal recalc and if successfull, sets fullyFormed
                ___ThrustRaw += foundEngineComp.Thrust; 
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SaveOurShip2.SpaceShipCache), nameof(SaveOurShip2.SpaceShipCache.RemoveFromCache), MethodType.Normal)]
    public class PatchSpinalDeSpawn
    {
        public static void Postfix(Building b, ref int ___EngineMass, ref float ___ThrustRaw)
        {
            Building argBuilding = b;

            if (argBuilding.TryGetComp<CompSpinalMount>() == null)
            {
                //Log.Message("SOS2ExpSpinalEngines SpinalComp null");
                return; //Not spinal-related, bail ASAP
            }
            Building foundEngine = TheCafFiend.SOS2ExpSpinalEngines.EngineFromSpinal(argBuilding);
            if (foundEngine == null)
            {
                Log.Message("SOS2ExpSpinalEngines EngineFromSpinal null");
                return;
            }
            CompSpinalEngineTrail foundEngineComp = foundEngine.TryGetComp<CompSpinalEngineTrail>();
            if (foundEngineComp == null)
            {
                Log.Error("SOS2Spinal engines foundEngineComp is null!");
            }
            // Log.Message($"About to check for engine/not, then remove mass {foundEngineComp.supportWeight} from EngineMass {___EngineMass}");
            if (argBuilding.TryGetComp<CompSpinalEngineTrail>() != null) // Built building is engine itself
            {
                ___EngineMass -= foundEngineComp.supportWeight; //regular remove would have already pulled thrust: Just broke spinal, so remove mass
            }
            else if (foundEngineComp.fullyFormed) // *was* a complete spinal engine, now time to cleanup
            {
                // Log.Message($"fully formed spinal with a component busted, remove thrust {foundEngineComp.Thrust}");
                //only reached removing a spinal component that isn't an engine but found an engine in EngineFromSpinal (aka amp/capacitor)
                ___EngineMass -= foundEngineComp.supportWeight;
                ___ThrustRaw -= foundEngineComp.Thrust; 
                foundEngineComp.fullyFormed = false;
            }

        }
    }
    /*[HarmonyPatch(typeof(SaveOurShip2.DoPreDeSpawn), nameof(SaveOurShip2.DoPreDeSpawn.PreDeSpawn), MethodType.Normal)] // Oh prefixing a prefix, clearly better
    public class PatchSpinalDespawn
    {
        // quick and ugly info gathering
        /*public static bool Prefix(object[] __args)
        {
            if (__args[0] == null || __args[1] == null)
            {
                Log.Error("Did something change in SOS2 DoPreDeSpawn? Invalid for patching from SOS2ExpSpinalEngines");
                return false;
            }
            Building argBuilding = (Building)__args[0];
            var mapComp = argBuilding.Map.GetComponent<ShipMapComp>();
            int shipIndex = mapComp.ShipIndexOnVec(argBuilding.Position);
            SpaceShipCache shipToOperateOn = mapComp.ShipsOnMap[shipIndex];
            Log.Message($"*PRE*fixed prefix PreDeSpawn PatchSpinalDespawn buildingname is {argBuilding.def.defName}");
            Log.Message($"sos2expse removing, ship weight: {shipToOperateOn.MassSum}, ship thrust {shipToOperateOn.ThrustRaw} ratio {shipToOperateOn.ThrustRatio}");
            return true;
        }
        // See previous complaint about static OnSpawn, s/onSpawn/PreDeSpawn
        public static bool Prefix(object[] __args)
        {
            if (__args[0] == null || __args[1] == null)
            {
                Log.Error("Did something change in SOS2 DoPreDeSpawn? Invalid for patching from SOS2ExpSpinalEngines");
                return false;
            }
            Building argBuilding = (Building)__args[0];
            var mapComp = argBuilding.Map.GetComponent<ShipMapComp>();
            if (mapComp.CacheOff || ShipInteriorMod2.MoveShipFlag || mapComp.ShipsOnMap.NullOrEmpty())
            {
                return true; // This means re-checking in base SOS DoPreDeSpawn but a false will exit building DeSpawn I think?
            }
            CompSpinalMount FoundSpinalComp = argBuilding.TryGetComp<CompSpinalMount>();
            if (FoundSpinalComp == null)
            {
                // Log.Message("Despawn on non-spinal, returning to original");
                return true;
            }
            int shipIndex = mapComp.ShipIndexOnVec(argBuilding.Position);
            if (shipIndex == -1)
            {
                Log.Error("SOS2SpinalEngines null shipindex!");
                return false;
            }
            SpaceShipCache shipToOperateOn = mapComp.ShipsOnMap[shipIndex];
            Building foundEngine = TheCafFiend.SOS2ExpSpinalEngines.EngineFromSpinal(argBuilding);
            if (foundEngine == null)
            {
                Log.Message("SOS2ExpSpinalEngines FoundEngine null");
                return true;
            }
            CompEngineTrail foundEngineComp = foundEngine.TryGetComp<CompSpinalEngineTrail>();
            //Log.Message($"Prefixed prefix PreDeSpawn PatchSpinalDespawn buildingname is {argBuilding.def.defName}");
            //Log.Message($"sos2expse removing, ship weight: {shipToOperateOn.MassSum}, ship thrust {shipToOperateOn.ThrustRaw} ratio {shipToOperateOn.ThrustRatio}");
            // This will break (Well, wrong results) if the ampbonus is not 0.25 for some reason
            // magic numbers: 0.25 to figure out how many amps are attached, amp is 1X5 (5), cap is 3X5 (15), this makes total area 
            // regular AddToCache already gets the engine, only assigning 50% of the enginemass because caps extra and it costs a ton more
            int spinalEngineMass = (int)(((SOS2ExpSpinalEngines.SpinalRecalc(foundEngineComp) / 0.25f) * 5) + 15) * 30;
            if (argBuilding.TryGetComp<CompSpinalEngineTrail>() != null)
            {
                /*if (shipToOperateOn.Buildings.Contains(argBuilding))
                {
                    Log.Message("shipCache contains spinal engine on prefix of DoPreDespawn, which it should");
                }
                Log.Message($"sos2se remove postfix bailing due to found engine");
                Log.Message($"compenginetrail thrust is: {argBuilding.TryGetComp<CompEngineTrail>().Thrust}");
                Log.Message($"compshipcachepart is: {argBuilding.TryGetComp<CompShipCachePart>()}, shippart is: {argBuilding.def.building.shipPart}");
                shipToOperateOn.EngineMass -= spinalEngineMass;
                return true;
            }
            //Only fires on amp/capacitor removal making nonfunctional engine!
            shipToOperateOn.EngineMass -= spinalEngineMass;
            shipToOperateOn.ThrustRaw -= foundEngineComp.Thrust;
            Log.Message($"sos2expse removed, ship weight: {shipToOperateOn.MassSum} , ship thrust  {shipToOperateOn.ThrustRaw} ratio {shipToOperateOn.ThrustRatio}");
            return true;

        }
    }*/
}



