﻿#if !MINI
using UnityEngine;
using MSCLoader;
using System.Collections;
using System.Collections.Generic;

using ExpandedShop;

namespace UniversalShoppingSystem;

internal class USSBagInventory : MonoBehaviour
{
    public List<GameObject> BagContent = [];
    
    private static readonly bool ESPresent = ModLoader.IsModPresent("ExpandedShop");

    private void Start() => StartCoroutine(InitiateBag());

    private void TakeESOver() // Copys ExpandedShop items over to USS bag inventory
    {
        ModShopBagInv es = this.gameObject.GetComponent<ModShopBagInv>();

        if (es != null && es.shoplist.Count > 0)
        {
            this.BagContent.AddRange(es.shoplist);

            for (int i = 0; i < this.BagContent.Count; i++)
            {
                ModItem moditm = BagContent[i].GetComponent<ModItem>();
                if (moditm != null)
                {
                    moditm.BagID = gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                    moditm.BagCountInt = i;
                }

                else if (!BagContent[i].GetComponent<USSItem>()) ModConsole.LogError($"[USS] Found no shop system item behaviour on item {i}!");
                es.shoplist.Clear();
            }
        }
    }

    private IEnumerator InitiateBag()
    {
        yield return new WaitForSeconds(0.5f);

        // Set BagID for ever USS item in the bag
        for (int i = 0; i < this.BagContent.Count; i++) if (BagContent[i].GetComponent<USSItem>()) BagContent[i].GetComponent<USSItem>().BagID = this.gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;

        // Take over all ExpandedShop items if ES is loaded
        if (ESPresent) TakeESOver();

        if (BagContent.Count == 0) Destroy(this);

        yield break;
    }
}
#endif