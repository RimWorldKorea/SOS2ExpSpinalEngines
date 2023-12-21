using RimWorld;
using Verse;
using HarmonyLib;


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
            int AmplifierCount = -1;
            if ("Ship_Engine_Spinal" == instance.parent.def.defName) // This feels filthy I bet there is a better way TODO
            {
                float AmplifierBonus = 0;
                CompSpinalMount spinalComp = instance.parent.TryGetComp<CompSpinalMount>();
                if (null == spinalComp)
                {
                    Log.Message("spinalcomp in SpinalRecalc null");
                    return 0;
                }
                float ampBoost = 0;
                bool foundNonAmp = false;
                Thing amp = instance.parent;
                IntVec3 previousThingPos;
                IntVec3 vec;
                Rot4 currRotation;
                if (0 == amp.Rotation.AsByte)
                {
                    vec = new IntVec3(0, 0, -1);
                }
                else if (1 == amp.Rotation.AsByte)
                {
                    vec = new IntVec3(-1, 0, 0);
                }
                else if (2 == amp.Rotation.AsByte)
                {
                    vec = new IntVec3(0, 0, 1);
                }
                else
                {
                    vec = new IntVec3(1, 0, 0);
                }
                // Log.Message(string.Format("found rotation in SpincalRecalc, iterating, vec is: {0}", vec));
                previousThingPos = amp.Position - vec; //Important switch for engine direction, inverse from "turret" 
                previousThingPos -= vec; // engines are beeg, move one more than a spinal engine
                do
                {
                    previousThingPos -= vec;
                    if (null == instance.parent.Map)
                    {
                        Log.Message("parent map in SpinalRecalc was null!");
                        return 0;
                    }
                    amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(instance.parent.Map);
                    if (null == amp)
                    {
                        Log.Message("amp map in SpinalRecalc was null!");
                        return 0; 
                    }
                    // Log.Message("Amp not null");
                    CompSpinalMount ampComp = amp.TryGetComp<CompSpinalMount>();
                    // Log.Message(string.Format("vecs are: amp, {0} Previous {1}", amp.Position, previousThingPos));
                    currRotation = amp.Rotation.Opposite; // Weapon vs engine directionality 
                    if (currRotation != instance.parent.Rotation)
                    {
                        Log.Message("amps rotation in SpinalRecalc did not match parent rotation");
                        AmplifierCount = -1;
                        break;
                    }
                    //This is... A way, to check I guess. (amp = ptp.gftwc<csm> returns thing, if that is *also* the center, 1-wide, must? be amp) 
                    if (amp.Position == previousThingPos)
                    {
                        AmplifierCount += 1;
                        // Log.Message(string.Format("amp found, ampcount is {0}", AmplifierCount));
                        ampBoost += ampComp.Props.ampAmount;
                        // ampComp.SetColor(spinalComp.Props.color);
                    }
                    //found emitter
                    // Log.Message(string.Format("Looking for stackEnd results at {0}, result of: {1}", ampComp, ampComp.Props.stackEnd));
                    else if (amp.Position == previousThingPos - vec && ampComp.Props.stackEnd)
                    {
                        AmplifierCount += 1;
                        foundNonAmp = true;
                    }
                    //found unaligned
                    else
                    {
                        Log.Message(string.Format("amp.position: {0}, previouspos minus vec: {1} ", amp.Position, previousThingPos - vec));
                        Log.Message("Found unaligned in SpinalRecalc");
                        AmplifierCount = -1;
                        foundNonAmp = true;
                    }
                } while (!foundNonAmp);
                Log.Message(string.Format("Found amps on spinal engine: {0}, boost: {1}", AmplifierCount, ampBoost));

                if (0 < ampBoost)
                {
                    AmplifierBonus = ampBoost;
                }

                return AmplifierBonus;
            }
            return 0; 
        }

        public static Building EngineFromSpinal(Building SpinalObject) // derived from spinalrecalc but a bit too different to combine?
        {
            IntVec3 previousThingPos;
            IntVec3 vec;
            Rot4 currRotation;
            bool foundNonAmp = false;
            Thing amp = SpinalObject;
            if (0 == amp.Rotation.AsByte)
            {
                vec = new IntVec3(0, 0, -1);
            }
            else if (1 == amp.Rotation.AsByte)
            {
                vec = new IntVec3(-1, 0, 0);
            }
            else if (2 == amp.Rotation.AsByte)
            {
                vec = new IntVec3(0, 0, 1);
            }
            else
            {
                vec = new IntVec3(1, 0, 0);
            }
            // Log.Message($"found rotation on a spinal component, iterating, vec is: {vec}");
            previousThingPos = amp.Position;
            if ("ShipSpinalEmitter" == SpinalObject.def.defName)
            {
                previousThingPos -= vec; // Jump one more if it's a capacitor, to reach the amp
            }
            do
            {
                previousThingPos -= vec;
                // Log.Message($"Climbing spinal to find engine, looking at: {previousThingPos}");
                if (null == amp.Map)
                {
                    Log.Message("parent map on trying to find EngineFromSpinal was null!"); // Something went rather sideways, is this log.error? 
                    return null;
                }
                // Log.Message($"previousThingPos is {previousThingPos} and amp.Position is {amp.Position} and amp.Map.Biome is {amp.Map.Biome}");
                amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(amp.Map);
                if (null == amp)
                {
                    Log.Message("amp in find EngineFromSpinal was null!");
                    return null;
                }
                CompSpinalMount ampComp = amp.TryGetComp<CompSpinalMount>();
                currRotation = amp.Rotation; // the same because non-engine

                // Log.Message($"amp.position is {amp.Position} and previousthingpos - vec - vec is {previousThingPos - vec - vec} ampdefname is {amp.def.defName}");
                if (currRotation != SpinalObject.Rotation && "Ship_Engine_Spinal" != amp.def.defName) // Engines are backwards! 
                {
                    Log.Message("amps rotation in EngineFromSpinal did not match parent rotation");
                    break;
                }
                else if (amp.Position == previousThingPos)
                {
                    // Log.Message(string.Format("amp found, ampcount is {0}", AmplifierCount));
                    // ampComp.SetColor(spinalComp.Props.color);
                    // Left in case I need this block later, but seems unlikely: Don't care how many are found here
                }
                //found engine
                else if (previousThingPos - vec - vec == amp.Position && "Ship_Engine_Spinal" == amp.def.defName) // Is there a better check?
                {
                    Log.Message("EngineFromSpinal found an engine and is about to return it");
                    return (Building)amp;
                    // foundNonAmp = true; // return bails anyhow
                }
                //found unaligned
                else
                {
                    Log.Message(string.Format("amp.position: {0}, previouspos minus vec: {1} ", amp.Position, previousThingPos - vec));
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
            var harmony = new Harmony("TheCafFiend.SOS2ExpSpinal"); //Not 100% sure but think this is just namespace stuff?
            harmony.PatchAll();
        }
    }
    [HarmonyPatch(typeof(CompEngineTrail), nameof(CompEngineTrail.Thrust), MethodType.Getter)]
    public class PatchEngineThrust
    {
        public static void Postfix(ref int __result, CompEngineTrail __instance)
        {
            Log.Message("SOS2ExpSpinalEngine PostFix from PatchEngineThrust called");
            if ("Ship_Engine_Spinal" == __instance.parent.def.defName) // This feels filthy I bet there is a better way TODO
            {
                float TempAmpBonus = SOS2ExpSpinalEngines.SpinalRecalc(__instance);
                if (0 != TempAmpBonus)
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
            if ("Ship_Engine_Spinal" == __instance.parent.def.defName) // This feels filthy I bet there is a better way TODO
            {
                float TempAmpBonus = SOS2ExpSpinalEngines.SpinalRecalc(__instance);
                if (0 != TempAmpBonus)
                {
                    Log.Message($"Spinal Engine fuel (OG {__result.fuelUse}) postfixed value (Bonus {TempAmpBonus}) is: {__result.fuelUse * (1 + TempAmpBonus)}");
                    __result.fuelUse = (int)(__result.fuelUse * (1 + TempAmpBonus));
                }
                else // Incomplete spinal engine, no fuel use
                {
                    __result.fuelUse = 0;
                }
            } // postfix so no "else", already correct value
        }
    }
    [HarmonyPatch(typeof(SaveOurShip2.DoSpawn), nameof(SaveOurShip2.DoSpawn.OnSpawn), MethodType.Normal)] // postfixing a postfix feels wrong 
    public class PatchSpinalSpawn
    {
       public static void Postfix(object[] __args)
        {
            // Can't say I care for all this but OnSpawn is static so I gotta steal the args and rebuild half of it apparently
            Map argMap = (Map)__args[1];
            Building argBuilding = (Building)__args[0];
            // Log.Message("SOS2ExpSE Postfix Postfix (lol) spawn called");
            if ((bool)__args[2])
                return;
            var mapComp = argMap.GetComponent<ShipHeatMapComp>();
            if (mapComp.CacheOff || mapComp.ShipsOnMapNew.NullOrEmpty() || argBuilding.TryGetComp<CompSoShipPart>() != null)
                return;
            foreach (IntVec3 vec in GenAdj.CellsOccupiedBy(argBuilding)) 
            {
                // Log.Message("part on ship");
                int shipIndex = mapComp.ShipIndexOnVec(vec);
                if (-1 != shipIndex)
                {
                    mapComp.ShipsOnMapNew[shipIndex].AddToCache(argBuilding); // I think? this fails but is a hashset anyhow? 
                    CompSpinalMount FoundSpinalComp = argBuilding.TryGetComp<CompSpinalMount>();
                    if (null != FoundSpinalComp)
                    {
                        Log.Message("Found a spinal component in postpostfix");
                        Building FoundEngine = TheCafFiend.SOS2ExpSpinalEngines.EngineFromSpinal(argBuilding);
                        if (null != FoundEngine)
                        {
                            Log.Message("Adding thrust for spinal after climb and find");
                            SoShipCache ShipToOperateOn = mapComp.ShipsOnMapNew[shipIndex];
                            // I hate this but the only thing really not cached from an incomplete spinal is the thrust so just directly add it, what could go wrong?
                            CompEngineTrail FoundEngineComp = FoundEngine.TryGetComp<CompEngineTrail>();
                            if (null != FoundEngineComp)
                            {
                                //Everything but the thrust and the engine in the cache should be already in a good state
                                ShipToOperateOn.ThrustRaw += FoundEngineComp.Thrust;
                                // ShipToOperateOn.Engines.Add(FoundEngineComp);
                                // Not currently removed by an invalid placement, don't think? it matters
                            }
                        }
                    }
                }
                return;
            }
        }

    }

    [HarmonyPatch(typeof(SaveOurShip2.DoPreDeSpawn), nameof(SaveOurShip2.DoPreDeSpawn.PreDeSpawn), MethodType.Normal)] // Oh postfixing a prefix, clearly better
    public class PatchSpinalDespawn
    {
        // See previous complaint about static OnSpawn, s/onSpawn/PreDeSpawn
        public static void Postfix(object[] __args)
        {
            Building PassedBuilding = (Building)__args[0];
            Log.Message($"Postfixed prefix PreDeSpawn PatchSpinalDespawn buildingname is {PassedBuilding.def.defName}");
            var mapComp = PassedBuilding.Map.GetComponent<ShipHeatMapComp>();
            if (mapComp.CacheOff)
                return;
            CompSpinalMount FoundSpinalComp = PassedBuilding.TryGetComp<CompSpinalMount>();
            if (null != FoundSpinalComp)
            {
                // Log.Message("Found a spinal component in postprefix");
                Building FoundEngine = TheCafFiend.SOS2ExpSpinalEngines.EngineFromSpinal(PassedBuilding);
                if (null != FoundEngine)
                {
                    int shipIndex = mapComp.ShipIndexOnVec(PassedBuilding.Position);
                    if (-1 != shipIndex)
                    {
                        SoShipCache ShipToOperateOn = mapComp.ShipsOnMapNew[shipIndex];
                        // Easier than add, everything else is still there, just dump the thrust, the upcoming removal will clear the rest
                        ShipToOperateOn.ThrustRaw -= FoundEngine.TryGetComp<CompEngineTrail>().Thrust;
                    } 
                }
            }
            return;
        }
    }
}



