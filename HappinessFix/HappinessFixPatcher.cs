using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
//using ColossalFramework;
//using ColossalFramework.Math;
using ColossalFramework.Plugins;
using HarmonyLib;

namespace HappinessFix
{
    public static class HappinessFixPatcher
    {
        private const string HarmonyId = "Infixo.HappinessFix";
        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) { Debug.Log("PatchAll: already patched!"); return; }
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (Harmony.HasAnyPatches(HarmonyId))
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, $"{HarmonyId} methods patched ok");
                patched = true;
                var myOriginalMethods = harmony.GetPatchedMethods();
                foreach (var method in myOriginalMethods)
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, $"{HarmonyId} ...method {method.Name}");
            }
            else
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, $"{HarmonyId} ERROR: methods not patched");
        }

        public static void UnpatchAll()
        {
            if (!patched) { Debug.Log("UnpatchAll: not patched!"); return; }
            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            patched = false;
        }
    }

    /* 
     * Old method that is easier to implement but technically less efficient
     * 
    
    [HarmonyPatch(typeof(CommonBuildingAI))]
    public static class CommonBuildingAIPatches
    {
        [HarmonyPatch("GetVisitBehaviour")]
        [HarmonyReversePatch]
        public static void GetVisitBehaviourReverse(object instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            Debug.Log("ERROR: GetVisitBehaviour reverse patch not applied");
        }
    }
    
    // protected int HandleWorkers(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveWorkerCount, ref int totalWorkerCount, ref int workPlaceCount)
    [HarmonyPatch(typeof(PrivateBuildingAI))]
    public static class PrivateBuildingAIPatches
    {
        [HarmonyPatch("HandleWorkers")]
        [HarmonyReversePatch]
        public static int HandleWorkersReverse(object instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveWorkerCount, ref int totalWorkerCount, ref int workPlaceCount)
        { 
            Debug.Log("ERROR: HandleWorkers reverse patch not applied");
            return 0;
        }

        //protected int HandleWorkers(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveWorkerCount, ref int totalWorkerCount, ref int workPlaceCount)
        [HarmonyPatch("HandleWorkers")]
        [HarmonyPostfix]
        // this method increases worker problem counter, so when called twice....
        public static int HandleWorkersPostfix(int __result, PrivateBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveWorkerCount, ref int totalWorkerCount, ref int workPlaceCount)
        {
            if (__instance is CommercialBuildingAI)
            {
                if ((buildingData.m_problems & Notification.Problem.NoWorkers) == Notification.Problem.NoWorkers && buildingData.m_workerProblemTimer > 0)
                    --buildingData.m_workerProblemTimer;
                if ((buildingData.m_problems & Notification.Problem.NoEducatedWorkers) == Notification.Problem.NoEducatedWorkers && buildingData.m_workerProblemTimer > 0)
                    --buildingData.m_workerProblemTimer;
            }
            return __result;
        }
    }

    [HarmonyPatch(typeof(CommercialBuildingAI))]
    public static class CommercialBuildingAIPatches
    {
        [HarmonyPatch("MaxIncomingLoadSize")]
        [HarmonyReversePatch]
        public static int MaxIncomingLoadSizeReverse(object instance)
        {
            Debug.Log("ERROR: MaxIncomingLoadSize reverse patch not applied");
            return 4000;
            //throw new NotImplementedException(message);
        }
        
        // patch for method that calculates happiness during simulation
        [HarmonyPatch("SimulationStepActive")]
        [HarmonyPostfix]
        public static void SimulationStepActivePostfix(CommercialBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            // district needed to fix the cumulative data
            DistrictManager instance = Singleton<DistrictManager>.instance;
            byte district = instance.GetDistrict(buildingData.m_position);

            int happiness = buildingData.m_happiness; // happiness calculated by the orginal method

            //num18 tells how many workers we have and is needed to reverse stockpile logic and apply new customer logic
            int aliveWorkerCount = 0;
            int totalWorkerCount = 0;
            int workPlaceCount = 0;
            Citizen.BehaviourData behaviour = default(Citizen.BehaviourData);
            int productionRate = PrivateBuildingAIPatches.HandleWorkersReverse(__instance, buildingID, ref buildingData, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount, ref workPlaceCount);
            int num18 = aliveWorkerCount * 20 / workPlaceCount;

            // first, we need to reverse the data stored on the City/Disctrict level
            // a bit ugly hack for now...
            // instance.m_districts.m_buffer[district].AddCommercialData(ref behaviour, num16, num17, crimeBuffer, workPlaceCount, aliveWorkerCount, Mathf.Max(0, workPlaceCount - totalWorkerCount), num3, aliveCount, num4, buildingData.m_level, electricityConsumption, heatingConsumption, waterConsumption, sewageAccumulation, garbageAccumulation, incomeAccumulation, Mathf.Min(100, (int)buildingData.m_garbageBuffer / 50), buildingData.m_waterPollution * 100 / 255, buildingData.m_finalImport, buildingData.m_finalExport, m_info.m_class.m_subService);
            //instance.m_districts.m_buffer[district].m_commercialData.m_tempHappiness -= (uint)(happiness * workPlaceCount);
            instance.m_districts.m_buffer[district].m_commercialData.m_tempHappiness -= (uint)(happiness * workPlaceCount);
            
            // remove the unnecessary part
            // num17 += num18 - Mathf.Min(num18, buildingData.m_customBuffer2 * num18 / num6);
            // num6 is needed to reverse stockpile logic
            int width = buildingData.Width;
            int length = buildingData.Length;
            int num2 = MaxIncomingLoadSizeReverse(__instance);
            int aliveCount = 0;
            int totalCount = 0;
            CommonBuildingAIPatches.GetVisitBehaviourReverse(__instance, buildingID, ref buildingData, ref behaviour, ref aliveCount, ref totalCount);
            int num3 = __instance.CalculateVisitplaceCount((ItemClass.Level)buildingData.m_level, new Randomizer(buildingID), width, length);
            int num5 = num3 * 500;
            int num6 = Mathf.Max(num5, num2 * 4);

            // reverse stockpile related happiness
            happiness -= num18 - Mathf.Min(num18, buildingData.m_customBuffer2 * num18 / num6);

            // use no of customers instead
            happiness += Mathf.Min(num18, num18 * aliveCount / num3);
            
            buildingData.m_happiness = (byte)Mathf.Clamp(happiness, 0, 100);

            // and at last we can fix the data stored on the City/Disctrict level
            // a bit ugly hack for now...
            //ref District 
            instance.m_districts.m_buffer[district].m_commercialData.m_tempHappiness += (uint)(happiness * workPlaceCount);
        }
    }
    *
    * End of OLD METHOD
    */

    /*
     * NEW METHOD USING TRANSPILER
     * here we change only 1 line of code that contains an original formula
     * 
    // happiness.48 += num18.49 - Mathf.Min(num18.49, buildingData.m_customBuffer2 * num18.49 / num6.19);
    -4 IL_0928: ldloc.s 48
    -3 IL_092a: ldloc.s 49
    -2 IL_092c: ldloc.s 49
    -1 IL_092e: ldarg.2
     0 IL_092f: ldfld uint16 Building::m_customBuffer2 // this is the 3rd occurence
     1 IL_0934: ldloc.s 49
     2 IL_0936: mul
     3 IL_0937: ldloc.s 19
     4 IL_0939: div
     5 IL_093a: call int32[UnityEngine]UnityEngine.Mathf::Min(int32, int32)
	 6 IL_093f: sub
     7 IL_0940: add
     8 IL_0941: stloc.s 48

    // new formula to replace the existing one
    happiness.48 += Mathf.Min(num18.49, num18.49 * aliveCount.14 / num3.16);
    -4 [-] ldloc.s 48 // for +=
    -3 [-] ldloc.s 49 // for Min
    -2 [-] ldloc.s 49
    -1 <nop>
     0 <nop>
     1 ldloc.s 14
     2 [-] mul
     3 ldloc.s 16
     4 div
     5 call int32[UnityEngine]UnityEngine.Mathf::Min(int32, int32)
     6 <nop>
     7 [-] add
     8 [-] stloc.s 48
*/
    [HarmonyPatch(typeof(CommercialBuildingAI))]
    [HarmonyPatch("SimulationStepActive")]
    public static class CommercialBuildingAI_SimulationStepActive_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int occurence = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                // find 3rd occurence of m_customBuffer2
                if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand == AccessTools.Field(typeof(Building), "m_customBuffer2"))
                {
                    //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, $"{i}: {codes[i].operand}");
                    ++occurence;
                    if (occurence == 3)
                    {
                        // new IL code here
                        // i-4, i-3, i-2 are the same
                        codes[i - 1] = new CodeInstruction(OpCodes.Nop);
                        codes[i    ] = new CodeInstruction(OpCodes.Nop);
                        codes[i + 1] = new CodeInstruction(OpCodes.Ldloc_S, 14);
                        codes[i + 2] = new CodeInstruction(OpCodes.Mul);
                        codes[i + 3] = new CodeInstruction(OpCodes.Ldloc_S, 16);
                        codes[i + 4] = new CodeInstruction(OpCodes.Div);
                        // codes[i + 5] = new CodeInstruction(); // call Matf.Min
                        codes[i + 6] = new CodeInstruction(OpCodes.Nop);
                        // i+7, i+8 are the same
                        break; // no need to go further with analysis
                    }
                }
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, $"<{i:X4}> {codes[i]}");
                //var x = new CodeInstruction(OpCodes.Unbox);
            }
            return codes.AsEnumerable();
        }
    }
}