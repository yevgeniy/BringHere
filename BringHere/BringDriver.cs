using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace BringHere
{
    public class BringDriver: JobDriver_HaulToCell
    {
      
        public Thing Item { get { return TargetA.Thing; } }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var hauling = job.count;
            Log.Message("HAULING: " + Item.def.defName + " " + job.count);

            /*Update entry for pawn to item line */
            var p = pawn;
            var itemHash = BringItemEntry.ToHash(Item);
            var bringRequest = TargetC.Thing as BringRequest;
            var bringRequestEntry = bringRequest.Items.FirstOrDefault(v => v.Hash == itemHash);

            Action remover = delegate { };
            if (bringRequestEntry != null)
            {
                remover=bringRequestEntry.AddPawnItemLineDrawer(() =>
                {
                    GenDraw.DrawLineBetween(bringRequest.DrawPos, Item.DrawPos, SimpleColor.White);
                    GenDraw.DrawLineBetween(Item.DrawPos, p.DrawPos, SimpleColor.Green);
                });
                AddFinishAction(() =>
                {
                    remover();
                });
            }
            
            foreach (var t in base.MakeNewToils())
            {
                if (bringRequestEntry != null && t.debugName == "StartCarryThing")
                    t.AddPreInitAction(() =>
                    {
                        remover();
                        remover= bringRequestEntry.AddPawnItemLineDrawer(() =>
                        {
                            GenDraw.DrawLineBetween(bringRequest.DrawPos, p.DrawPos, SimpleColor.Green);
                        });
                    });

                yield return t;
            }
                

            
            var endtoil = ToilMaker.MakeToil("adjust-bring-here-still-needed");

            endtoil.initAction=() =>
            {
                Log.Message("FINISHED: " + hauling);
                BringHereManager.RegisterDelivered(TargetC.Thing as BringRequest, TargetA.Thing, hauling);
            };


            yield return endtoil;

            yield return Toils_General.Wait(5, TargetIndex.None);

        }


    }
}
