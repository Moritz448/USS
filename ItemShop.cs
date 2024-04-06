using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MSCLoader;
using System.Collections;
using System.Linq;

namespace UniversalShoppingSystem
{
    public class ItemShop : MonoBehaviour
    {
        public delegate void ShopEvent();

        // Settings to change in Unity
        [Header("Shop Settings")]
        public string ShopID = "Unique ID for Save/Load management";
        public string ItemName = "Shop Item Name";
        public float ItemPrice;
        [Header("Relative to store_inside")]
        public Vector3 TriggerPosition;
        public Vector3 TriggerRotation;

        public GameObject ItemPrefab;
        public bool SpawnInBag;

        // After here nothing is important for the setup in unity, so the following fields are not included in the mini dll
        public List<GameObject> BoughtItems = new List<GameObject>(); // Used for saving, no need to change in inspector!

        public event ShopEvent OnBuy;
        public event ShopEvent OnBagTakeout;
        public event ShopEvent OnRestock;

        private Vector3 bigItemSpawnPosition = new Vector3(-1551.303f, 4.88f, 1181.904f);

        public int Stock;
        public int Cart;

        private int itemsBought;

        private PlayMakerFSM register; // Required to hook paying mechanics
        private PlayMakerFSM registerData;
        private GameObject vanillaShopInventory; // Required to hook restock mechanics

        public void SaveShop(Mod mod)
        {
            // SHOP SAVING
            List<bool> shopItemsActive = new List<bool>();
            for (int i = 0; i < transform.childCount; i++) shopItemsActive.Add(transform.GetChild(i).gameObject.activeInHierarchy);

            SaveLoad.WriteValue(mod, $"USS_{ShopID}_shopItemsActive", shopItemsActive);
            SaveLoad.WriteValue(mod, $"USS_{ShopID}_stock", Stock + Cart);
            SaveLoad.WriteValue(mod, $"USS_{ShopID}_itemsBought", itemsBought);

            // BOUGHT ITEMS SAVING
            List<Vector3> pos = new List<Vector3>();
            List<Quaternion> rot = new List<Quaternion>();
            List<bool> inBag = new List<bool>();
            List<string> bagID = new List<string>();
            List<float> condition = new List<float>();

            foreach (GameObject obj in BoughtItems)
            {
                USSItem itm = obj.GetComponent<USSItem>();
                pos.Add(obj.transform.position);
                rot.Add(obj.transform.rotation);
                inBag.Add(itm.InBag);
                bagID.Add(itm.BagID);
                condition.Add(itm.Condition);
            }

            SaveLoad.WriteValue<Vector3>(mod, $"USS_{ShopID}_pos", pos);
            SaveLoad.WriteValue<Quaternion>(mod, $"USS_{ShopID}_rot", rot);
            SaveLoad.WriteValue<bool>(mod, $"USS_{ShopID}_inBag", inBag);
            SaveLoad.WriteValue<string>(mod, $"USS_{ShopID}_bagID", bagID);
            SaveLoad.WriteValue<float>(mod, $"USS_{ShopID}_condition", condition);
        }

        public void LoadShop(Mod mod)
        {
            if (SaveLoad.ValueExists(mod, $"USS_{ShopID}_stock"))
            {
                // SHOP LOADING
                List<bool> shopItemsActive = SaveLoad.ReadValueAsList<bool>(mod, $"USS_{ShopID}_shopItemsActive");
                for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(shopItemsActive[i]);

                Stock = SaveLoad.ReadValue<int>(mod, $"USS_{ShopID}_stock");
                itemsBought = SaveLoad.ReadValue<int>(mod, $"USS_{ShopID}_itemsBought");


                // BOUGHT ITEMS LOADING
                List<Vector3> pos = SaveLoad.ReadValueAsList<Vector3>(mod, $"USS_{ShopID}_pos");
                List<Quaternion> rot = SaveLoad.ReadValueAsList<Quaternion>(mod, $"USS_{ShopID}_rot");
                List<bool> inBag = SaveLoad.ReadValueAsList<bool>(mod, $"USS_{ShopID}_inBag");
                List<string> bagID = SaveLoad.ReadValueAsList<string>(mod, $"USS_{ShopID}_bagID");
                List<float> condition = SaveLoad.ReadValueAsList<float>(mod, $"USS_{ShopID}_condition");

                List<GameObject> shoppingBags = (from gameObject in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>()
                                                 where gameObject.name.Contains("shopping bag") && gameObject.GetComponent<PlayMakerFSM>() != null
                                                 select gameObject).ToList();

                for (int i = 0; i < pos.Count; i++)
                {
                    GameObject obj = GameObject.Instantiate(ItemPrefab);
                    USSItem itm = obj.GetComponent<USSItem>();

                    obj.transform.position = pos[i];
                    obj.transform.rotation = rot[i];
                    itm.InBag = inBag[i];
                    itm.BagID = bagID[i];
                    itm.Condition = condition[i];

                    obj.SetActive(false);

                    if (itm.InBag)
                    {
                        GameObject bag = shoppingBags.FirstOrDefault((GameObject select) => select.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmString("ID").Value == itm.BagID);
                        if (bag == null)
                        {
                            ModConsole.LogWarning("UniversalShoppingSystem: Couldnt find bag; Spawning item outside.");
                            obj.SetActive(true);
                            continue;
                        }

                        USSBagInventory baginv = bag.GetComponent<USSBagInventory>();
                        if (baginv == null)
                        {
                            baginv = bag.AddComponent<USSBagInventory>();
                            baginv.BagContent = new List<GameObject> { obj };
                            USSBagSetupOpenAction act = bag.AddComponent<USSBagSetupOpenAction>();
                            act.bag = bag;
                            act.inv = baginv;
                        }
                        else baginv.BagContent.Add(obj);
                    }

                    else obj.SetActive(true);
                    BoughtItems.Add(obj); // Keep the item tracked for next save
                }
            }
        }

