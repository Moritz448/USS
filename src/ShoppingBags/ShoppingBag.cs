#if !MINI
using UnityEngine;
using MSCLoader;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;


namespace UniversalShoppingSystem;

internal class USSBagInventory : MonoBehaviour
{
    public List<GameObject> BagContent = [];
    
    //private static readonly bool ESPresent = ModLoader.IsModPresent("ExpandedShop");
    //private static readonly Type ModShopBagInvType = ESPresent ? Type.GetType("ExpandedShop.ModShopBagInv, ExpandedShop") : null;
    //private static readonly Type ModItemType = ESPresent ? Type.GetType("ExpandedShop.ModItem, ExpandedShop") : null;

    private void Start() => StartCoroutine(InitiateBag());

    //private void TakeESOver() // Copies ExpandedShop items over to USS bag inventory
    //{
    //    if (!ESPresent || ModShopBagInvType == null) return;
    //    Component es = gameObject.GetComponent(ModShopBagInvType);
    //    if (es == null) return;

    //    FieldInfo shoplistField = ModShopBagInvType.GetField("shoplist");

    //    if (shoplistField?.GetValue(es) is not List<GameObject> shoplist || shoplist.Count == 0) return;

    //    BagContent.AddRange(shoplist);

    //    for (int i = 0; i < BagContent.Count; i++)
    //    {
    //        if (ModItemType != null)
    //        {
    //            Component moditm = BagContent[i].GetComponent(ModItemType);
    //            if (moditm != null)
    //            {
    //                ModItemType.GetField("BagID")?.SetValue(moditm, gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value);
    //                ModItemType.GetField("BagCountInt")?.SetValue(moditm, i);
    //            }
    //        }

    //        else if (!BagContent[i].GetComponent<USSItem>())
    //            ModConsole.LogError($"[USS] Found no shop system item behavior on item {i}!");
    //    }

    //    // Clear shoplist after copying
    //    shoplist.Clear();
    //}

    private IEnumerator InitiateBag()
    {
        yield return new WaitForSeconds(0.5f);

        // Set BagID for ever USS item in the bag
        for (int i = 0; i < this.BagContent.Count; i++) if (BagContent[i].GetComponent<USSItem>()) BagContent[i].GetComponent<USSItem>().BagID = this.gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;

        // Take over all ExpandedShop items if ES is loaded
        //if (ESPresent) TakeESOver();

        if (BagContent.Count == 0) Destroy(this);

        yield break;
    }
}
#endif