using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using System.Collections.Generic;

using ExpandedShop;
using System.Linq;
using System.Net;

namespace UniversalShoppingSystem
{
    public class ItemShopRaycast : MonoBehaviour
    {
        public List<ItemShop> Shops = new List<ItemShop>();

        private Camera fpsCam;
        private RaycastHit[] hits;

        private FsmBool _guiBuy;
        private FsmString _guiText;
        private FsmBool storeOpen;

        private bool cartIconShowing;

        private void Awake()
        {
            ConsoleCommand.Add(new USSCommands());
            if (ModLoader.IsModPresent("ExpandedShop")) // Loading up the ES compatibility stuff, including version check
            {

                if (System.Convert.ToDecimal(ModLoader.GetMod("ExpandedShop").Version) < 1.1m)
                {
                    ModUI.ShowCustomMessage("You are using an old version of ExpandedShop which is not compatible with UniversalShoppingSystem. Please update or uninstall ExpandedShop.", "Wrong Version", new MsgBoxBtn[]
                    {
                    ModUI.CreateMessageBoxBtn("I will", () => { }, false)
                    });
                }

                else GameObject.Find("STORE/TeimoDrinksMod(Clone)").GetComponent<ShopRaycast>().ApplyFsmBool = false;
            }

            fpsCam = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera").GetComponent<Camera>();
            _guiBuy = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIbuy");
            _guiText = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
            storeOpen = GameObject.Find("STORE").GetPlayMaker("OpeningHours").FsmVariables.FindFsmBool("OpenStore");

            // Check for duplicate Shop IDs, which would cause the save/load system to malfunction
            List<string> shopIDs = new List<string>();
            foreach (ItemShop shop in Shops)
            {
                if (shopIDs.Contains(shop.ShopID)) ModConsole.LogError("UniversalShoppingSystem: ShopID " + shop.ShopID + " is not unique!");
                else shopIDs.Add(shop.ShopID);
            }
        }

        private void Update()
        {
            if (storeOpen.Value)
            {
                bool lmb = Input.GetKeyDown(KeyCode.Mouse0);
                bool rmb = Input.GetKeyDown(KeyCode.Mouse1);

                hits = Physics.RaycastAll(fpsCam.ScreenPointToRay(Input.mousePosition), 1.35f)
                    .Where(go => go.collider.GetComponent<ItemShop>())
                    .ToArray();


                if (hits != null) foreach (ItemShop shop in hits.Select(hit => hit.collider.GetComponent<ItemShop>()))
                    {
                        cartIconShowing = true;
                        _guiBuy.Value = true;
                        _guiText.Value = $"{shop.ItemName} {shop.ItemPrice} mk";

                        if (lmb && shop.Stock > 0) shop.Buy();
                        else if (rmb && shop.Cart > 0) shop.Unbuy();

                        break;
                    }

                else if (cartIconShowing)
                {
                    cartIconShowing = false;
                    _guiBuy.Value = false;
                    _guiText.Value = "";
                }
            }
        }
    }
}