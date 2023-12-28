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

    [BepInPlugin("Appun.DSP.plugin.RecipeFinder", "DSPRecipeFinder", "0.0.5")]
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
        public static Dictionary<int, recipeStruct> recipeDictionary = new Dictionary<int, recipeStruct>();

        public void Start()
        {
            LogManager.Logger = Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }


        //[HarmonyPostfix, HarmonyPatch(typeof(UIItemTip), "SetTip")]
        //[HarmonyPriority(1)]
        //public static void UIItemTip_SetTip_PostFix()
        //      {
        //	LogManager.Logger.LogError("------------------------------------------UIItemTip_SetTip_PostFix");

        //}


        [HarmonyPrefix, HarmonyPatch(typeof(UIItemTip), "SetTip")]
        [HarmonyPriority(1)]

        public static bool UIItemTip_SetTip_Prefix(UIItemTip __instance, int itemId, int corner, Vector2 offset, Transform parent, int itemCount, int incCount, UIButton.ItemTipType type, bool cannothand = false, bool cannotInc = false, bool isRecipe = false, bool isSign = false)
        {

            //オリジナル
            __instance.showingItemId = itemId;
            __instance.cannotHandmake = cannothand;
            int num;
            int id;
            if (itemId > 0)
            {
                num = itemId;
                id = 0;
            }
            else if (itemId < 0)
            {
                num = 0;
                id = -itemId;
            }
            else
            {
                num = 0;
                id = 0;
            }
            __instance.trans.SetParent(parent, true);
            int num2 = (itemCount == 0) ? incCount : (incCount / itemCount);
            ItemProto itemProto = LDB.items.Select(num);
            RecipeProto recipeProto = LDB.recipes.Select(id);
            if (recipeProto != null)
            {
                if (UIItemTip.tmp_recipeList == null)
                {
                    UIItemTip.tmp_recipeList = new List<RecipeProto>();
                    UIItemTip.tmp_recipeList.Add(null);
                }
                UIItemTip.tmp_recipeList[0] = recipeProto;
            }
            string text = (itemProto == null) ? ((recipeProto == null) ? "Unknown item" : ("括号公式".Translate() + recipeProto.name)) : itemProto.name;
            string text2 = (itemProto == null) ? ((recipeProto == null) ? "Unknown type" : recipeProto.madeFromString) : itemProto.typeString;
            string text3 = (itemProto == null) ? ((recipeProto == null) ? "" : recipeProto.description) : itemProto.description;
            Sprite sprite = (itemProto == null) ? ((recipeProto == null) ? null : recipeProto.iconSprite) : itemProto.iconSprite;

            //オリジナル//////////////////////////////////////////////////////
            //List<RecipeProto> list = (itemProto == null) ? ((recipeProto == null) ? null : UIItemTip.tmp_recipeList) : itemProto.recipes;
            //変更後//////////////////////////////////////////////////////////
            List<RecipeProto> listOrigin = (itemProto == null) ? ((recipeProto == null) ? null : UIItemTip.tmp_recipeList) : itemProto.recipes;
            ////////////////////////////////////////////////////////////////////

            ////listに追加//////////////////////////////////////////////////////
            ////追加////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////

            List<RecipeProto> list = new List<RecipeProto>();
            if (listOrigin != null)
            {
                foreach (RecipeProto recipe in listOrigin)
                {
                    list.Add(recipe);
                }
            }
            if (recipeDictionary.ContainsKey(itemId))
            {
                recipeStruct recipestruct = recipeDictionary[itemId];
                //解除されていないレシピは削除
                List<int> tmpItems = new List<int>();
                for (int i = 0; i < recipestruct.ItemsCount; i++)
                {
                    RecipeProto recipeProto2 = LDB.recipes.Select(recipestruct.Items[i]);
                    if (GameMain.history.recipeUnlocked.Contains(recipeProto2.ID))
                    {
                        tmpItems.Add(recipestruct.Items[i]);
                    }
                }
                for (int i = 0; i < tmpItems.Count; i++)
                {
                    RecipeProto recipeProto2 = LDB.recipes.Select(tmpItems[i]);
                    RecipeProto newRecipeProto = new RecipeProto();
                    //該当アイテムを素材リストの先頭に移動
                    newRecipeProto.ResultCounts = recipeProto2.ResultCounts;
                    newRecipeProto.Results = recipeProto2.Results;
                    newRecipeProto.ItemCounts = new int[recipeProto2.ItemCounts.Length];
                    for (int j = 0; j < recipeProto2.ItemCounts.Length; j++)
                    {
                        newRecipeProto.ItemCounts[j] = recipeProto2.ItemCounts[j];
                    }
                    newRecipeProto.Items = new int[recipeProto2.Items.Length];
                    for (int j = 0; j < recipeProto2.Items.Length; j++)
                    {
                        newRecipeProto.Items[j] = recipeProto2.Items[j];
                    }
                    newRecipeProto.Type = recipeProto2.Type;
                    newRecipeProto.TimeSpend = recipeProto2.TimeSpend;
                    newRecipeProto.Type = recipeProto2.Type;
                    for (int j = 0; j < newRecipeProto.Items.Length; j++)
                    {
                        if (newRecipeProto.Items[j] == itemId)
                        {
                            newRecipeProto.Items[0] = recipeProto2.Items[j];
                            newRecipeProto.Items[j] = recipeProto2.Items[0];
                            newRecipeProto.ItemCounts[0] = recipeProto2.ItemCounts[j];
                            newRecipeProto.ItemCounts[j] = recipeProto2.ItemCounts[0];
                            break;
                        }
                    }
                    list.Add(newRecipeProto);
                }
            }
            //2列に分けたときの最大行数計算
            int maxRow = list.Count;
            int cutRow = 0;
            bool wideWindow = false; ;
            if (list.Count > 9)
            {
                maxRow = (int)Math.Ceiling((double)list.Count / 2);
                wideWindow = true;
                cutRow = list.Count - maxRow;
            }
            int leftWIdthMax = 0;
            ////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////
            int num3 = 0;
            string text4 = "";
            string text5 = "";
            int num4 = 0;
            StringBuilder sb = new StringBuilder("         ", 12);
            if (itemProto != null)
            {
                if (itemProto.HeatValue > 0L)
                {
                    text2 += "顿号燃料".Translate();
                }
                bool flag = false;
                num3 = itemProto.DescFields.Length;
                for (int i = 0; i < num3; i++)
                {
                    if (itemProto.DescFields[i] == 40)
                    {
                        flag = true;
                    }
                    else
                    {
                        text4 = text4 + itemProto.GetPropName(i) + "\r\n";
                        text5 = text5 + itemProto.GetPropValue(i, sb, num2) + "\r\n";
                    }
                }
                if (flag)
                {
                    num3--;
                }
                if (itemProto.maincraft != null && !string.IsNullOrEmpty(itemProto.produceFrom))
                {
                    text4 = text4 + "制造于".Translate() + "\r\n";
                    text5 = text5 + itemProto.produceFrom + "\r\n";
                    num3++;
                }
                if (__instance.cannotHandmake)
                {
                    text4 += "\r\n";
                    text5 = text5 + "不能手动制造".Translate() + "\r\n";
                    num3++;
                }
                else if (itemProto.handcraft != null)
                {
                    text4 = text4 + "手动制造".Translate() + "\r\n";
                    text5 = text5 + "合成面板".Translate() + "\r\n";
                    num3++;
                }
                if (!GameMain.history.ItemUnlocked(num))
                {
                    if (itemProto.preTech != null)
                    {
                        __instance.preTechText.text = "需要科技".Translate() + "\r\n" + itemProto.preTech.name;
                        num4 = 2;
                    }
                    else if (itemProto.missingTech)
                    {
                        __instance.preTechText.text = "该版本尚未加入".Translate();
                        num4 = 1;
                    }
                }
                if (num4 == 0)
                {
                    __instance.preTechText.text = "";
                }
            }
            else if (recipeProto != null)
            {
                num3 = 0;
                num4 = 0;
                __instance.preTechText.text = "";
                if (__instance.cannotHandmake)
                {
                    text4 += "\r\n";
                    text5 = text5 + "不能手动制造".Translate() + "\r\n";
                    num3++;
                }
            }
            __instance.nameText.text = text;
            __instance.categoryText.text = text2;
            __instance.descText.text = text3;
            __instance.propsText.text = text4;
            __instance.valuesText.text = text5;
            __instance.iconImage.sprite = sprite;
            int num5 = (int)__instance.preTechText.preferredWidth + 126;
            int num6 = (int)__instance.valuesText.preferredWidth;
            int num7 = Mathf.Max(0, num6 - 166);
            __instance.valuesText.rectTransform.sizeDelta = new Vector2((float)(166 + num7), __instance.valuesText.rectTransform.sizeDelta.y);
            num6 += 230;
            int num8 = 290;
            if (num8 < num5)
            {
                num8 = num5;
            }
            if (num8 < num6)
            {
                num8 = num6;
            }
            int num9 = num8 - 290;
            if (num9 < 0)
            {
                num9 = 0;
            }
            __instance.preTechText.rectTransform.sizeDelta = new Vector2(__instance.preTechText.preferredWidth + 1f, __instance.preTechText.rectTransform.sizeDelta.y);
            Vector2 anchorMin;
            Vector2 pivot;
            switch (corner)
            {
                case 1:
                    anchorMin = new Vector2(0f, 0f);
                    pivot = new Vector2(1f, 1f);
                    break;
                case 2:
                    anchorMin = new Vector2(0.5f, 0f);
                    pivot = new Vector2(0.5f, 1f);
                    break;
                case 3:
                    anchorMin = new Vector2(1f, 0f);
                    pivot = new Vector2(0f, 1f);
                    break;
                case 4:
                    anchorMin = new Vector2(0f, 0.5f);
                    pivot = new Vector2(1f, 0.5f);
                    break;
                case 5:
                    anchorMin = new Vector2(0.5f, 0.5f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case 6:
                    anchorMin = new Vector2(1f, 0.5f);
                    pivot = new Vector2(0f, 0.5f);
                    break;
                case 7:
                    anchorMin = new Vector2(0f, 1f);
                    pivot = new Vector2(0f, 1f);
                    break;
                case 8:
                    anchorMin = new Vector2(0.5f, 1f);
                    pivot = new Vector2(0.5f, 0f);
                    break;
                case 9:
                    anchorMin = new Vector2(1f, 1f);
                    pivot = new Vector2(0f, 0f);
                    break;
                default:
                    anchorMin = new Vector2(0.5f, 0.5f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
            __instance.trans.anchorMax = (__instance.trans.anchorMin = anchorMin);
            __instance.trans.pivot = pivot;
            __instance.trans.anchoredPosition = offset;
            bool flag2 = num3 + num4 <= 2;
            if (flag2)
            {
                __instance.descText.rectTransform.anchoredPosition = new Vector2(110f, -47f);
                __instance.descText.rectTransform.sizeDelta = new Vector2((float)(166 + num9), 16f);
            }
            else
            {
                __instance.descText.rectTransform.anchoredPosition = new Vector2(12f, -47f);
                __instance.descText.rectTransform.sizeDelta = new Vector2((float)(268 + num9), 16f);
            }
            int num10 = string.IsNullOrEmpty(text3) ? 0 : ((int)__instance.descText.preferredHeight);
            int num11 = 61 + num10;
            int num12 = flag2 ? -50 : (-num11 + 11);
            __instance.iconImage.rectTransform.anchoredPosition = new Vector2(15f, (float)num12);
            int num13 = num12 - 80;
            int num14 = 17 * num3 + num11;
            int num15 = (num4 == 0) ? 0 : (num4 * 17 + 6);
            __instance.preTechText.gameObject.SetActive(num4 > 0);
            bool flag3 = false;
            int num16 = 0;
            int num17 = -(num14 + num15);
            if (num17 > num13 - 8)
            {
                num16 = num17 - (num13 - 8);
                num17 = num13 - 8;
                flag3 = true;
            }
            num16 /= 2;
            __instance.propsText.rectTransform.anchoredPosition = new Vector2(110f, (float)(-(float)num11 + 11 - num16));
            __instance.valuesText.rectTransform.anchoredPosition = new Vector2((float)(110 + num9 - num7), (float)(-(float)num11 + 11 - num16));
            __instance.preTechText.rectTransform.anchoredPosition = new Vector2(__instance.preTechText.rectTransform.anchoredPosition.x, (float)(-(float)num14 + 6 - num16));
            int num18 = 0;
            int num19 = 0;
            __instance.incSepLine.gameObject.SetActive(false);
            __instance.incPointText.gameObject.SetActive(false);
            __instance.incNameText1.gameObject.SetActive(false);
            __instance.incNameText2.gameObject.SetActive(false);
            __instance.incNameText3.gameObject.SetActive(false);
            for (int j = 0; j < __instance.incDescText1.Length; j++)
            {
                __instance.incDescText1[j].gameObject.SetActive(false);
                __instance.incDescText2[j].gameObject.SetActive(false);
                __instance.incDescText3[j].gameObject.SetActive(false);
            }
            for (int k = 0; k < __instance.incExtraDescText.Length; k++)
            {
                __instance.incExtraDescText[k].gameObject.SetActive(false);
            }
            __instance.itemIncs[0].enabled = false;
            __instance.itemIncs[1].enabled = false;
            __instance.itemIncs[2].enabled = false;
            if (GameMain.history.TechUnlocked(1151) && !isSign && type != UIButton.ItemTipType.Other)
            {
                if (isRecipe)
                {
                    bool flag4 = true;
                    bool flag5 = false;
                    bool flag6 = false;
                    if (itemProto != null)
                    {
                        for (int l = 0; l < itemProto.recipes.Count; l++)
                        {
                            flag4 &= itemProto.recipes[l].productive;
                            flag5 |= (itemProto.recipes[l].Type == ERecipeType.Fractionate);
                        }
                    }
                    else if (recipeProto != null)
                    {
                        flag4 = recipeProto.productive;
                        flag6 = (recipeProto.Type == ERecipeType.Fractionate);
                    }
                    __instance.incSepLine.gameObject.SetActive(true);
                    __instance.incSepLine.anchoredPosition = new Vector2(12f, (float)num17);
                    num19 = 0;
                    num18 = 40;
                    __instance.incExtraDescText[0].gameObject.SetActive(true);
                    __instance.incExtraDescText[0].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[0].rectTransform.anchoredPosition.x, (float)(num17 - 10 - num19 * 17));
                    __instance.incExtraDescText[0].text = "+\u00a0" + (flag4 ? "增产公式描述1".Translate() : "增产公式描述2".Translate());
                    num19 += Mathf.FloorToInt(__instance.incExtraDescText[0].preferredHeight / 25f);
                    num18 += 17 * Mathf.FloorToInt(__instance.incExtraDescText[0].preferredHeight / 25f);
                    if (flag6)
                    {
                        __instance.incExtraDescText[0].text = "+\u00a0" + "增产公式描述3".Translate();
                    }
                    if (flag5)
                    {
                        num19 -= Mathf.FloorToInt(__instance.incExtraDescText[0].preferredHeight / 25f);
                        num18 -= 17 * Mathf.FloorToInt(__instance.incExtraDescText[0].preferredHeight / 25f);
                        num19 += Mathf.CeilToInt(__instance.incExtraDescText[0].preferredHeight / 25f);
                        num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[0].preferredHeight / 25f);
                        __instance.incExtraDescText[1].gameObject.SetActive(true);
                        __instance.incExtraDescText[1].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[1].rectTransform.anchoredPosition.x, (float)(num17 - 10 - num19 * 17));
                        __instance.incExtraDescText[1].text = "+\u00a0" + "增产公式描述3".Translate();
                        num19 += Mathf.FloorToInt(__instance.incExtraDescText[1].preferredHeight / 25f);
                        num18 += 17 * Mathf.FloorToInt(__instance.incExtraDescText[1].preferredHeight / 25f);
                    }
                }
                else if (itemProto != null)
                {
                    num18 = 40;
                    num19 = 1;
                    int num20 = 0;
                    if (UIItemTip.tmp_incDic == null)
                    {
                        UIItemTip.tmp_incDic = new Dictionary<int, int>();
                    }
                    UIItemTip.tmp_incDic.Clear();
                    __instance.incSepLine.gameObject.SetActive(true);
                    __instance.incSepLine.anchoredPosition = new Vector2(12f, (float)num17);
                    if (type != UIButton.ItemTipType.IgnoreIncPoint || incCount > 0)
                    {
                        __instance.incPointText.gameObject.SetActive(true);
                        __instance.incPointText.rectTransform.anchoredPosition = new Vector2(__instance.incPointText.rectTransform.anchoredPosition.x, (float)(num17 - 8));
                        __instance.incPointText.text = "增产点数共计".Translate() + incCount + "空格点".Translate();
                        __instance.incPointText.color = ((incCount > 0) ? __instance.containIncColor : __instance.noIncColor);
                    }
                    else
                    {
                        num18 = 23;
                        num19 = 0;
                    }
                    if (incCount > 0)
                    {
                        num18 += 2;
                        __instance.incNameText1.gameObject.SetActive(true);
                        __instance.incNameText2.gameObject.SetActive(true);
                        __instance.incNameText3.gameObject.SetActive(true);
                        __instance.incNameText1.rectTransform.anchoredPosition = new Vector2(__instance.incNameText1.rectTransform.anchoredPosition.x, (float)(num17 - 10 - num19 * 17));
                        num18 += 17;
                        num19++;
                        int num21 = (int)Cargo.fastIncArrowTable[(num2 > 10) ? 10 : num2];
                        __instance.itemIncs[0].enabled = (num21 == 1);
                        __instance.itemIncs[1].enabled = (num21 == 2);
                        __instance.itemIncs[2].enabled = (num21 == 3);
                        int num22 = incCount - num2 * itemCount;
                        int num23 = num2 + 1;
                        int num24 = itemCount - num22;
                        num2 = ((num2 > 10) ? 10 : num2);
                        num23 = ((num23 > 10) ? 10 : num23);
                        if (num24 > 0)
                        {
                            UIItemTip.tmp_incDic[num2] = num24;
                        }
                        if (num22 > 0)
                        {
                            UIItemTip.tmp_incDic[num23] = num22;
                        }
                        for (int m = 0; m < UIItemTip.tmp_incDic.Count; m++)
                        {
                            __instance.incDescText1[m].gameObject.SetActive(UIItemTip.tmp_incDic.Count >= m + 1);
                            __instance.incDescText2[m].gameObject.SetActive(UIItemTip.tmp_incDic.Count >= m + 1);
                            __instance.incDescText3[m].gameObject.SetActive(UIItemTip.tmp_incDic.Count >= m + 1);
                            __instance.incDescText1[m].rectTransform.anchoredPosition = new Vector2(__instance.incDescText1[m].rectTransform.anchoredPosition.x, (float)(num17 - 10 - num19 * 17));
                            num19++;
                            if (m >= 1)
                            {
                                break;
                            }
                        }
                        int num25 = 0;
                        foreach (KeyValuePair<int, int> keyValuePair in UIItemTip.tmp_incDic)
                        {
                            __instance.incDescText1[num25].text = keyValuePair.Value + "个物品".Translate();
                            __instance.incDescText2[num25].text = (itemProto.Productive ? "<color=#61D8FFc0>+" : "") + (itemProto.Productive ? (((double)Cargo.incTable[keyValuePair.Key] * 0.1).ToString("0.#") + " %</color>") : "— ");
                            __instance.incDescText3[num25].text = "<color=#FD965ECC>+" + ((double)Cargo.accTable[keyValuePair.Key] * 0.1).ToString("0.#") + " %</color>";
                            num25++;
                            if (num25 >= 2)
                            {
                                break;
                            }
                        }
                        num18 += 17 * UIItemTip.tmp_incDic.Count + 2;
                        __instance.incExtraDescText[num20].gameObject.SetActive(true);
                        __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                        __instance.incExtraDescText[num20].text = "+\u00a0" + (itemProto.Productive ? "增产剂选择描述".Translate() : "增产剂加速描述".Translate());
                        num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                        num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                        num20++;
                        if (itemProto.HeatValue > 0L && itemProto.ID != 2207)
                        {
                            __instance.incExtraDescText[num20].gameObject.SetActive(true);
                            __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                            __instance.incExtraDescText[num20].text = "+\u00a0" + (itemProto.Productive ? "增产剂燃料描述2".Translate() : "增产剂燃料描述1".Translate());
                            num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num20++;
                        }
                        if (itemProto.ID == 1209)
                        {
                            __instance.incExtraDescText[num20].gameObject.SetActive(true);
                            __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                            __instance.incExtraDescText[num20].text = "+\u00a0" + "增产剂透镜描述".Translate();
                            num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num20++;
                        }
                        if (itemProto.ID == 6001 || itemProto.ID == 6002 || itemProto.ID == 6003 || itemProto.ID == 6004 || itemProto.ID == 6005 || itemProto.ID == 6006)
                        {
                            __instance.incExtraDescText[num20].gameObject.SetActive(true);
                            __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                            __instance.incExtraDescText[num20].text = "+\u00a0" + "增产剂矩阵描述".Translate();
                            num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num20++;
                        }
                        if (itemProto.ID == 1141 || itemProto.ID == 1142 || itemProto.ID == 1143)
                        {
                            __instance.incExtraDescText[num20].gameObject.SetActive(true);
                            __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                            __instance.incExtraDescText[num20].text = "+\u00a0" + "增产剂套娃描述".Translate();
                            num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num20++;
                        }
                        if (itemProto.ID == 1131)
                        {
                            __instance.incExtraDescText[num20].gameObject.SetActive(true);
                            __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                            __instance.incExtraDescText[num20].text = "+\u00a0" + "增产剂地基描述".Translate();
                            num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num20++;
                        }
                    }
                    else
                    {
                        num18 += 2;
                        __instance.incExtraDescText[num20].gameObject.SetActive(true);
                        __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 10 - num19 * 17));
                        __instance.incExtraDescText[num20].text = "+\u00a0" + (itemProto.Productive ? "无增产点描述1".Translate() : "无增产点描述2".Translate());
                        num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                        num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                        num20++;
                        if (itemProto.HeatValue > 0L && itemProto.ID != 2207)
                        {
                            __instance.incExtraDescText[num20].gameObject.SetActive(true);
                            __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                            __instance.incExtraDescText[num20].text = "+\u00a0" + (itemProto.Productive ? "增产剂燃料描述2".Translate() : "增产剂燃料描述1".Translate());
                            num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num20++;
                        }
                        if (itemProto.ID == 1209)
                        {
                            __instance.incExtraDescText[num20].gameObject.SetActive(true);
                            __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                            __instance.incExtraDescText[num20].text = "+\u00a0" + "增产剂透镜描述".Translate();
                            num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num20++;
                        }
                        if (itemProto.ID == 6001 || itemProto.ID == 6002 || itemProto.ID == 6003 || itemProto.ID == 6004 || itemProto.ID == 6005 || itemProto.ID == 6006)
                        {
                            __instance.incExtraDescText[num20].gameObject.SetActive(true);
                            __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                            __instance.incExtraDescText[num20].text = "+\u00a0" + "增产剂矩阵描述".Translate();
                            num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                            num20++;
                        }
                    }
                    __instance.incExtraDescText[num20].gameObject.SetActive(true);
                    __instance.incExtraDescText[num20].rectTransform.anchoredPosition = new Vector2(__instance.incExtraDescText[num20].rectTransform.anchoredPosition.x, (float)(num17 - 12 - num19 * 17));
                    __instance.incExtraDescText[num20].text = "增产剂通用提示".Translate();
                    num19 += Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                    num18 += 17 * Mathf.CeilToInt(__instance.incExtraDescText[num20].preferredHeight / 25f);
                    num20++;
                }
            }

            int num26 = 0;
            int num27 = 0;
            if (list != null && list.Count > 0)
            {
                if (__instance.recipeEntryArr == null)
                {
                    __instance.recipeEntryArr = new UIRecipeEntry[32];
                }
                __instance.recipeEntryArr[0] = __instance.recipeEntry;
                num27 = list[0].Results.Length + list[0].Items.Length + 1;
                for (int n = 1; n < list.Count; n++)
                {
                    if (__instance.recipeEntryArr[n] == null)
                    {
                        __instance.recipeEntryArr[n] = UnityEngine.Object.Instantiate<UIRecipeEntry>(__instance.recipeEntry, __instance.transform);
                    }
                    __instance.recipeEntryArr[n].SetRecipe(list[n]);

                    //位置設定////////////////////////////////////////
                    //オリジナル
                    //__instance.recipeEntryArr[n].rectTrans.anchoredPosition = new Vector2(12f, (float)(num17 - n * 40 - 8 - num18));
                    //////////////////////////////////////////
                    //変更後////////////////////////////////////
                    if (n < maxRow)
                    {
                        __instance.recipeEntryArr[n].rectTrans.anchoredPosition = new Vector2(12f, (float)(num17 - n * 40 - 8 - num18));
                    }
                    else
                    {
                        __instance.recipeEntryArr[n].rectTrans.anchoredPosition = new Vector2(leftWIdthMax * 40 + 30 + 12f, num17 - (n - maxRow) * 40 - 8 - num18);
                    }
                    //////////////////////////////////////////
                    __instance.recipeEntryArr[n].gameObject.SetActive(true);
                    int num28 = list[n].Results.Length + list[n].Items.Length + 1;
                    if (num27 < num28)
                    {
                        num27 = num28;
                    }
                    //最大幅の更新
                    //追加/////////////////////////////////////////
                    if (wideWindow && n == maxRow - 1)
                    {
                        leftWIdthMax = num27;
                        num27 = 0;
                    }
                    ///////////////////////////////////////////////
                }
                __instance.recipeEntry.SetRecipe(list[0]);
                __instance.recipeEntry.rectTrans.anchoredPosition = new Vector2(12f, (float)(num17 - 8 - num18));
                __instance.recipeEntry.gameObject.SetActive(true);
                num26 = list.Count;
                __instance.sepLine.gameObject.SetActive(true);
                __instance.sepLine.anchoredPosition = new Vector2(12f, (float)(num17 - num18));
            }
            else
            {
                __instance.sepLine.gameObject.SetActive(false);
            }

            int num29 = 290;
            if (num27 >= 7)
            {
                num29 = num27 * 40 + 30;
            }
            //ウインドウ幅/////////////////////////////////////////////////////
            //追加/////////////////////////////////////////////////////////////
            num29 += leftWIdthMax * 40 + 20;
            ///////////////////////////////////////////////////////////////////
            if (__instance.recipeEntryArr != null)
            {
                for (int num30 = num26; num30 < __instance.recipeEntryArr.Length; num30++)
                {
                    if (__instance.recipeEntryArr[num30] != null)
                    {
                        __instance.recipeEntryArr[num30].gameObject.SetActive(false);
                    }
                }
            }
            //ウインドウ高さ/////////////////////////////////////////////////////////////////
            //オリジナル
            //int num31 = -num17 + ((num26 == 0) ? (flag3 ? 4 : 4) : (num26 * 40 + 20));
            //変更後
            int num31 = -num17 + ((num26 == 0) ? (flag3 ? 4 : 4) : ((num26 - cutRow) * 40 + 20));
            //////////////////////////////////////////////////////////////////////////////
            num31 /= 2;
            num31 *= 2;
            if (num29 < num8)
            {
                num29 = num8;
            }
            num29 /= 2;
            num29 *= 2;
            num18 /= 2;
            num18 *= 2;
            __instance.trans.sizeDelta = new Vector2((float)num29, (float)(num31 + num18));
            __instance.trans.SetParent(UIRoot.instance.itemTipTransform, true);
            Rect rect = UIRoot.instance.itemTipTransform.rect;
            float num32 = (float)Mathf.RoundToInt(rect.width);
            float num33 = (float)Mathf.RoundToInt(rect.height);
            float num34 = __instance.trans.anchorMin.x * num32 + __instance.trans.anchoredPosition.x;
            float num35 = __instance.trans.anchorMin.y * num33 + __instance.trans.anchoredPosition.y;
            Rect rect2 = __instance.trans.rect;
            rect2.x += num34;
            rect2.y += num35;
            Vector2 zero = Vector2.zero;
            if (rect2.xMin < 0f)
            {
                zero.x -= rect2.xMin;
            }
            if (rect2.yMin < 0f)
            {
                zero.y -= rect2.yMin;
            }
            if (rect2.xMax > num32)
            {
                zero.x -= rect2.xMax - num32;
            }
            if (rect2.yMax > num33)
            {
                zero.y -= rect2.yMax - num33;
            }
            __instance.trans.anchoredPosition = __instance.trans.anchoredPosition + zero;
            __instance.trans.anchoredPosition = new Vector2((float)((int)__instance.trans.anchoredPosition.x), (float)((int)__instance.trans.anchoredPosition.y));
            __instance.trans.localScale = new Vector3(1f, 1f, 1f);
            return false;
        }

        //レシピをチェックしてLIST作成
        [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        public static void VFPreload_InvokeOnLoadWorkEnded_Patch()
        {
            if (recipeDictionary.Count == 0)
            {
                for (int h = 0; h < LDB.items.Length; h++)
                {
                    ItemProto itemProto = LDB.items.dataArray[h];

                    recipeStruct recipestruct = new recipeStruct();
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
                                recipestruct.ResultsCount++;
                                Array.Resize(ref recipestruct.Results, recipestruct.ResultsCount);
                                recipestruct.Results[recipestruct.ResultsCount - 1] = recipeProto.ID;
                            }
                        }
                        for (int j = 0; j < recipeProto.Items.Length; j++)
                        {
                            if (recipeProto.Items[j] == itemProto.ID)
                            {
                                recipestruct.ItemsCount++;
                                Array.Resize(ref recipestruct.Items, recipestruct.ItemsCount);
                                recipestruct.Items[recipestruct.ItemsCount - 1] = recipeProto.ID;
                            }
                        }
                    }
                    recipeDictionary.Add(itemProto.ID, recipestruct);
                }
                LogManager.Logger.LogInfo($"Recipe Dictionary created");
            }
        }
    }

    public class LogManager
    {
        public static ManualLogSource Logger;
    }

}