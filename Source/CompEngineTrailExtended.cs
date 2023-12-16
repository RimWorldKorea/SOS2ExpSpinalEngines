using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using RimworldMod;
using UnityEngine;

namespace RimWorld
{
    public class CompEngineTrailExtended : CompEngineTrail
    {
        public int AmplifierCount = -1;
        public float AmplifierBonus = 0;
        public CompSpinalMount spinalComp;
        int size;
        public void SpinalRecalc() //shamelessly stolen from Building_ShipTurret, then modified
        {
            if (spinalComp == null)
            {
                Log.Message("spinalcomp null");
                return;
            }
            AmplifierCount = -1;
            float ampBoost = 0;
            bool foundNonAmp = false;
            Thing amp = parent;
            IntVec3 previousThingPos;
            IntVec3 vec;
            Rot4 currRotation;
            if (parent.Rotation.AsByte == 0) // Engine and turret rotation are inverted
            {
                vec = new IntVec3(0, 0, -1);
            }
            else if (parent.Rotation.AsByte == 1)
            {
                vec = new IntVec3(-1, 0, 0);
            }
            else if (parent.Rotation.AsByte == 2)
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
                amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(parent.Map);
                CompSpinalMount ampComp = amp.TryGetComp<CompSpinalMount>(); //Should this be post-null-check?
                // Log.Message(string.Format("vecs are: amp, {0} Previous {1}", amp.Position, previousThingPos));
                if (parent.def.defName == "Ship_Engine_Spinal") //Engines are indeed different directions than weapons, ugly? workaround
                {
                    currRotation = amp.Rotation.Opposite;
                }
                else
                {
                    currRotation = amp.Rotation;
                }
                Log.Message(string.Format("Looking for stackEnd results at {0}, result of: {1}", ampComp, ampComp.Props.stackEnd));
                if (amp == null || currRotation != parent.Rotation)
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

            if (ampBoost > 0)
            {
                AmplifierBonus = ampBoost;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad) //If this was virtual in CompET I'd have just added to it, but I don't think? I can otherwise
        {
            base.PostSpawnSetup(respawningAfterLoad);
            flickComp = parent.TryGetComp<CompFlickable>();
            refuelComp = parent.TryGetComp<CompRefuelable>();
            powerComp = parent.TryGetComp<CompPowerTrader>();
            mapComp = parent.Map.GetComponent<ShipHeatMapComp>();
            size = parent.def.size.x;
            if (Props.reactionless)
                return;
            ExhaustArea.Clear();
            CellRect rectToKill;
            if (size > 3)
                rectToKill = parent.OccupiedRect().MovedBy(killOffsetL[parent.Rotation.AsInt]).ExpandedBy(2);
            else
                rectToKill = parent.OccupiedRect().MovedBy(killOffset[parent.Rotation.AsInt]).ExpandedBy(1);
            if (parent.Rotation.IsHorizontal)
                rectToKill.Width = rectToKill.Width * 2 - 3;
            else
                rectToKill.Height = rectToKill.Height * 2 - 3;
            foreach (IntVec3 v in rectToKill.Where(v => v.InBounds(parent.Map)))
            {
                ExhaustArea.Add(v);
            }
            spinalComp = parent.TryGetComp<CompSpinalMount>();
            Log.Message("Checking if item type is spinal");
            if (parent.def.defName == "Ship_Engine_Spinal")
            {
                Log.Message("Item type was spinal");
                SpinalRecalc();
            }
        }
        //Removed for now, initialize runs when map is not present
        /*public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            spinalComp = parent.TryGetComp<CompSpinalMount>();
            Log.Message("Checking if item type is spinal");
            if (parent.def.defName == "Ship_Engine_Spinal")
            {
                Log.Message("Item type was spinal");
                SpinalRecalc();
            }
        }*/

        public override int Thrust
        {
            get
            {
                Log.Message("asked for thrust from an engine");
                Log.Message(string.Format("parent: {0}, ampbonus: {1}, propsthrust: {2}", parent, AmplifierBonus, Props.thrust));
                // Somehow this is not called on placing an engine, even though it seems? it should on soshipcache
                if (parent.def.defName == "Ship_Engine_Spinal")
                {
                    //Not technically perfectly accurate :think:
                    return (int)(Props.thrust * AmplifierBonus);
                }
                else 
                {
                    return (int)Props.thrust;
                }
                
            }
        }

        public override void CompTick() // Still not sure there is a less-dumb way than replicating this whole thing
        {
            base.CompTick();
            if (active && !Props.reactionless) //destroy stuff in plume
            {
                if (refuelComp != null && Find.TickManager.TicksGame % 60 == 0)
                {
                    refuelComp.ConsumeFuel((Props.fuelUse) * AmplifierBonus);
                }
                HashSet<Thing> toBurn = new HashSet<Thing>();
                foreach (IntVec3 cell in ExhaustArea)
                {
                    foreach (Thing t in cell.GetThingList(parent.Map))
                    {
                        if ((t.def.useHitPoints || t is Pawn) && t.def.altitudeLayer != AltitudeLayer.Terrain)
                            toBurn.Add(t);
                    }
                }
                foreach (Thing t in toBurn)
                {
                    t.TakeDamage(new DamageInfo(DamageDefOf.Bomb, 100));
                }
            }
        }
    }
}


