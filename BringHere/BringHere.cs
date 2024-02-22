using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BringHere
{
    [StaticConstructorOnStartup]
    public static class BringHere
    {
        public static bool HasAllowTool;

        static BringHere()
        {
            Log.Message("BRING STARTED.");

            Harmony.DEBUG = true;  // Enable Harmony Debug
            Harmony harmony = new Harmony("nimm.bringhere");

            Patch_AllowTool.Wire(harmony);


            harmony.PatchAll();

            Log.Message("BRING PATCHED.");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            /* find 'haul urgently' class */
            var classType = assemblies.SelectMany(assembly => assembly.GetTypes())
                    .FirstOrDefault(v => v.Name == "Designator_HaulUrgently");
            if (classType != null)
            {
                HasAllowTool = true;
            }
            Log.Message("HAS TOOL: " + HasAllowTool);
                
        }
    }


}