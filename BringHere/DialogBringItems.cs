using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace BringHere
{
    public class DialogBringItems :Window
    {
        public static bool IsActive { get; internal set; }
        public static Task<List<TransferableOneWay>> Show(IntVec3 cell, ref DialogBringItems currentDialog)
        {
            var t = new TaskCompletionSource<List<TransferableOneWay>>();

            Find.WindowStack.Add(currentDialog=new DialogBringItems(
                cell, 
                onSubmit:(List<TransferableOneWay> stuff) =>
                {
                   t.TrySetResult(stuff);
                }
            ));

            return t.Task;

        }

        public DialogBringItems(IntVec3 cell, Action<List<TransferableOneWay>> onSubmit)
        {
            closeOnAccept=true;
            closeOnCancel = true;
            forcePause = true;
            absorbInputAroundWindow=true;
            _cell = cell;
            _onSubmit = onSubmit;
        }

        private List<TransferableOneWay> _transferables = new List<TransferableOneWay>();
        private TransferableOneWayWidget _transferWidget;
        private Action<List<TransferableOneWay>> _onSubmit;

        public override Vector2 InitialSize => new Vector2(1024, Verse.UI.screenHeight);

        private IntVec3 _cell;

        public override void PostOpen()
        {
            IsActive = true;
            DoReset();
            base.PostOpen();
            
        }
        public override void PostClose()
        {
            IsActive = false;
            base.PostClose();
        }



        public override void DoWindowContents(Rect inRect)
        {
            var rect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "Bring Here");

            inRect.yMin += 60;
            Widgets.DrawMenuSection(inRect);
            inRect = inRect.ContractedBy(17f);

            Widgets.BeginGroup(inRect);
            var bottomRect = inRect.AtZero();
            RenderBottomButtons(bottomRect);
            bottomRect.yMax -= 76f;

            _transferWidget.OnGUI(bottomRect, out bool didChange);
            
            Widgets.EndGroup();
        }

        private void RenderBottomButtons(Rect rect)
        {
            var buttonWidth = 150f;
            var buttonHeight = 40f;

            var buttonY = rect.height - buttonHeight - 2f;
            var acceptX = rect.width / 2 - buttonWidth / 2;

            var rect2 = new Rect(acceptX, buttonY, buttonWidth, buttonHeight+5);
            if (Widgets.ButtonText(rect2, "Accept", true, true, true))
            {
                DoSubmit();
            }


            Text.Font = GameFont.Small;

            var resetX = acceptX + buttonWidth + 27f;
            var rect3 = new Rect(resetX, buttonY + 5, buttonWidth, buttonHeight - 5);
            if (Widgets.ButtonText(rect3, "Reset", true, true, true))
            {
                Log.Message("RESET");
                DoReset();
            }

            var closeX = resetX + buttonWidth + 17f;
            var rect4 = new Rect(closeX, buttonY+5, buttonWidth, buttonHeight-5);
            if (Widgets.ButtonText(rect4, "Cancel", true, true, true))
            {
                DoClose();
            }
        }

        private void DoReset()
        {
            
            IdentifyTransferables();

            _transferWidget = new TransferableOneWayWidget(
                _transferables,
                null,
                null,
                null,
                false,
                ignorePawnInventoryMass: IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
                includePawnsMassInMassUsage: false,
                availableMassGetter: () => 99999f
            );
        }

        public void DoSubmit()
        {
            var selected = _transferables.Where(v => v.CountToTransfer > 0).ToList();
            _onSubmit(selected);
            DoClose();
        }

        private void DoClose()
        {
            _onSubmit(null);
            Close(true);
        }

        private void IdentifyTransferables()
        {
            _transferables.Clear();



            foreach (var item in CaravanFormingUtility.AllReachableColonyItems(Find.CurrentMap, true, false, false))
            {
                var tranEntry = TransferableUtility.TransferableMatching(item, _transferables, TransferAsOneMode.PodsOrCaravanPacking);
                if (tranEntry == null)
                {
                    tranEntry = new TransferableOneWay();
                    _transferables.Add(tranEntry);
                }
                /*check just in case something tried to parse same thing*/
                if (tranEntry.things.Contains(item))
                {
                    continue;
                }
                tranEntry.things.Add(item);
            }
        }
    }
}

//GenUI.TargetsAt()