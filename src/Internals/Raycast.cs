#if !MINI
using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using System.Collections.Generic;
using System;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using System.Net;

namespace UniversalShoppingSystem;

public class ItemShopRaycast : MonoBehaviour
{
    public List<ShopBase> Shops = [];

    private static FsmBool _guiBuy, storeOpen;
    private static FsmString _guiText;

    private bool cartIconShowing;

    private RaycastHit hit;

    private float fleamarketLastSound = -Mathf.Infinity;


    private IEnumerator ToggleESBool() // Due to load order depending on the dll names (alphabetical order), the script needs to wait until ExpandedShop instantiated its components
    {
        GameObject es;
        while ((es = GameObject.Find("STORE/TeimoDrinksMod(Clone)")) == null) yield return new WaitForSeconds(1f);
        Type RaycastType = Type.GetType("ExpandedShop.ShopRaycast, ExpandedShop");
        es.GetComponent(RaycastType)?.GetType()?.GetField("ApplyFsmBool")?.SetValue(RaycastType, null);
        yield break;
    }

    internal static IEnumerator DeleteVanillaStuff(FleamarketShop shop)
    {
        yield return new WaitForEndOfFrame();

        if (!shop.isActiveAndEnabled)
        {
            yield return new WaitForSeconds(5f);
            shop.transform.parent.gameObject.SetActive(true);
        }

        FsmBool randomized = shop.transform.parent.parent.GetPlayMaker("Create").FsmVariables.FindFsmBool("Randomized");

        while (!randomized.Value) yield return new WaitForSeconds(1f);

        foreach (Transform t in shop.transform.parent) if (t != shop.transform) GameObject.Destroy(t.gameObject);
        
        yield return null;
    }

    private void Awake()
    {
        ConsoleCommand.Add(new DebugCommands());

        _guiBuy = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIbuy");
        _guiText = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        
        if (ModLoader.CurrentGame == Game.MySummerCar) storeOpen = GameObject.Find("STORE").GetPlayMaker("OpeningHours").FsmVariables.FindFsmBool("OpenStore");
        USSItem.fridges = [
            GameObject.Find("YARD")?.transform.Find("Building/KITCHEN/Fridge/FridgePoint").Find(ModLoader.CurrentGame == Game.MySummerCar ? "ChillArea" : "ChillArea1"),
            ModLoader.CurrentGame == Game.MyWinterCar ? GameObject.Find("HOMENEW")?.transform.Find("Functions/Fridge/FridgePoint/ChillArea2") : null,
            ModLoader.CurrentGame == Game.MyWinterCar ? GameObject.Find("JOBS")?.transform.Find("FACTORY/Mesh/Kitchen/Fridge/FridgePoint/ChillArea3"): null
        ];

        if (ModLoader.CurrentGame == Game.MyWinterCar)
        {
            Transform lod = GameObject.Find("FleaMarket").transform.Find("LOD");
            bool lodDisabled = lod.gameObject.activeInHierarchy;
            if (lodDisabled) lod.gameObject.SetActive(true);

            PlayMakerFSM fleamarketRegisterFsm = lod.Find("FleaCashRegister/CashRegisterLogic").GetPlayMaker("Data");
            fleamarketRegisterFsm.InitializeFSM();
            FsmState state2 = fleamarketRegisterFsm.GetState("State 2");
            Transform speak = GameObject.Find("FleaMarket").transform.Find("LOD/OpenHours/Cashier/Pivot/Speak");
            FsmString subtitle = FsmVariables.GlobalVariables.FindFsmString("GUIsubtitle");

            fleamarketRegisterFsm.FsmInject("Wait button", delegate
            {
                if (Time.time - fleamarketLastSound >= 120f)
                {
                    MasterAudio.StopAllOfSound("Kirpparimummo");
                    MasterAudio.PlaySound3DAndForget("Kirpparimummo", speak, true, variationName: "payment");
                    subtitle.Value = "Payment in Finnish marks please. Not in the nature...";
                    fleamarketLastSound = Time.time;
                }
            }, index: 4);

            state2.RemoveAction(3);
            state2.RemoveAction(3);
            state2.RemoveAction(2);

            if (lodDisabled) lod.gameObject.SetActive(false);
        }

    }

    private void Start()
    {
        FleamarketRestock.Reset();
        if (ModLoader.CurrentGame == Game.MySummerCar && ModLoader.IsModPresent("ExpandedShop")) StartCoroutine(ToggleESBool());
            
        // Check for duplicate Shop IDs, which would cause the save/load system to malfunction
        HashSet<string> shopIDs = [];
        foreach (ShopBase shop in Shops) if (!shopIDs.Add(shop.ShopID)) ModConsole.LogError($"[USS] ShopID {shop.ShopID} is not unique!");
    }

    private void Update()
    {
        if (ModLoader.CurrentGame == Game.MySummerCar) if (!storeOpen.Value) return;

        bool lmb = Input.GetMouseButtonDown(0);
        bool rmb = Input.GetMouseButtonDown(1);

        if (!string.IsNullOrEmpty(UnifiedRaycast.GetHitName()))
        {
            hit = UnifiedRaycast.GetRaycastHit();

            if (ItemShop.ShopLookup.TryGetValue(hit.collider, out ShopBase shop))
            {
                if (!(shop is FleamarketShop fleamarketShop && fleamarketShop.Stock == 0))
                {
                    cartIconShowing = _guiBuy.Value = true;
                    _guiText.Value = $"{shop.ItemName} {shop.GetItemPrice():0.##} mk";
                    if (lmb && shop.Stock > 0) shop.Buy();
                    else if (rmb && shop.Cart > 0) shop.Unbuy();
                }
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