        public class CreateBagAction : FsmStateAction
        {
            public ItemShop shop;
            public FsmInt check;

            public override void OnEnter()
            {
                if (shop.Cart > 0) if (check.Value == 0) Fsm.Event("FINISHED");
                Finish();
            }
        }

        public class RestockAction : FsmStateAction
        {
            public ItemShop shop;
            public override void OnEnter()
            {
                shop.Restock();
                Finish();
            }
        }

        private void Awake()
        {
            if (ShopID == "Unique ID for Save/Load management") ModConsole.Error("UniversalShoppingSystem: ShopID of " + ItemName + " is still default!");

            if (!GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>()) GameObject.Find("PLAYER").AddComponent<ItemShopRaycast>();
            GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>().Shops.Add(this); // Add shop for commands etc

            register = GameObject.Find("STORE/StoreCashRegister/Register").GetComponent<PlayMakerFSM>();
            vanillaShopInventory = GameObject.Find("STORE/Inventory");
            registerData = GameObject.Find("StoreCashRegister").transform.GetChild(2).GetPlayMaker("Data");
            registerData.InitializeFSM();

            GameHook.InjectStateHook(register.gameObject, "Purchase", () => { Pay(); });
            vanillaShopInventory.GetPlayMaker("Logic").GetState("Items").InsertAction(0, new RestockAction { shop = this });

            this.transform.SetParent(GameObject.Find("STORE").transform.Find("LOD").transform.Find("GFX_Store").transform.Find("store_inside"), false);
            this.transform.localEulerAngles = TriggerRotation;
            this.transform.localPosition = TriggerPosition;
            this.transform.SetParent(null, true); // Needs to be parented to root in order to stay active all the time to not cause problems with restocking; Might change.

            Stock = this.transform.childCount;
            Cart = 0;
            itemsBought = 0;
            Restock(); // Might change later for more convenient save loading

            if (SpawnInBag) // Only setup the whole stuff when items should spawn in bags
            {
                // Bag Spawning Setup
                GameObject store = GameObject.Find("STORE");
                PlayMakerFSM fsm = store.transform.Find("LOD/ShopFunctions/BagCreator").GetPlayMaker("Create");
                fsm.InitializeFSM();
                fsm.GetState("Copy contents").InsertAction(0, new USSBagSetupAction
                {
                    bag = (fsm.GetState("Copy contents").Actions.First(action => action is ArrayListCopyTo) as ArrayListCopyTo).gameObjectTarget.GameObject,
                    shop = this
                });
                // Abusing the oil filter shop for our purposes
                registerData.GetState("Oil filter").InsertAction(0, new CreateBagAction
                {
                    shop = this,
                    check = registerData.FsmVariables.FindFsmInt("QOilfilter")
                });
            }
        }
        
        public void TookOutOfBag() => OnBagTakeout?.Invoke();
        
        private void Restock()
        {
            StartCoroutine(RestockShop());
            itemsBought = 0;
            Stock = this.gameObject.transform.childCount;
            OnRestock?.Invoke(); // Run user-provided actions
        }

        private IEnumerator RestockShop()
        {
            for (int i = 0; i < this.gameObject.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(true); 
            yield break;
        }

        public void SpawnBag(USSBagInventory inv)
        {
            if (this.SpawnInBag) StartCoroutine(BagSpawner(inv));
        }

        private IEnumerator BagSpawner(USSBagInventory invent)
        {
            yield return new WaitForSeconds(0.31f);
            itemsBought += Cart;
            while (Cart > 0)
            {
                Cart--;
                GameObject item = GameObject.Instantiate(ItemPrefab);
                item.SetActive(false);
                invent.BagContent.Add(item);
                BoughtItems.Add(item); // For Saving/Loading
                USSItem ussitm = item.GetComponent<USSItem>();
                ussitm.InBag = true;
                ussitm.OriginShop = this;
                yield return null;
            }

            yield break;
        }

        public void Buy() // Put item in Cart
        {
            Stock--;
            Cart++;

            this.transform.GetChild(Cart - 1 + itemsBought).gameObject.SetActive(false);

            register.FsmVariables.GetFsmFloat("PriceTotal").Value += ItemPrice;
            register.SendEvent("PURCHASE");
        }

        public void Unbuy() // Take item back out of Cart
        {
            this.transform.GetChild(Cart - 1 + itemsBought).gameObject.SetActive(true);
            Stock++;
            Cart--;

            register.FsmVariables.GetFsmFloat("PriceTotal").Value -= ItemPrice;
            register.SendEvent("PURCHASE");
        }

        private void Pay()
        {
            if (this.Cart > 0) OnBuy?.Invoke();
            
            if (!this.SpawnInBag) // When its a big item...
            {
                for (int i = 0; i < Cart; i++)
                {
                    GameObject item = GameObject.Instantiate(ItemPrefab);
                    this.BoughtItems.Add(item);
                    item.transform.position = bigItemSpawnPosition;
                    item.GetComponent<Rigidbody>().isKinematic = false;
                    item.SetActive(true);
                }
                itemsBought += Cart;
                Cart = 0;
            }
        }
    }
}