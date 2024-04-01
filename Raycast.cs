using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace UniversalShoppingSystem
{
    public class ItemShopRaycast : MonoBehaviour
    {

        private Camera fpsCam;
        private RaycastHit hit;

        private FsmBool _guiBuy;
        private FsmString _guiText;
        private FsmBool storeOpen;

        private bool cartIconShowing;

        private void Awake()
        {
            fpsCam = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera").GetComponent<Camera>();
            _guiBuy = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIbuy");
            _guiText = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
            storeOpen = GameObject.Find("STORE").GetPlayMaker("OpeningHours").FsmVariables.FindFsmBool("OpenStore");
        }

        private void Update()
        {
            bool lmb = Input.GetKeyDown(KeyCode.Mouse0);
            bool rmb = Input.GetKeyDown(KeyCode.Mouse1);

            Physics.Raycast(fpsCam.ScreenPointToRay(Input.mousePosition), out hit, 1.35f);

            if (hit.collider.gameObject.GetComponent<ItemShop>())
            {
                if (storeOpen.Value == true)
                {
                    ItemShop shop = hit.collider.gameObject.GetComponent<ItemShop>();
                    cartIconShowing = true;
                    _guiBuy.Value = true;
                    _guiText.Value = $"{shop.ItemName} {shop.ItemPrice} mk";

                    if (lmb && shop.Stock > 0) shop.Buy();
                    else if (rmb && shop.Cart > 0) shop.Unbuy();
                }
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