using System.Reflection;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.Math;
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
            //Harmony.DEBUG = true;
            var harmony = new Harmony(HarmonyId);
            //harmony.PatchAll(typeof(HappinessFixPatcher).Assembly); // you can also do manual patching here!
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (Harmony.HasAnyPatches(HarmonyId))
            {
                Debug.Log("PatchAll: methods patched ok");
                patched = true;
                var myOriginalMethods = harmony.GetPatchedMethods();
                foreach (var method in myOriginalMethods)
                    Debug.Log($"...method {method.Name} from {method.Module}");
            }
            else
                Debug.Log("ERROR PatchAll: methods not patched");
        }

        public static void UnpatchAll()
        {
            if (!patched) { Debug.Log("UnpatchAll: not patched!");  return; }
            //Harmony.DEBUG = true;
            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            patched = false;
            //Harmony.DEBUG = false;
        }

        // debug info
        public static void DebugLogPatches()
        {
            Harmony.DEBUG = true;
            /*
            // To get a list of all patched methods in the current appdomain(yours and others), call GetAllPatchedMethods:
            Debug.Log("==== ALL METHODS ====");
            var originalMethods = Harmony.GetAllPatchedMethods();
            foreach (var method in originalMethods)
                Debug.Log($"Method {method.Name} from {method.Module}");
            */
            //If you are only interested in your own patched methods, use GetPatchedMethods:
            Debug.Log("==== MY METHODS ====");
            var harmony = new Harmony(HarmonyId);
            var myOriginalMethods = harmony.GetPatchedMethods();
            foreach (var method in myOriginalMethods)
                Debug.Log($"Method {method.Name} from {method.Module}");
            Harmony.DEBUG = false;
        }
/*
        public static void DebugLogMethod(string method)
        {
            // get the MethodBase of the original
            var original = typeof(TheClass).GetMethod("TheMethod");

            // retrieve all patches
            var patches = Harmony.GetPatchInfo(original);
            if (patches is null) return; // not patched

            // get a summary of all different Harmony ids involved
            FileLog.Log("all owners: " + patches.Owners);

            // get info about all Prefixes/Postfixes/Transpilers
            foreach (var patch in patches.Prefixes)
            {
                FileLog.Log("index: " + patch.index);
                FileLog.Log("owner: " + patch.owner);
                FileLog.Log("patch method: " + patch.PatchMethod);
                FileLog.Log("priority: " + patch.priority);
                FileLog.Log("before: " + patch.before);
                FileLog.Log("after: " + patch.after);
            }
        }
*/
}
    
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
}