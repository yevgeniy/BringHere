using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Mono.Cecil.Cil;
using RimWorld;
using BringHere;
using Verse;
using Verse.AI;
using UnityEngine;



namespace RimWorld
{
    public class Patch_AllowTool
    {
        public static void Wire(Harmony harmony)
        {
            // Get all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Get all types from all assemblies and group them by namespace
            var classType = assemblies.SelectMany(assembly => assembly.GetTypes())
                    .First(v => v.Name == "Designator_HaulUrgently");
            if (classType == null)
                return;
            var meth = classType.GetMethod("ThingIsRelevant", BindingFlags.NonPublic | BindingFlags.Instance);

            harmony.Patch(meth, prefix: new HarmonyMethod(typeof(Patch_AllowTool), nameof(Prefixer)));
        }

        public static bool Prefixer(Thing thing, ref bool __result)
        {

            if (!thing.Spawned)
            {
                __result = false;
                return false;
            }

            return true;

                
        }


    }


    [HarmonyPatch(typeof(Thing), "DrawGUIOverlay")]
    public class foobarwas
    {
        
        public static bool Prefix(Thing __instance)
        {
            

            if (BringHereManager.IsDontHaul(__instance))
            {
                var vect = __instance.Position.ToVector3();
                vect.x += .5f;
                vect.z += .5f;
                GenMapUI.DrawText(new Vector2(vect.x, vect.z), "O", Color.green);

                //var vect2 = vect.MapToUIPosition();

                //var rect = new Rect(vect2, new Vector2(65f, 65f));
                //Widgets.DrawTextureFitted(rect, BringHereManager.UseDontHaulIcon, .3f);
            }

            

            //Texture badTex = this.icon;

            //if (badTex == null)
            //{
            //    badTex = BaseContent.BadTex;
            //}


            //if (!this.disabled || parms.lowLight)
            //{
            //    GUI.color = this.IconDrawColor;
            //}
            //else
            //{
            //    GUI.color = this.IconDrawColor.SaturationChanged(0f);
            //}
            //if (parms.lowLight)
            //{
            //    GUI.color = GUI.color.ToTransparent(0.6f);
            //}
            
            
            
            //GUI.color = Color.white;
            return true;
        }
    }

    /* When item is currently marked as not to be hauled, if a user
    * selects and clicks 'f' on the item it should reset this item back to
    * default haulable state */
    [HarmonyPatch(typeof(CompForbiddable), "CompGetGizmosExtra")]
    public class BringHerePatches
    {
        
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompForbiddable __instance)
        {
            foreach (var r in __result)
            {
                if (r!=null && r is Command_Toggle commandToggle)
                {
                    if (!__instance.parent.def.IsDoor)
                    {
                        commandToggle.toggleAction = () =>
                        {
                            if (BringHereManager.IsDontHaul(__instance.parent))
                            {
                                BringHereManager.RemoveDontHaul(__instance.parent);
                            } else
                            {
                                __instance.Forbidden = !__instance.Forbidden;
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Forbidding, KnowledgeAmount.SpecificInteraction);
                            }
                        };
                        
                    }
                }
                yield return r;
            }
                
        }

    }

    [HarmonyPatch(typeof(StoreUtility), "IsInValidStorage")]
    public class StoreUtility_IsInValidStorage
    {
        public static bool Prefix(Thing t, ref bool __result)
        {
            if (BringHereManager.IsDontHaul(t))
            {
                //Log.Message("verriding haulable in: " + from);
                __result = true;
                return false;
            }
            return true;

        }
    }

    /* In haul util all evaluators of haulable item should consider if item is marked
     * not to be hauled */
    public class StoreUtilityPatch
    {
        public static bool Prefixer(Thing t, string from, ref bool __result)
        {
            if (BringHereManager.IsDontHaul(t))
            {
                //Log.Message("verriding haulable in: " + from);
                __result = false;
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(StoreUtility), "TryFindBestBetterNonSlotGroupStorageFor")]
    public class StoreUtility_TryFindBestBetterNonSlotGroupStorageFor
    {
        [HarmonyPrefix]
        public static bool Prefix(Thing t, ref bool __result)
        {
            return StoreUtilityPatch.Prefixer(t, "TryFindBestBetterNonSlotGroupStorageFor", ref __result);
        }
    }

    [HarmonyPatch(typeof(StoreUtility), "TryFindBestBetterStorageFor")]
    public class StoreUtility_TryFindBestBetterStorageFor
    {
        [HarmonyPrefix]
        public static bool Prefix(Thing t, ref bool __result)
        {
            return StoreUtilityPatch.Prefixer(t, "TryFindBestBetterStorageFor", ref __result);
        }
    }
    [HarmonyPatch(typeof(StoreUtility), "TryFindBestBetterStoreCellFor")]
    public class StoreUtility_TryFindBestBetterStoreCellFor
    {
        [HarmonyPrefix]
        public static bool Prefix(Thing t, ref bool __result)
        {
            return StoreUtilityPatch.Prefixer(t, "TryFindBestBetterStoreCellFor", ref __result);
        }
    }
    [HarmonyPatch(typeof(StoreUtility), "TryFindStoreCellNearColonyDesperate")]
    public class StoreUtility_TryFindStoreCellNearColonyDesperate
    {
        [HarmonyPrefix]
        public static bool Prefix(Thing item, ref bool __result)
        {
            return StoreUtilityPatch.Prefixer(item, "TryFindStoreCellNearColonyDesperate", ref __result);
        }
    }


    /* Ctrl+Right clicking on ground should bring up bring menu */
    [HarmonyPatch(typeof(MainButtonsRoot), "MainButtonsOnGUI")]
    public class MainButtonRoot_MainButtonOnGUI
    {
        public static bool Prefix()
        {

            switch(Event.current.type)
            {
                case EventType.MouseDown:
                    if (Event.current.button==1 && Event.current.control && !DialogBringItems.IsActive)
                        BringHereManager.NewBringRequest();    
                    break;
                case EventType.KeyUp:
                    if (Event.current.keyCode==KeyCode.Return || Event.current.keyCode==KeyCode.KeypadEnter)
                    {
                        BringHereManager.ProcessKey(KeyCode.Return);
                    }
                    break;
                        
            }
            
            return true;

        }
    }
}



