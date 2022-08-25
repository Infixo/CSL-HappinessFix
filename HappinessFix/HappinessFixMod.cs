using ICities;
using CitiesHarmony.API;
// Make sure that "using HarmonyLib;" does not appear here!
// Only reference HarmonyLib in code that runs when Harmony is ready (DoOnHarmonyReady, IsHarmonyInstalled)
/*
 * Make sure that there are no references to HarmonyLib in your IUserMod implementation.
 * Otherwise the mod could not be loaded if CitiesHarmony is not subscribed. Instead,
 * it is recommended to keep HarmonyLib-related code (such as calls to PatchAll and UnpatchAll)
 * in a separate static Patcher class.
 * */

namespace HappinessFix
{
    public class HappinessFixMod : IUserMod
    {
        public string Name => "Happiness Fix";
        public string Description => "Fixes Commercial Happiness";

        /*
        public class CommercialBuildingAIFixed : CommercialBuildingAI
        {
            protected new void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
            {
                base.SimulationStepActive(buildingID, ref buildingData, ref frameData);
            }
        }
        */

        public void OnEnabled()
        {
            //HarmonyHelper.EnsureHarmonyInstalled();
            /*
             * Will invoke the passed action when Harmony 2.x is ready to use.
             * This hook should be called from IUserMod.OnEnabled. If the Harmony
             * mod is not installed, this hook will attempt to auto-subscribe to it. */
            HarmonyHelper.DoOnHarmonyReady(() => HappinessFixPatcher.PatchAll());
            //if (HarmonyHelper.IsHarmonyInstalled) HappinessFixPatcher.PatchAll();
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) HappinessFixPatcher.UnpatchAll();
            /* Returns true is Harmony is ready to be used. When queried, this hook will not attempt
             * to auto-subscribe to the Harmony workshop item. Use this hook for all kinds of unpatching,
             * applying patches in the LoadingExtension or while the simulation is running. */
        }
    }
}
