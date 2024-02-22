using HarmonyLib;
using KTrie;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BringHere
{
    [StaticConstructorOnStartup]
    public class BringHereManager : MapComponent
    {
        #region static
        private static List<BringRequest> bringRequests = new List<BringRequest>();
        private static HashSet<Thing> dontHaulRepository = new HashSet<Thing>();
        public static Material DontHaulMat;
        public static Texture2D UseDontHaulIcon;

        [TweakValue("X" )]
        public static float x = 0f;
        [TweakValue("Y")]
        public static float y = 0f;

        public static BringItemEntry GetTransferable(BringRequest bringRequest, Thing thing)
        {

            var thingName = thing.def.defName;
            return bringRequest.Items.First(v => v.Things.Select(vv => vv.def.defName).Contains(thingName));

        }

        public static void RegisterDelivered(BringRequest bringRequest, Thing item, int count)
        {
            var bringItemEntry = bringRequest.Items.Where(v=>v.Things.Select(vv=>vv.def.defName).ToList().Contains(item.def.defName)).FirstOrDefault();
            if (bringItemEntry == null)
                Log.Warning("Brought an item but can't find it in bringRequest.");

            AddDontHaul(item);

            bringItemEntry.StillNeeded -= count;
            if (bringItemEntry.StillNeeded <= 0)
                bringRequest.Items.Remove(bringItemEntry);

            if (bringRequest.Items.Count == 0)
            {
                RemoveRequest(bringRequest);
            }
        }

        public static void AddDontHaul(Thing item)
        {
            
            dontHaulRepository.Add(item);
            
            


            /*clean repo of despawned items */
            var toRemove = dontHaulRepository.Where(v => !v.Spawned);
            foreach(var i in  toRemove)
                dontHaulRepository.Remove(i);

        }
        public static void RemoveDontHaul(Thing thing)
        {
            dontHaulRepository.Remove(thing);
        }

        public static void RemoveRequest(BringRequest bringRequest)
        {
            bringRequests.Remove(bringRequest);
            bringRequest.Destroy();
        }

        public static BringRequest AddRequest(IntVec3 cell, List<TransferableOneWay> items)
        {
            var bringRequestDef = new BringRequestDef();
            var bringRequest = GenSpawn.Spawn(bringRequestDef, cell, Find.CurrentMap) as BringRequest;
            bringRequest.Items = items.Where(v => v.HasAnyThing).Select(v =>
            {
                var hash = BringItemEntry.ToHash(v.things.First());

                var exampleThing = v.things.First();

                /* example thing should be the closest item matching the item hash */
                return new BringItemEntry
                {
                    Things = v.things,
                    StillNeeded = v.CountToTransfer,
                    Hash = hash,
                    ExampleThing = exampleThing,
                };
            }).ToList();

            Log.Message("ADDED");
            bringRequests.Add(bringRequest);

            return bringRequest;

            
        }

        public static bool IsDontHaul(Thing t)
        {
            return dontHaulRepository.Contains(t);
        }


        #endregion

        static BringHereManager()
        {
            UseDontHaulIcon = ContentFinder<Texture2D>.Get("useDontHaul");
            DontHaulMat= MaterialPool.MatFrom("useDontHaul", ShaderDatabase.MetaOverlay);
        }
        public BringHereManager(Map map) : base(map)
        {
            
        }
        private static DialogBringItems currentDialog = null;

        public static async void NewBringRequest()
        {
            //var map = Find.CurrentMap.GetComponent<BringHereManager>();
            var cell = Verse.UI.MouseCell();
            var items=await DialogBringItems.Show(cell, ref currentDialog);

            if (items == null)
                return;

            var bringrequest=AddRequest(cell, items );
            currentDialog = null;

        }
        public IEnumerable<BringRequest> GetActiveBringRequests()
        {
            return bringRequests.Where(v => v.Map.uniqueID == map.uniqueID);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref bringRequests, "nimm-bring-here", LookMode.Reference, LookMode.Deep);
        }

        public static void ProcessKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Return:
                    if (currentDialog != null)
                        currentDialog.DoSubmit();
                    break;
            }
            
        }
    }

   
}
