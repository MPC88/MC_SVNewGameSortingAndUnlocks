using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MC_SVFilters
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        // BepInEx
        public const string pluginGuid = "mc.starvalor.newgamesortingandunlocks";
        public const string pluginName = "SV New Game Sorting and Unlocks";
        public const string pluginVersion = "1.0.2";

        // Mod        
        private static int[] shipExcludeList = { 92, 94, 101 }; // 92 = Shriek, 94 = Thoth, 101 = Testudo
        private static int[] crewExclueList = { 0, 2, 10, 13 }; // 0 = Laious, 2 = The Exiled, 10 = 0x3D07, 13 = Sam Holo's Gunner

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        [HarmonyPatch(typeof(PerksPanel), "ShowShips")]
        [HarmonyPrefix]
        // Show all ships
        private static bool PerksPanelShowShips_Pre(PerksPanel __instance, StartingShipControl startingShipControl)
        {
            AccessTools.Method(typeof(PerksPanel), "Validate").Invoke(__instance, null);
            Transform ___panel = (Transform)(AccessTools.Field(typeof(PerksPanel), "panel").GetValue(__instance));
            Text ___title = (Text)(AccessTools.Field(typeof(PerksPanel), "title").GetValue(__instance));
            AccessTools.FieldRefAccess<PerksPanel, StartingShipControl>(__instance, "ssc") = startingShipControl;

            List<int> shipList = GetAllShips();
            int count = shipList.Count;
            int i = 0;
            for (int j = 0; j < count; j++)
            {
                ShipModelData model = ShipDB.GetModel(shipList[j]);
                if (i >= ___panel.childCount)
                {
                    UnityEngine.Object.Instantiate(__instance.perkGO, ___panel);
                }
                ___panel.GetChild(i).GetComponent<PerkControl>().SetupShip(model, __instance);
                ___panel.GetChild(i).gameObject.SetActive(value: true);
                if (!ShipUnlocked(shipList[j]))
                {
                    PerkControl pc = ___panel.GetChild(i).GetComponentInChildren<PerkControl>();
                    AccessTools.FieldRefAccess<PerkControl, Image>(pc, "image").sprite = PerkDB.lockedSprite;
                }

                i++;
            }
            AccessTools.Method(typeof(PerksPanel), "AdjustPanelSize").Invoke(__instance, new object[] { i });
            for (; i < ___panel.childCount; i++)
            {
                ___panel.GetChild(i).gameObject.SetActive(value: false);
            }
            ___title.text = Lang.Get(0, 269);

            return false;
        }

        [HarmonyPatch(typeof(PerksPanel), "ShowCrewMembers")]
        [HarmonyPrefix]
        // Show all ships
        private static bool PerksPanelShowCrewMembers_Pre(PerksPanel __instance, StartingCrewControl startingCrewControl)
        {
            AccessTools.Method(typeof(PerksPanel), "Validate").Invoke(__instance, null);
            Transform ___panel = (Transform)(AccessTools.Field(typeof(PerksPanel), "panel").GetValue(__instance));
            Text ___title = (Text)(AccessTools.Field(typeof(PerksPanel), "title").GetValue(__instance));
            AccessTools.FieldRefAccess<PerksPanel, StartingCrewControl>(__instance, "scc") = startingCrewControl;

            List<int> crewList = GetAllCrew();
            int count = crewList.Count;
            int i = 0;

            for (int j = 0; j < count; j++)
            {
                CrewMember predefinedCrewMember = CrewDB.GetPredefinedCrewMember(crewList[j]);
                if (i >= ___panel.childCount)
                {
                    UnityEngine.Object.Instantiate(__instance.perkGO, ___panel);
                }
                ___panel.GetChild(i).GetComponent<PerkControl>().SetupCrewman(predefinedCrewMember, __instance);
                ___panel.GetChild(i).gameObject.SetActive(value: true);
                if (!CrewUnlocked(crewList[j]))
                {
                    PerkControl pc = ___panel.GetChild(i).GetComponentInChildren<PerkControl>();
                    AccessTools.FieldRefAccess<PerkControl, Image>(pc, "image").sprite = PerkDB.lockedSprite;
                }
                i++;
            }
            AccessTools.Method(typeof(PerksPanel), "AdjustPanelSize").Invoke(__instance, new object[] { i });
            for (; i < ___panel.childCount; i++)
            {
                ___panel.GetChild(i).gameObject.SetActive(value: false);
            }
            ___title.text = Lang.Get(23, 159);

            return false;
        }

        [HarmonyPatch(typeof(PerkControl), "Click")]
        [HarmonyPrefix]
        // Do not select a ship or crew if it is not unlocked.
        private static bool PerkControlClick_Pre(ShipModelData ___shipModel, CrewMember ___crewMember)
        {
            if ((___shipModel != null && !ShipUnlocked(___shipModel.id)) ||
                ___crewMember != null && !CrewUnlocked(___crewMember.id))
                return false;

            return true;
        }

        // Get a list of all ships
        private static List<int> GetAllShips()
        {
            List<int> ships = new List<int>();

            foreach (ShipModelData smd in AccessTools.StaticFieldRefAccess<List<ShipModelData>>(typeof(ShipDB), "shipModels"))
                if (!shipExcludeList.Contains<int>(smd.id))
                    ships.Add(smd.id);

            ships.Sort(CompareShips);

            return ships;
        }

        private static int CompareShips(int x, int y)
        {
            // 1 => x > y, -1 => x < y, 0 => x==y
            ShipModelData shipX = ShipDB.GetModel(x);
            ShipModelData shipY = ShipDB.GetModel(y);

            if (shipX.shipClass > shipY.shipClass)
                return 1;
            else if (shipX.shipClass < shipY.shipClass)
                return -1;
            else
            {
                if (shipX.rarity > shipY.rarity)
                    return 1;
                if (shipX.rarity < shipY.rarity)
                    return -1;
                else
                {
                    if ((int)shipX.shipRole > (int)shipY.shipRole)
                        return 1;
                    else if ((int)shipX.shipRole < (int)shipY.shipRole)
                        return -1;
                    else
                        return 0;
                }
            }
        }

        // Get a list of all crew
        private static List<int> GetAllCrew()
        {
            List<int> crew = new List<int>();

            foreach (CrewMember cm in GameManager.predefinitions.crewMembers)
                if (!crewExclueList.Contains<int>(cm.id))
                    crew.Add(cm.id);

            return crew;
        }

        private static bool ShipUnlocked(int id)
        {
            return GenData.GetUnlockedShips().Contains(id);
        }

        private static bool CrewUnlocked(int id)
        {
            return GenData.GetUnlockedCrewMembers().Contains(id);
        }

        [HarmonyPatch(typeof(ShipDB), nameof(ShipDB.GetModelString))]
        [HarmonyPostfix]
        private static void ShipDBGetString_Post(int id, ref string __result)
        {
            if (GameManager.instance != null && GameManager.instance.inGame)
                return;

            ShipModelData smd = ShipDB.GetModel(id);
            if (smd == null || (smd.id != id))
                return;

            string matchString = "\n\n" + Lang.Get(5, 116) + ":  </color><b>" + smd.spaceOcupied + "</b>";
            string pointString = Lang.Get(0, 419, "</color><b>" + smd.startingCost + "</b>");
            __result = __result.Insert(__result.IndexOf(matchString) + matchString.Length, ColorSys.gray + "\n" + pointString);
        }
    }
}
