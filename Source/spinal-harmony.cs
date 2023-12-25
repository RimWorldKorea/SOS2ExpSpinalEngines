using HarmonyLib;
using RimWorld;
using Verse;


/*TODO
 * Fuel postfix called... While paused and visible? The hell?
 * Fuel use returns 0's after some decon nonsense, somehow
 */

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
        
        public static float SpinalRecalc(CompEngineTrail instance) //stolen from Building_ShipTurret but modified 
        {
            int amplifierCount = 0;
            float ampBoost = 0;
            bool foundNonAmp = false;
            Thing amp = instance.parent;
            IntVec3 previousThingPos;
            IntVec3 vecMoveCheck;
            if (instance.parent.def.defName == "Ship_Engine_Spinal") // This feels filthy I bet there is a better way TODO
            {
                CompSpinalMount spinalComp = instance.parent.TryGetComp<CompSpinalMount>();
                if (spinalComp == null)
                {
                    Log.Message("spinalcomp in SpinalRecalc null");
                    return 0;
                }
                if (instance.parent.Map == null)
                {
                    Log.Message("parent map in SpinalRecalc was null!");
                    return 0;
                }
                vecMoveCheck = -1 * amp.Rotation.FacingCell; // OG SOS2 basically rebuilt FacingCell, someone pointed that out to me
                // Log.Message(string.Format("found rotation in SpincalRecalc, iterating, vec is: {0}", vec));
                previousThingPos = amp.Position - (2 * vecMoveCheck); //Engines are inverse side from turrets, and engines are lorge
                while (!foundNonAmp)
                {
                    previousThingPos -= vecMoveCheck;
                    amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(instance.parent.Map);
                    if (amp == null)
                    {
                        Log.Message("new amp at previousthingspos in SpinalRecalc was null!");
                        return 0; 
                    }
                    // Log.Message("Amp not null");
                    CompSpinalMount ampComp = amp.TryGetComp<CompSpinalMount>();
                    // Log.Message(string.Format("vecs are: amp, {0} Previous {1}", amp.Position, previousThingPos));
                    if (amp.Rotation.Opposite != instance.parent.Rotation) // Remember, engines vs amps are inverted
                    {
                        Log.Message("amps rotation in SpinalRecalc did not match parent rotation");
                        amplifierCount = -1;
                        break;
                    }
                    //This is... A way, to check I guess. (amp = ptp.gftwc<csm> returns thing, if that is *also* the center, 1-wide, must? be amp) 
                    if (amp.Position == previousThingPos)
                    {
                        amplifierCount += 1;
                        // Log.Message(string.Format("amp found, ampcount is {0}", AmplifierCount));
                        ampBoost += ampComp.Props.ampAmount;
                        ampComp.SetColor(spinalComp.Props.color);
                    }
                    //found emitter
                    // Log.Message(string.Format("Looking for stackEnd results at {0}, result of: {1}", ampComp, ampComp.Props.stackEnd));
                    else if (amp.Position == previousThingPos - vecMoveCheck && ampComp.Props.stackEnd)
                    {
                        // removed + 1 amp count because initializing to -1 feels kind of illegal
                        foundNonAmp = true;
                    }
                    //found unaligned
                    else
                    {
                        Log.Message(string.Format("amp.position: {0}, previouspos minus vec: {1} ", amp.Position, previousThingPos - vecMoveCheck));
                        Log.Message("Found unaligned in SpinalRecalc");
                        amplifierCount = -1;
                        foundNonAmp = true;
                    }
                }
                Log.Message(string.Format("Found amps on spinal engine: {0}, boost: {1}", amplifierCount, ampBoost));

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
            Thing amp = SpinalObject;
            vecMoveCheck = -1 * amp.Rotation.FacingCell; 
            if (amp.Map == null)
            {
                Log.Message("parent map on trying to find EngineFromSpinal was null!"); // Something went rather sideways, is this log.error? 
                return null;
            }
            // Log.Message($"found rotation on a spinal component, iterating, vecMoveCheck is: {vecMoveCheck}");
            previousThingPos = amp.Position;
            if (SpinalObject.def.defName == "ShipSpinalEmitter")
            {
                previousThingPos -= vecMoveCheck; // Jump one more if it's a capacitor, to reach the amp
            }
            do
            {
                previousThingPos -= vecMoveCheck;
                // Log.Message($"Climbing spinal to find engine, looking at: {previousThingPos}");
                // Log.Message($"previousThingPos is {previousThingPos} and amp.Position is {amp.Position} and amp.Map.Biome is {amp.Map.Biome}");
                amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(amp.Map);
                if (amp == null)
                {
                    Log.Message("amp in find EngineFromSpinal was null!");
                    return null;
                }

                // Log.Message($"amp.position is {amp.Position} and previousthingpos - vec - vec is {previousThingPos - vec - vec} ampdefname is {amp.def.defName}");
                if (amp.Rotation != SpinalObject.Rotation && "Ship_Engine_Spinal" != amp.def.defName) // Engines are backwards! 
                {
                    Log.Message("amps rotation in EngineFromSpinal did not match parent rotation");
                    break;
                }
                else if (amp.Position == previousThingPos)
                {
                    // Log.Message(string.Format("amp found, ampcount is {0}", AmplifierCount));
                    // ampComp.SetColor(spinalComp.Props.color);
                    // Left in case I need this block later, but seems unlikely: Don't care how many are found here, don't setcolour till checking from an engine
                }
                //found engine (Note the double -vecMoveCheck!)
                else if (previousThingPos - vecMoveCheck - vecMoveCheck == amp.Position && "Ship_Engine_Spinal" == amp.def.defName) // Is there a better check?
                {
                    Log.Message("EngineFromSpinal found an engine and is about to return it");
                    return (Building)amp;
                    // foundNonAmp = true; // return bails anyhow
                }
                //found unaligned
                else
                {
                    Log.Message(string.Format("amp.position: {0}, previouspos minus vec: {1} ", amp.Position, previousThingPos - vecMoveCheck));
                    Log.Message("Found unaligned in EngineFromSpinal");
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
    [HarmonyPatch(typeof(CompEngineTrail), nameof(CompEngineTrail.Thrust), MethodType.Getter)]
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
    }
    [HarmonyPatch(typeof(CompEngineTrail), nameof(CompEngineTrail.Props), MethodType.Getter)]
    public class PatchEngineFuel
    {
        public static void Postfix(ref CompProperties_EngineTrail __result, CompEngineTrail __instance)
        {
            // Log.Message("SOS2ExpSE Postfix enginetrail props called");
            if (__instance.parent.def.defName == "Ship_Engine_Spinal") // This feels filthy I bet there is a better way TODO
            {
                float tempAmpBonus = SOS2ExpSpinalEngines.SpinalRecalc(__instance);
                if (tempAmpBonus != 0)
                {
                    Log.Message($"Spinal Engine fuel (OG {__result.fuelUse}) postfixed value (Bonus {tempAmpBonus}) is: {__result.fuelUse * (1 + tempAmpBonus)}");
                    // __result.fuelUse = (int)(__result.fuelUse * (1 + tempAmpBonus));
                }
                else // Incomplete spinal engine, no fuel use
                {
                     // __result.fuelUse = 0;
                }
            } // postfix so no "else", already correct value
        }
    }
    [HarmonyPatch(typeof(SaveOurShip2.DoSpawn), nameof(SaveOurShip2.DoSpawn.OnSpawn), MethodType.Normal)] // postfixing a postfix feels wrong 
    public class PatchSpinalSpawn
    {
       public static void Postfix(object[] __args)
        {
            if ((bool)__args[2])
                return;
            if (__args[0] == null || __args[1] == null || __args[2] == null)
            {
                Log.Error("Did something change in SOS2OnSpawn? Invalid for patching from SOS2ExpSpinalEngines");
                return;
            }
            // Can't say I care for all this but OnSpawn is static so I gotta steal the args and rebuild half of it apparently
            Map argMap = (Map)__args[1];
            Building argBuilding = (Building)__args[0];
            // Log.Message("SOS2ExpSE Postfix Postfix (lol) spawn called");
            var mapComp = argMap.GetComponent<ShipHeatMapComp>();
            if (mapComp.CacheOff || mapComp.ShipsOnMapNew.NullOrEmpty())
            {
                return;
            }
            foreach (IntVec3 vec in GenAdj.CellsOccupiedBy(argBuilding))
            { 
                // Log.Message("part on ship");
                int shipIndex = mapComp.ShipIndexOnVec(vec);
                if (shipIndex == -1)
                {
                    return; // Might not be an error, may just be partly on ship, or not on at all, check other parts
                }
                SoShipCache shipToOperateOn = mapComp.ShipsOnMapNew[shipIndex];
                shipToOperateOn.AddToCache(argBuilding); // I think? this fails but is a hashset anyhow? 
                if (argBuilding.TryGetComp<CompSpinalMount>() == null)
                {
                    Log.Message("SOS2ExpSpinalEngines SpinalComp null");
                    return;
                }
                // Log.Message("Found a spinal component in postpostfix");
                Building foundEngine = TheCafFiend.SOS2ExpSpinalEngines.EngineFromSpinal(argBuilding);
                if (foundEngine == null)
                {
                    Log.Message("SOS2ExpSpinalEngines FoundSpinalComp null");
                    return;
                }
                Log.Message("Adding thrust for spinal after climb and find");
                // I hate this but the only thing really not cached from an incomplete spinal is the thrust so just directly add it, what could go wrong?
                CompEngineTrail foundEngineComp = foundEngine.TryGetComp<CompEngineTrail>();
                if (foundEngineComp != null)
                {
                    //Everything but the thrust and the engine in the cache should be already in a good state
                    shipToOperateOn.ThrustRaw += foundEngineComp.Thrust;
                    return;
                    // Don't check other cells, causes bugs
                }
            }
        }

    }

    [HarmonyPatch(typeof(SaveOurShip2.DoPreDeSpawn), nameof(SaveOurShip2.DoPreDeSpawn.PreDeSpawn), MethodType.Normal)] // Oh postfixing a prefix, clearly better
    public class PatchSpinalDespawn
    {
        // See previous complaint about static OnSpawn, s/onSpawn/PreDeSpawn
        public static void Postfix(object[] __args)
        {
            if (__args[0] == null || __args[1] == null)
            {
                Log.Error("Did something change in SOS2 DoPreDeSpawn? Invalid for patching from SOS2ExpSpinalEngines");
                return;
            }
            Building PassedBuilding = (Building)__args[0];
            // Log.Message($"Postfixed prefix PreDeSpawn PatchSpinalDespawn buildingname is {PassedBuilding.def.defName}");
            var mapComp = PassedBuilding.Map.GetComponent<ShipHeatMapComp>();
            if (mapComp.CacheOff)
            {
                return;
            }  
            CompSpinalMount FoundSpinalComp = PassedBuilding.TryGetComp<CompSpinalMount>();
            if (FoundSpinalComp == null)
            {
                return;
            }
            // Log.Message("Found a spinal component in postprefix");
            Building FoundEngine = TheCafFiend.SOS2ExpSpinalEngines.EngineFromSpinal(PassedBuilding);
            if (FoundEngine == null)
            {
                Log.Message("SOS2ExpSpinalEngines FoundEngine null");
                return;
            }
            int shipIndex = mapComp.ShipIndexOnVec(PassedBuilding.Position);
            if (shipIndex != 0)
            {
                SoShipCache ShipToOperateOn = mapComp.ShipsOnMapNew[shipIndex];
                // Easier than add, everything else is still there, just dump the thrust, the removal will clear the rest
                ShipToOperateOn.ThrustRaw -= FoundEngine.TryGetComp<CompEngineTrail>().Thrust;
                return; // DOn't check other cells covered by the same object
            }
        }
    }
}



