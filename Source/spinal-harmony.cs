using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;


namespace TheCafFiend.SOS2ExpSpinalEngines
{
    [StaticConstructorOnStartup]
    public static class SOS2ExpSpinal
    {
        static SOS2ExpSpinal()
        {
            SOS2ExpSpinal_HarmonyPatch.DoPatching();
            //add any other startup shenanigins?
            Log.Message("SOS2ExpSpinal static constructor");
        }
    }

    public class SOS2ExpSpinal_HarmonyPatch
    {
        public static void DoPatching()
        {
            Log.Message("SOS2EXPSpinalEngines DoPatching called");
            var harmony = new Harmony("TheCafFiend.SOS2ExpSpinal.patch"); //Not 100% sure but think this is just namespace stuff?
            harmony.PatchAll();
        }
    }
    [HarmonyPatch(typeof(CompEngineTrail), nameof(CompEngineTrail.Thrust), MethodType.Getter)]
    public class PatchEngineThrust
    {
        public static void PostFix(ref int __result, CompEngineTrail __instance)
        {
            int AmplifierCount = -1;
            Log.Message("SOS2ExpSpinalEngine PostFix from PatchEngineThrust called");
            if (null != __result) // VS says this is not required; So many null errors in RW mods I'm paranoid
            {
                if ("Ship_Engine_Spinal" == __instance.parent.def.defName) // This feels filthy I bet there is a better way TODO
                {
                    
                    float AmplifierBonus = 0;
                    CompSpinalMount spinalComp = __instance.parent.TryGetComp<CompSpinalMount>();
                    if (null == spinalComp)
                    {
                        Log.Message("spinalcomp null");
                        return;
                    }
                    float ampBoost = 0;
                    bool foundNonAmp = false;
                    Thing amp = __instance.parent;
                    IntVec3 previousThingPos;
                    IntVec3 vec;
                    Rot4 currRotation;
                    if (0 == __instance.parent.Rotation.AsByte) // Engine and turret rotation are inverted, changed all from ShipTurret
                    {
                        vec = new IntVec3(0, 0, -1);
                    }
                    else if (1 == __instance.parent.Rotation.AsByte)
                    {
                        vec = new IntVec3(-1, 0, 0);
                    }
                    else if (2 == __instance.parent.Rotation.AsByte)
                    {
                        vec = new IntVec3(0, 0, 1);
                    }
                    else
                    {
                        vec = new IntVec3(1, 0, 0);
                    }
                    Log.Message(string.Format("found rotation, iterating, vec is: {0}", vec));
                    previousThingPos = amp.Position - vec; //Important switch for engine direction
                    previousThingPos -= vec; // engines are beeg
                    do
                    {
                        previousThingPos -= vec;
                        amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(__instance.parent.Map);
                        CompSpinalMount ampComp = amp.TryGetComp<CompSpinalMount>(); //Should this be post-null-check?
                                                                                     // Log.Message(string.Format("vecs are: amp, {0} Previous {1}", amp.Position, previousThingPos));
                        if ("Ship_Engine_Spinal" == __instance.parent.def.defName) //Engines are indeed different directions than weapons, ugly? workaround
                        {
                            currRotation = amp.Rotation.Opposite;
                        }
                        else
                        {
                            currRotation = amp.Rotation;
                        }
                        Log.Message(string.Format("Looking for stackEnd results at {0}, result of: {1}", ampComp, ampComp.Props.stackEnd));
                        if (null == amp || currRotation != __instance.parent.Rotation)
                        {
                            Log.Message("amps rotation did not match parent rotation");
                            AmplifierCount = -1;
                            break;
                        }
                        Log.Message("Amp not null");
                        if (amp.Position == previousThingPos)
                        {
                            AmplifierCount += 1;
                            Log.Message(string.Format("amp found, ampcount is {0}", AmplifierCount));
                            ampBoost += ampComp.Props.ampAmount;
                            // ampComp.SetColor(spinalComp.Props.color); TODO, fix this for-realsies
                        }
                        //found emitter
                        else if (amp.Position == previousThingPos - vec && ampComp.Props.stackEnd)
                        {
                            AmplifierCount += 1;
                            foundNonAmp = true;
                        }
                        //found unaligned
                        else
                        {
                            Log.Message(string.Format("amp.position: {0}, previouspos minus vec: {1} ", amp.Position, previousThingPos - vec));
                            Log.Message("Found unaligned");
                            AmplifierCount = -1;
                            foundNonAmp = true;
                        }
                    } while (!foundNonAmp);
                    Log.Message(string.Format("Found amps on spinal engine: {0}, boost: {1}", AmplifierCount, ampBoost));

                    if (0 > ampBoost)
                    {
                        AmplifierBonus = ampBoost;
                    }
                    __result = (int)(__instance.Props.thrust * AmplifierBonus);

                }
            }
        }

    }
}