#region example

/*
namespace Verse.AI
{

    public static class ToilFailConditionsExtensions
    {
        public static JobDriver_HaulToCell FailOnNotHaulable(this JobDriver_HaulToCell f, TargetIndex ind)
        {
            Log.Message("IT WORKS IN JobDriver_HaulToCell!");
            f.AddEndCondition(() =>
            {
                Pawn actor = f.GetActor();

                Thing thing = actor.jobs.curJob.GetTarget(ind).Thing;
                if (BringHereManager.IsDontHaul(thing) && ForbidUtility.CaresAboutForbidden(actor, false))
                {
                    return JobCondition.Incompletable;
                }

                return JobCondition.Ongoing;
            });
            return f;
        }
        public static Toil ToilFailOnNotHaulable(this Toil f, TargetIndex ind)
        {
            f.AddEndCondition(() =>
            {
                Pawn actor = f.GetActor();

                Thing thing = actor.jobs.curJob.GetTarget(ind).Thing;
                if (BringHereManager.IsDontHaul(thing) && ForbidUtility.CaresAboutForbidden(actor, false))
                {
                    return JobCondition.Incompletable;
                }

                return JobCondition.Ongoing;
            });
            return f;
        }


    }

    [HarmonyPatch(typeof(JobDriver_HaulToCell), MethodType.Enumerator)]
    [HarmonyPatch("MakeNewToils")]
    public static class JobDriver_HaulToCell_MakeNewToils
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            var fondForbid = false;
            var foundPop = false;
            var lines = instructions.ToList();
            var insertAt = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var instruction = lines[i];

                if (!fondForbid)
                {
                    if (instruction.opcode == OpCodes.Call
                        && instruction.operand is MethodInfo methodInfo
                        && methodInfo.DeclaringType == typeof(ToilFailConditions)
                        && methodInfo.Name == "FailOnForbidden")
                    {
                        fondForbid = true;
                    }
                }
                else if (!foundPop)
                {
                    if (instruction.opcode == OpCodes.Pop)
                    {
                        foundPop = true;
                        insertAt = i + 1;
                        break;
                    }
                }

            }


            lines.InsertRange(insertAt, new List<CodeInstruction>()
            {
                
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(ToilFailConditionsExtensions),
                        nameof(ToilFailConditionsExtensions.FailOnNotHaulable)
                    )
                ),
                new CodeInstruction(OpCodes.Pop)
            });

            foreach (var instruction in lines)
            {
                yield return instruction;
            }
        }

    }


}

namespace PickUpAndHaulPatch
{
    
    public static class JobDriver_HaulToInventory_Patcher
    {
        public static void Wire(Harmony harmony)
        {
            try
            {
                ((Action)(() =>
                {
                    if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Pick Up And Haul"))
                    {
                        Log.Message("HAS PICKUPANDHAUL");

                        Assembly otherModAssembly = AppDomain.CurrentDomain.GetAssemblies()
                            .FirstOrDefault(assembly => assembly.FullName.Contains("PickUpAndHaul"));
                        var otherModClassType = otherModAssembly?.GetType("PickUpAndHaul.JobDriver_HaulToInventory");
                        var meth = otherModClassType?.GetMethod("MakeNewToils");

                        harmony.Patch(
                            AccessTools.EnumeratorMoveNext(meth),
                            transpiler: new HarmonyMethod(typeof(JobDriver_HaulToInventory_Patcher), nameof(Transpile))
                        );
                            
                    } else
                    {
                        Log.Message("DOES NOT HAVE!");
                    }
                }))();
            }
            catch (TypeLoadException ex) {
                Log.Message("DOES NOT HAVE ERROR");
            }
        }

        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            Log.Message("LINES:");

            var lines = instructions.ToList();
            var startAt = -1;
            for(var i=0; i<lines.Count;i++)
            {
                var line = lines[i];
                if (line.ToString().Contains("FailOnDespawnedNullOrForbidden"))
                {
                    startAt = i + 2;
                    break;
                }
            }

            lines.InsertRange(startAt, new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(ToilFailConditionsExtensions),
                        nameof(ToilFailConditionsExtensions.ToilFailOnNotHaulable)
                    )
                ),
                new CodeInstruction(OpCodes.Pop)
            });

            

            foreach (var i in lines)
            {
                Log.Message(i.ToString());
                yield return i;
            }

        }
    }

}
*/
#endregion