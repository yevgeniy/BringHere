using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;


namespace BringHere
{
    public class WorkGiver_BringHere : WorkGiver_Scanner
    {
        private static MethodInfo tryFindSpotToPlaceHaulableCloseTo 
            = typeof(HaulAIUtility).GetMethod("TryFindSpotToPlaceHaulableCloseTo", BindingFlags.NonPublic | BindingFlags.Static);

        private static Type jobDefType = typeof(BringDriver);
        private static HashSet<Thing> _neededItems = new HashSet<Thing>();
        public override PathEndMode PathEndMode => PathEndMode.OnCell;
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var requests = pawn.Map.GetComponent<BringHereManager>().GetActiveBringRequests();
            if (def.workType.defName == "BringHereUrgently")
                return requests.Where(v => v.BringUrgently);

            return requests;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is BringRequest bringRequest && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
            {

                var thing = FindThingToBring(bringRequest, pawn);
                if (thing!=null)
                {
                    int countLeft = CountLeftForItem(bringRequest, pawn, thing);
                    var stackCountForJob = Mathf.Min(thing.stackCount, countLeft);
                    if (stackCountForJob>0)
                    {
                        var methparams = new object[] { t, pawn, bringRequest.Position, null };
                        tryFindSpotToPlaceHaulableCloseTo.Invoke(null, methparams);

                        var haulToSpot = (IntVec3)methparams[3];

                        var jobDef= new JobDef
                        {
                            driverClass = jobDefType
                        };
                        var job = JobMaker.MakeJob(jobDef, thing, haulToSpot);

                        job.count = stackCountForJob;
                        job.haulOpportunisticDuplicates = false;
                        job.haulMode = HaulMode.ToCellNonStorage;
                        job.ignoreDesignations = true;
                        job.targetC = bringRequest;

                        return job;

                    }
                }
            }
            return null;

        }

        private int CountLeftForItem(BringRequest bringRequest, Pawn pawn, Thing thing)
        {
            var bringItemEntry = BringHereManager.GetTransferable(bringRequest, thing);
            if (bringItemEntry == null)
                return 0;
            return CountLeftToPack(bringRequest, pawn, bringItemEntry);
        }

        private int CountLeftToPack(BringRequest bringRequest, Pawn pawn, BringItemEntry bringItemEntry)
        {
            if (bringItemEntry.StillNeeded <= 0 )
                return 0;
            return Mathf.Max(bringItemEntry.StillNeeded - ItemsCountHauledByOthers(bringRequest.Map, pawn, bringItemEntry), 0);
        }

        private Thing FindThingToBring(BringRequest bringRequest, Pawn pawn)
        {
            var neededItems = new List<Thing>();
            foreach(var itemEntry in bringRequest.Items)
            {
                int countLeftToBring = CountLeftToBring(bringRequest, pawn, itemEntry);
                if (countLeftToBring>0)
                {
                    foreach(var thing in itemEntry.Things)
                    {
                        neededItems.Add(thing);
                    }
                }
            }
            if (!neededItems.Any())
                return null;

            Thing result = ClosestHaulable(pawn, ThingRequestGroup.Pawn, neededItems) ?? ClosestHaulable(pawn, ThingRequestGroup.HaulableEver, neededItems);
            return result;
            
        }

        private Thing ClosestHaulable(Pawn pawn, ThingRequestGroup thingRequestGroup, List<Thing> neededItems)
        {
            return GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(thingRequestGroup),
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                validator: (Thing thing) => neededItems.Contains(thing) 
                    && pawn.CanReserve(thing) 
                    && !thing.IsForbidden(pawn.Faction)
                    && !BringHereManager.IsDontHaul(thing) 
            );
        }

        private int CountLeftToBring(BringRequest bringRequest, Pawn pawn, BringItemEntry item)
        {
            return Mathf.Max(item.StillNeeded - ItemsCountHauledByOthers(bringRequest.Map, pawn, item), 0);
        }

        private List<Pawn>SpawnedColonyMechs()
        {
            var mapPawns = Find.CurrentMap.mapPawns;

            List<Pawn> pawns = new List<Pawn>();
            foreach (Pawn pawn in mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn.IsColonyMech)
                {
                    pawns.Add(pawn);
                }
                else if (pawn.IsColonyMechPlayerControlled)
                {
                    pawns.Add(pawn);
                }
            }
            return pawns;
        }

        private int ItemsCountHauledByOthers(Map map, Pawn pawn, BringItemEntry bringItemEntry)
        {
            var mechCount = ModsConfig.BiotechActive
                ? HauledByOthers(pawn, bringItemEntry, SpawnedColonyMechs())
                : 0;
            var slaveCount = ModsConfig.IdeologyActive
                ? HauledByOthers(pawn, bringItemEntry, map.mapPawns.SlavesOfColonySpawned)
                : 0;
            var colsCount = HauledByOthers(pawn, bringItemEntry, map.mapPawns.FreeColonistsSpawned);
            return mechCount + slaveCount + colsCount;
        }

        private int HauledByOthers(Pawn pawn, BringItemEntry bringItemEntry, List<Pawn> pawns)
        {
            var count = 0;
            foreach(var spawnedPawn in pawns)
            {
                if (spawnedPawn == pawn)
                    continue;

                if (spawnedPawn.CurJob!=null && spawnedPawn.CurJob.def.driverClass == jobDefType)
                {
                    if (spawnedPawn.jobs.curDriver is BringDriver driver)
                    {
                        var bringing = driver.Item;
                        if (bringing!=null && bringItemEntry.Things.Select(v=>v.def.defName).Contains(bringing.def.defName) )
                            count += bringing.stackCount;
                        
                    }
                }
            }
            return count;
        }
    }
}
