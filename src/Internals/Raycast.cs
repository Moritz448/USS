#if !MINI
using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using System.Collections.Generic;
using System;
using System.Collections;

namespace UniversalShoppingSystem;

public class ItemShopRaycast : MonoBehaviour
{
    public List<ItemShop> Shops = [];

    private static FsmBool _guiBuy, storeOpen;
    private static FsmString _guiText;

    private bool cartIconShowing;

    private RaycastHit hit;
    
    private IEnumerator ToggleESBool() // Due to load order depending on the dll names (alphabetical order), the script needs to wait until ExpandedShop instantiated its components
    {
        GameObject es;
        while ((es = GameObject.Find("STORE/TeimoDrinksMod(Clone)")) == null) yield return new WaitForSeconds(1f);
        Type RaycastType = Type.GetType("ExpandedShop.ShopRaycast, ExpandedShop");
        es.GetComponent(RaycastType)?.GetType()?.GetField("ApplyFsmBool")?.SetValue(RaycastType, null);
        yield break;
    }

    private void Awake()
    {
        _guiBuy = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIbuy");
        _guiText = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        
        if (ModLoader.CurrentGame == Game.MySummerCar) storeOpen = GameObject.Find("STORE").GetPlayMaker("OpeningHours").FsmVariables.FindFsmBool("OpenStore");
        USSItem.fridges = [
            GameObject.Find("YARD")?.transform.Find("Building/KITCHEN/Fridge/FridgePoint/ChillArea1"),
            ModLoader.CurrentGame == Game.MyWinterCar ? GameObject.Find("HOMENEW")?.transform.Find("Functions/Fridge/FridgePoint/ChillArea2") : null,
            ModLoader.CurrentGame == Game.MyWinterCar ? GameObject.Find("JOBS")?.transform.Find("FACTORY/Mesh/Kitchen/Fridge/FridgePoint/ChillArea3"): null
        ];
    }

    private void Start()
    {
        if (ModLoader.CurrentGame == Game.MySummerCar && ModLoader.IsModPresent("ExpandedShop")) StartCoroutine(ToggleESBool());
            
        // Check for duplicate Shop IDs, which would cause the save/load system to malfunction
        HashSet<string> shopIDs = [];
        foreach (ItemShop shop in Shops) if (!shopIDs.Add(shop.ShopID)) ModConsole.LogError($"[USS] ShopID {shop.ShopID} is not unique!");
    }

    private void Update()
    {
        if (ModLoader.CurrentGame == Game.MySummerCar) if (!storeOpen.Value) return;

        bool lmb = Input.GetMouseButtonDown(0);
        bool rmb = Input.GetMouseButtonDown(1);

        if (!string.IsNullOrEmpty(UnifiedRaycast.GetHitName()))
        {
            hit = UnifiedRaycast.GetRaycastHit();

            if (ItemShop.ShopLookup.TryGetValue(hit.collider, out ItemShop shop))
            {
                cartIconShowing = _guiBuy.Value = true;
                _guiText.Value = $"{shop.ItemName} {shop.ItemPrice} mk";
                if (lmb && shop.Stock > 0) shop.Buy();
                else if (rmb && shop.Cart > 0) shop.Unbuy();
            }
        }
        else if (cartIconShowing)
        {
            cartIconShowing = _guiBuy.Value = false;
            _guiText.Value = String.Empty;
        }
    }
}
#endif