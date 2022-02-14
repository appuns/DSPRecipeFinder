using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using static UnityEngine.GUILayout;
using UnityEngine.Rendering;
using Steamworks;
using rail;
using xiaoye97;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace DSPRecipeFinder
{

    [BepInPlugin("Appun.DSP.plugin.RecipeFinder", "DSPRecipeFinder", "0.0.1")]
    [BepInProcess("DSPGAME.exe")]

    [HarmonyPatch]
    public class Main : BaseUnityPlugin
    {

        public static GameObject incSepLine2obj;

        public struct recipeStruct
        {
            public int ResultsCount;
            public int[] Results;
            public int ItemsCount;
            public int[] Items;
        }

        //public static List<recipeStruct> recipeList = new List<recipeStruct>();

        public static Dictionary<int, recipeStruct> recipeDictionary = new Dictionary<int, recipeStruct>();



        public void Start()
        {
            LogManager.Logger = Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        }


        [HarmonyPostfix, HarmonyPatch(typeof(UIItemTip), "SetTip")]
        public static void UIItemTip_SetTip_Postfix(UIItemTip __instance)
        {
            //UIItemTip storageWindow = UIRoot.instance.uiGame.itemupTips;
            //__instance.eventLock = true

            if (!recipeDictionary.ContainsKey(__instance.showingItemId))
            {
                return;
            }
            recipeStruct recipestruct = recipeDictionary[__instance.showingItemId];
            //解除されていないレシピは削除
            List<int> tmpItems = new List<int>();
            for (int i = 0; i < recipestruct.ItemsCount; i++)
            {
                RecipeProto recipeProto = LDB.recipes.Select(recipestruct.Items[i]);
                if (GameMain.history.recipeUnlocked.Contains(recipeProto.ID))
                {
                    tmpItems.Add(recipestruct.Items[i]);
                }

            }




            //LogManager.Logger.LogInfo($"----------------------------------------__instance.showingItemId : {__instance.showingItemId}");
            //LogManager.Logger.LogInfo($"----------------------------------------recipestruct.ResultsCount : {recipestruct.ResultsCount}");
            //LogManager.Logger.LogInfo($"----------------------------------------ResultsCount + ItemsCount : {recipestruct.ResultsCount + recipestruct.ItemsCount}");
            int maxRow = tmpItems.Count; 
            int widthMax = 0;
            int leftWIdthMax = 0;
            //2列に分けたときの最大行数計算
          　if (tmpItems.Count > 7)
            {
                maxRow = (int)Math.Ceiling((double)tmpItems.Count / 2);                        
            }

            for (int i = 0; i < tmpItems.Count; i++)
            {
                //レシピが解除されていなければスキップ

                //レシピを表示&位置設定
                if (__instance.recipeEntryArr[recipestruct.ResultsCount + i] == null)
                // LogManager.Logger.LogInfo($"----------------------------------------recipestruct.Results[i] : {recipestruct.Results[i]}");
                {
                    __instance.recipeEntryArr[recipestruct.ResultsCount + i] = UnityEngine.Object.Instantiate<UIRecipeEntry>(__instance.recipeEntry, __instance.transform);
                }
                RecipeProto recipeProto = LDB.recipes.Select(recipestruct.Items[i]);
                __instance.recipeEntryArr[recipestruct.ResultsCount + i].SetRecipe(recipeProto);
                if (i < maxRow)
                {
                    __instance.recipeEntryArr[recipestruct.ResultsCount + i].rectTrans.anchoredPosition = new Vector2(12f, __instance.recipeEntryArr[0].rectTrans.anchoredPosition.y - (float)((recipestruct.ResultsCount + i) * 40 + 13));
                }
                else
                {
                    __instance.recipeEntryArr[recipestruct.ResultsCount + i].rectTrans.anchoredPosition = new Vector2(leftWIdthMax * 40 + 30 + 12f, __instance.recipeEntryArr[0].rectTrans.anchoredPosition.y - (float)((recipestruct.ResultsCount + i - maxRow) * 40 + 13));
                }
                __instance.recipeEntryArr[recipestruct.ResultsCount + i].gameObject.SetActive(true);
                //最大幅の更新
                int width = recipeProto.Results.Length + recipeProto.Items.Length + 1;
                if (width > widthMax)
                {
                    widthMax = width;
                }
                if (i == maxRow - 1)
                {
                    leftWIdthMax = widthMax;
                    widthMax = 0;
                }
            }

            //ラインの複製と配置
            //if (tmpItems.Count > 0)
            //{
            //    if (incSepLine2obj == null)
            //    {
            //        incSepLine2obj = Instantiate(__instance.incSepLine.gameObject, __instance.incSepLine.transform.parent);
            //    }
            //    incSepLine2obj.transform.localPosition = new Vector2(12f, __instance.recipeEntryArr[recipestruct.ResultsCount].rectTrans.anchoredPosition.y +8f);
            //    //LogManager.Logger.LogInfo("--------------------------------  incSepLine2obj.transform.localPosition.y : " + incSepLine2obj.transform.localPosition.y);
            //    incSepLine2obj.SetActive(true);
            //}
            //else
            //{
            //    incSepLine2obj.SetActive(false);
            //}

            //ウインドウのサイズの調整
            float x = (leftWIdthMax + widthMax) * 40 + 30;
            if (leftWIdthMax>0)
            {
                x += 30;
            }
            if (x < __instance.trans.sizeDelta.x)
            {
                x = __instance.trans.sizeDelta.x;
            }
            __instance.trans.sizeDelta = new Vector2(x, (float)(__instance.trans.sizeDelta.y + maxRow * 40 + 20));


        }

        //レシピをチェックしてLIST作成
        [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        public static void VFPreload_InvokeOnLoadWorkEnded_Patch()
        {
            for (int h = 0; h < LDB.items.Length; h++)
            {
                ItemProto itemProto = LDB.items.dataArray[h];

                recipeStruct recipestruct = new recipeStruct();
                //recipestruct.id = itemProto.ID;
                //LogManager.Logger.LogInfo($"----------------------------------------itemProto.ID : {itemProto.ID}");
                //LogManager.Logger.LogInfo($"----------------------------------------itemProto.name : {itemProto.name.Translate()}");

                recipestruct.ResultsCount = 0;
                recipestruct.ItemsCount = 0;

                for (int i = 0; i < LDB.recipes.Length; i++)
                {
                    RecipeProto recipeProto = LDB.recipes.dataArray[i];
                    //生産物にあるかチェック
                    for (int j = 0; j < recipeProto.Results.Length; j++)
                    {
                        if (recipeProto.Results[j] == itemProto.ID)
                        {
                            //LogManager.Logger.LogInfo($"----------------------------------------ResultsMatch : {recipeProto.name.Translate()}");
                            recipestruct.ResultsCount++;
                            Array.Resize(ref recipestruct.Results, recipestruct.ResultsCount);
                            recipestruct.Results[recipestruct.ResultsCount-1] = recipeProto.ID;
                            //LogManager.Logger.LogInfo($"----------------------------------------Results[{recipestruct.ResultsCount-1}] : {recipestruct.Results[recipestruct.ResultsCount-1]}");
                        }
                    }
                    //LogManager.Logger.LogInfo($"----------------------------------------ResultsCount : {recipestruct.ResultsCount}");
                    for (int j = 0; j < recipeProto.Items.Length; j++)
                    {
                        if (recipeProto.Items[j] == itemProto.ID)
                        {
                            //LogManager.Logger.LogInfo($"----------------------------------------ItemsMatch : {recipeProto.name.Translate()}");
                            //LogManager.Logger.LogInfo($"----------------------------------------ItemsCount[{recipestruct.ItemsCount}]");
                            recipestruct.ItemsCount++;
                            //LogManager.Logger.LogInfo($"----------------------------------------ItemsCount[{recipestruct.ItemsCount}]");
                            Array.Resize(ref recipestruct.Items, recipestruct.ItemsCount);
                            //LogManager.Logger.LogInfo($"----------------------------------------Items.Length[{recipestruct.Items.Length}]");
                            recipestruct.Items[recipestruct.ItemsCount - 1] = recipeProto.ID;
                       //LogManager.Logger.LogInfo($"----------------------------------------Items[{recipestruct.ItemsCount-1}] : {recipestruct.Items[recipestruct.ItemsCount-1]}");
                        }
                    }
                    //LogManager.Logger.LogInfo($"----------------------------------------ItemsCount : {recipestruct.ItemsCount}");
                }

                //LogManager.Logger.LogInfo($"----------------------------------------itemProto.name : {itemProto.name.Translate()}");
                //LogManager.Logger.LogInfo($"----------------------------------------recipestruct.ResultsCount : {recipestruct.ResultsCount}");
                //LogManager.Logger.LogInfo($"----------------------------------------recipestruct.ItemsCount : {recipestruct.ItemsCount}");




                recipeDictionary.Add(itemProto.ID,recipestruct);

            }
            LogManager.Logger.LogInfo($"Recipe Dictionary created");
        }
    }

        public class LogManager
    {
        public static ManualLogSource Logger;
    }

}