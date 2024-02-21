using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BringHere
{
    public class ItemsTab : ITab
    {
        protected const float TopPadding = 20f;
        protected const float ThingIconSize = 28f;
        protected const float ThingRowHeight = 38f;
        protected const float ThingDropButotnSize = 24f;
        protected const float ThingLeftX = 36f;
        protected const float StandardLineHeight = 22f;
        protected Vector2 scrollPosition = Vector2.zero;
        protected float scrollViewHeight;

        public static readonly Color ThingLabelColor = new Color(.9f, .9f, .9f, 1f);
        public static readonly Color HighlightColor = new Color(.5f, .5f, .5f, 1f);

        protected static List<BringItemEntry> workingInvList = new List<BringItemEntry>();

        private List<BringItemEntry> Items { get
            {
                if (SelThing is BringRequest bringRequest)
                {
                    return bringRequest.Items;
                }
                return new List<BringItemEntry>();
            } } 

        public ItemsTab()
        {
            size = new Vector2(400f, 480f);
            labelKey = "NIMM_RequestedItems";
            tutorTag = "Still needed";
        }
        
        public override bool IsVisible => true;

        protected override void FillTab()
        {
            var curText = Text.Font;
            var curColor = GUI.color;

            Text.Font = GameFont.Small;
            var rect = new Rect(0f, TopPadding, size.x, size.y - TopPadding);
            var rect2 = rect.ContractedBy(10f);
            var position = new Rect(rect2.x, rect2.y, rect2.width, rect2.height);

            GUI.color = Color.white;
            var outRect = new Rect(0f, 0f, position.width, position.height);
            var viewRect = new Rect(0f, 0f, position.width-16f, scrollViewHeight);

            Widgets.BeginGroup(position);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);

            var curY = 0f;

            if (IsVisible)
            {
                Widgets.ListSeparator(ref curY, viewRect.width, "Needed");
                workingInvList.Clear();
                workingInvList.AddRange(Items);
                foreach (var t in workingInvList)
                {
                    DrawEntryRow(ref curY, viewRect.width, t);
                }
                workingInvList.Clear();
            }
            
            if (Event.current.type is EventType.Layout)
            {
                scrollViewHeight = curY + 30f;
            }

            Widgets.EndScrollView();
            Widgets.EndGroup();


            Text.Font = curText;
            GUI.color = curColor;
        }


        private void DrawEntryRow(ref float y, float width, BringItemEntry t)
        {
            var curText = Text.Font;
            var curColor = GUI.color;
            var textAnchor = Text.Anchor;
            var wordWrap = Text.WordWrap;

            var thing = t.ExampleThing;
            var stillneeded = t.StillNeeded;

            Rect rect = new Rect(0f, y, width, ThingIconSize);
            Widgets.InfoCardButton(rect.width - 24f, y, thing);
            rect.width -= 24f;

            var rect2 = rect;
            rect2.xMin = rect2.xMax - 60f;
            rect.width -= 60f;


            if (Mouse.IsOver(rect))
            {
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (!(thing.def.DrawMatSingle is null) && !(thing.def.DrawMatSingle.mainTexture is null))
            {
                Widgets.ThingIcon(new Rect(4f, y, ThingIconSize, ThingRowHeight), thing, 1f);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            var rect3 = new Rect(ThingLeftX, y, rect.width - ThingLeftX, rect.height);
            var text = string.Empty;
            text = thing.LabelNoCount + " x" + stillneeded;
            
            Text.WordWrap = false;
            Widgets.Label(rect3, text.Truncate(rect3.width, null));
            Text.WordWrap = true;

            var text2 = thing.DescriptionDetailed;
            
            TooltipHandler.TipRegion(rect, text2);
            y += ThingRowHeight;

            Text.Font = curText;
            GUI.color = curColor;
            Text.Anchor = textAnchor;
            Text.WordWrap = wordWrap;
        }

    }
}
