using KTrie;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BringHere
{
    public class BringItemEntry
    {
        public static int ToHash(Thing item)
        {
            var num = 0;
            num = Gen.HashCombine(num, item.def);
            num = Gen.HashCombine(num, item.Stuff);

            return num;
        }

        public List<Thing> Things { get; set; }

        public int Hash { get; set; }
        public string Name { get; set; }
        public int StillNeeded { get; set; }
        public Thing ExampleThing { get; set; }

        public List<Action> PawnItemLines = new List<Action> { };
        public Action AddPawnItemLineDrawer(Action act)
        {
            PawnItemLines.Add(act);
            return () =>
            {
                PawnItemLines.Remove(act);
            };
        }

    }
    public class BringRequest :Thing
    {
        public BringRequest() : base()
        {
            
        }
        public List<BringItemEntry> Items = new List<BringItemEntry>();
        public bool BringUrgently { get; set; }
   

        public override IEnumerable<Gizmo> GetGizmos()
        {
            var cancel = new Command_Action
            {
                defaultLabel = "Cancel",
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                hotKey = KeyBindingDefOf.Designator_Cancel,
                action = delegate ()
                {
                    BringHereManager.RemoveRequest(this);
                }
            };
            yield return cancel;

            if (BringHere.HasAllowTool)
            {

                yield return new Command_Action
                {
                    defaultLabel = BringUrgently ? "Take your time" : "Bring Urgently",
                    icon = ContentFinder<Texture2D>.Get(BringUrgently ? "dontHaulUrgently" : "haulUrgently"),
                    hotKey=KeyBindingDefOf.Command_ColonistDraft,
                    action = delegate {
                        BringUrgently = !BringUrgently;
                    }
                };
            }
        }

        public override IEnumerable<InspectTabBase> GetInspectTabs()
        {
            foreach (var i in base.GetInspectTabs())
                yield return i;

        }

        public override string GetInspectString()
        {
            return String.Join(", ", Items.Select(v => v.ExampleThing.LabelCapNoCount + " x" + v.StillNeeded));
        }


        public override void Draw()
        {
            var vect = this.Position.ToVector3();
            vect.x += .5f;
            vect.z += .5f;
            GenDraw.DrawTargetHighlight(new LocalTargetInfo(this.Position));
        }
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
        }
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();


            foreach (var bringEntry in Items)
            {

                /* line to pawn fetching item */
                foreach (var v in bringEntry.PawnItemLines) v();
            }
             
        }
        

    }
    
    public  class BringRequestDef:ThingDef
    {
        public BringRequestDef():base()
        {
            thingClass = typeof(BringRequest);
            selectable = true;
            inspectorTabs = new List<Type> { typeof(ItemsTab) };
            inspectorTabsResolved = new List<InspectTabBase> { InspectTabManager.GetSharedInstance(typeof(ItemsTab)) };

        }

    }
}
