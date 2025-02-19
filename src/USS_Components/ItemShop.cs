using UnityEngine;
using System;


#if !MINI
using MSCLoader;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#endif

namespace UniversalShoppingSystem;

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

#if !MINI
    // FOLLOWING NOT NEEDED FOR UNITY SETUP; THEREFORE NOT INCLUDED IN MINI DLL

    public List<GameObject> BoughtItems = new List<GameObject>(); // Used for saving, no need to change in inspector!

    public event ShopEvent OnBuy;
    public event ShopEvent OnRestock;

    public int Stock;
    public int Cart;
    
    private Vector3 bigItemSpawnPosition = new Vector3(-1551.303f, 4.88f, 1181.904f);

    private int itemsBought;

    private PlayMakerFSM register;           // Required to hook paying mechanics
    private PlayMakerFSM registerData;       // Required to hook bag creation mechanics
    private GameObject vanillaShopInventory; // Required to hook restock mechanics


    /// <summary>
    /// Save the shop and all bought items
    /// </summary>
    /// <param name="mod">Mod Class the method is called from</param>
    public void SaveShop(Mod mod)
    {
        // SHOP SAVING
        try
        {
            int childCount = transform.childCount;
            List<bool> activeItems = new List<bool>(childCount);

            for (int i = 0; i < childCount; i++) activeItems.Add(transform.GetChild(i).gameObject.activeInHierarchy);

            ShopSave saveData = new ShopSave(activeItems, Stock + Cart, itemsBought);
            SaveLoad.SerializeClass(mod, saveData, $"USS_{ShopID}");
        }
        catch (Exception ex) { ModConsole.LogError($"[USS] Failed to save shop: {ex.Message}\n{ex.StackTrace}"); }

        // BOUGHT ITEMS SAVING
        try
        {
            int itemCount = BoughtItems.Count;

            List<Vector3> pos = new List<Vector3>(itemCount);
            List<Vector3> rot = new List<Vector3>(itemCount);
            List<bool> inBag = new List<bool>(itemCount);
            List<string> bagID = new List<string>(itemCount);
            List<float> condition = new List<float>(itemCount);

            foreach (GameObject obj in BoughtItems)
            {
                USSItem itm = obj.GetComponent<USSItem>();
                if (itm != null)
                {
                    pos.Add(obj.transform.position);
                    rot.Add(obj.transform.eulerAngles);
                    inBag.Add(itm.InBag);
                    bagID.Add(itm.BagID);
                    condition.Add(itm.Condition);
                }
                else ModConsole.LogWarning($"[USS] Object {obj.name} is missing USSItem component.");
                
            }

            ItemSave saveData = new ItemSave(pos, rot, inBag, bagID, condition);
            SaveLoad.SerializeClass(mod, saveData, $"USS_{ShopID}_items");
        }
        catch (Exception ex) { ModConsole.LogError($"[USS] Failed to save {ShopID} items: {ex.Message}\n{ex.StackTrace}"); }
    }

    /// <summary>
    /// Load shop and all bought items
    /// </summary>
    /// <param name="mod">Mod class the method is called from</param>
    public void LoadShop(Mod mod)
    {
        if (SaveLoad.ValueExists(mod, $"USS_{ShopID}"))
        {
            // SHOP LOADING
            ShopSave shopData = SaveLoad.DeserializeClass<ShopSave>(mod, $"USS_{ShopID}");
            if (shopData != null && shopData.ActiveItems != null)
            {
                for (int i = 0; i < Mathf.Min(transform.childCount, shopData.ActiveItems.Count); i++) transform.GetChild(i).gameObject.SetActive(shopData.ActiveItems[i]);
                Stock = shopData.Stock;
                itemsBought = shopData.ItemsBought;
            }
            else ModConsole.LogError($"[USS] Failed to load shop data for {ShopID}: Save data is null or corrupted.");

            // ITEMS LOADING
            if (SaveLoad.ValueExists(mod, $"USS_{ShopID}_items"))
            {
                ItemSave itemData = SaveLoad.DeserializeClass<ItemSave>(mod, $"USS_{ShopID}_items");
                List<GameObject> shoppingBags = (from gameObject in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>()
                                                 where gameObject.name.Contains("shopping bag") && gameObject.GetComponent<PlayMakerFSM>() != null
                                                 select gameObject).ToList();

                int itemCount = itemData.Position.Count;
                if (itemData.Rotation.Count != itemCount || itemData.InBag.Count != itemCount || itemData.BagID.Count != itemCount || itemData.Condition.Count != itemCount)
                {
                    ModConsole.LogError($"[USS] Item save data mismatch for {ShopID}. Skipping item loading.");
                    return;
                }

                for (int i = 0; i < itemCount; i++)
                {
                    GameObject obj = GameObject.Instantiate(ItemPrefab);
                    USSItem itm = obj.GetComponent<USSItem>();

                    obj.transform.position = itemData.Position[i];
                    obj.transform.eulerAngles = itemData.Rotation[i];

                    itm.InBag = itemData.InBag[i];
                    itm.BagID = itemData.BagID[i];
                    itm.Condition = itemData.Condition[i];

                    obj.SetActive(false);

                    if (itm.InBag)
                    {
                        GameObject bag = FindBagByID(shoppingBags, itm.BagID);
                        if (bag == null)
                        {
                            ModConsole.Log("[USS]: Couldn't find bag; Spawning item outside.");
                            obj.SetActive(true);
                            itm.StartSpoiling();
                            continue;
                        }

                        AddItemToBag(bag, obj);
                    }
                    else
                    {
                        obj.SetActive(true);
                        itm.StartSpoiling();
                    }

                    BoughtItems.Add(obj); // Keep the item tracked for next save
                }
            }
        }
        // BACKWARDS COMPATIBILITY FOR PRE-
        else if (SaveLoad.ValueExists(mod, $"USS_{ShopID}_stock"))
        {
            ModConsole.Log($"[USS] Found old save data for shop {ShopID}; Loading with fallback.");

            // SHOP LOADING
            List<bool> shopItemsActive = SaveLoad.ReadValueAsList<bool>(mod, $"USS_{ShopID}_shopItemsActive");

            if (shopItemsActive == null || shopItemsActive.Count == 0) ModConsole.Log($"[USS] No saved active shop items found for {ShopID}. Defaulting all to active.");

            for (int i = 0; i < Mathf.Min(transform.childCount, shopItemsActive.Count); i++) transform.GetChild(i).gameObject.SetActive(shopItemsActive[i]);

            for (int i = shopItemsActive.Count; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(true); // Default any extra children to active if save data is incomplete

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

            if (rot.Count != pos.Count || inBag.Count != pos.Count || bagID.Count != pos.Count || condition.Count != pos.Count)
            {
                ModConsole.LogError($"[USS] Item save data mismatch for {ShopID}. Skipping item loading.");
                return;
            }

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
                    GameObject bag = FindBagByID(shoppingBags, itm.BagID);

                    if (bag == null)
                    {
                        ModConsole.LogWarning("[USS] Couldn't find bag; Spawning item outside.");
                        obj.SetActive(true);
                        itm.StartSpoiling();
                        continue;
                    }
                    AddItemToBag(bag, obj);
                }
                else
                {
                    obj.SetActive(true);
                    itm.StartSpoiling();
                }
                BoughtItems.Add(obj); // Keep the item tracked for next save
            }
        }
    }

    private GameObject FindBagByID(List<GameObject> bags, string bagID)
    {
        return bags.FirstOrDefault(bag =>
        {
            PlayMakerFSM fsm = bag.GetComponent<PlayMakerFSM>();
            return fsm != null && fsm.FsmVariables.FindFsmString("ID").Value == bagID;
        });
    }

    private void AddItemToBag(GameObject bag, GameObject obj)
    {
        USSBagInventory bagInventory = bag.GetComponent<USSBagInventory>();

        if (bagInventory == null)
        {
            bagInventory = bag.AddComponent<USSBagInventory>();
            bagInventory.BagContent = new List<GameObject> { obj };

            USSBagSetupOpenAction act = bag.AddComponent<USSBagSetupOpenAction>();
            act.Bag = bag;
            act.BagInventory = bagInventory;
        }
        else bagInventory.BagContent.Add(obj);
        
    }

    private class CreateBagAction : FsmStateAction
    {
        public ItemShop Shop;
        public FsmInt Check;

        public override void OnEnter()
        {
            if (Shop.Cart > 0) if (Check.Value == 0) Fsm.Event("FINISHED");
            Finish();
        }
    }

    private class RestockAction : FsmStateAction
    {
        public ItemShop shop;
        public override void OnEnter()
        {
            shop.Restock();
            Finish();
        }
    }

    private void Start()
    {
        if (ShopID == "Unique ID for Save/Load management") ModConsole.Error("[USS]: ShopID of " + ItemName + " is still default!");

        /*GameObject player = GameObject.Find("PLAYER");
        ItemShopRaycast itemShopRaycast = player.GetComponent<ItemShopRaycast>() ?? player.AddComponent<ItemShopRaycast>();
        itemShopRaycast.Shops.Add(this); // Register shop for commands, unique id check etc.

        GameObject store = GameObject.Find("STORE");
        Transform storeCashRegister = store.transform.Find("StoreCashRegister");

        vanillaShopInventory = store.transform.Find("Inventory").gameObject;

        register = storeCashRegister.GetComponent<PlayMakerFSM>();
        registerData = store.transform.Find("StoreCashRegister").transform.GetChild(2).GetPlayMaker("Data");
        registerData.InitializeFSM();

        // Hook into FSM states
        GameHook.InjectStateHook(register.gameObject, "Purchase", Pay);
        vanillaShopInventory.GetPlayMaker("Logic").GetState("Items").InsertAction(0, new RestockAction { shop = this });

        transform.SetParent(store.transform.Find("LOD/StoreInside"), false);
        transform.localEulerAngles = TriggerRotation;
        transform.localPosition = TriggerPosition;
        transform.SetParent(null, true); // Needs to be parented to root in order to stay active for restocking

        // Initialize shop data
        Stock = transform.childCount;
        Cart = itemsBought = 0;
        Restock(); // Fully restock, save data overrides the values

        if (SpawnInBag) SetupBagSpawning(store);*/


        if (!GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>()) GameObject.Find("PLAYER").AddComponent<ItemShopRaycast>();
        GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>().Shops.Add(this); // Add shop for commands, unique id check etc.

        register = GameObject.Find("STORE/StoreCashRegister/Register").GetComponent<PlayMakerFSM>();
        vanillaShopInventory = GameObject.Find("STORE/Inventory");
        registerData = GameObject.Find("StoreCashRegister").transform.GetChild(2).GetPlayMaker("Data");
        registerData.InitializeFSM();

        GameHook.InjectStateHook(register.gameObject, "Purchase", () => { Pay(); });
        vanillaShopInventory.GetPlayMaker("Logic").GetState("Items").InsertAction(0, new RestockAction { shop = this }); // Inject paying and restock mechanics

        transform.SetParent(GameObject.Find("STORE").transform.Find("LOD").transform.Find("GFX_Store").transform.Find("store_inside"), false);
        transform.localEulerAngles = TriggerRotation;
        transform.localPosition = TriggerPosition;
        transform.SetParent(null, true); // Needs to be parented to root in order to stay active all the time to not cause problems with restocking

        Stock = transform.childCount;
        Cart = itemsBought = 0;
        Restock(); // Fully restock, save data overrides the values

        if (SpawnInBag) // Only setup the whole stuff when items should spawn in bags
        {
            // Bag Spawning Setup
            GameObject store = GameObject.Find("STORE");
            PlayMakerFSM fsm = store.transform.Find("LOD/ShopFunctions/BagCreator").GetPlayMaker("Create");
            fsm.InitializeFSM();
            fsm.GetState("Copy contents").InsertAction(0, new USSBagSetupAction
            {
                Bag = (fsm.GetState("Copy contents").Actions.First(action => action is ArrayListCopyTo) as ArrayListCopyTo).gameObjectTarget.GameObject,
                Shop = this
            });
            // Abusing the oil filter shop for our purposes
            registerData.GetState("Oil filter").InsertAction(0, new CreateBagAction
            {
                Shop = this,
                Check = registerData.FsmVariables.FindFsmInt("QOilfilter")
            });
        }
    }

    private void SetupBagSpawning(GameObject store)
    {
        // Bag Spawning Setup
        PlayMakerFSM bagCreator = store.transform.Find("LOD/ShopFunctions/BagCreator").GetPlayMaker("Create");
        bagCreator.InitializeFSM();
        bagCreator.GetState("Copy contents").InsertAction(0, new USSBagSetupAction
        {
            Bag = (bagCreator.GetState("Copy contents").Actions.First(action => action is ArrayListCopyTo) as ArrayListCopyTo).gameObjectTarget.GameObject,
            Shop = this
        });
        // Abusing the oil filter shop for our purposes
        registerData.GetState("Oil filter").InsertAction(0, new CreateBagAction
        {
            Shop = this,
            Check = registerData.FsmVariables.FindFsmInt("QOilfilter")
        });
    }

    private void Restock()
    {
        StartCoroutine(RestockShop());
        itemsBought = 0;
        Stock = transform.childCount;
        OnRestock?.Invoke(); // Run user-provided actions
    }

    private IEnumerator RestockShop()
    {
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(true); 
        yield break;
    }

    internal void SpawnBag(USSBagInventory BagInventory)
    {
        if (this.SpawnInBag) StartCoroutine(BagSpawner(BagInventory));
    }

    private IEnumerator BagSpawner(USSBagInventory BagInventoryent)
    {
        yield return new WaitForSeconds(0.31f); // wait slightly longer than expandedshop
        itemsBought += Cart;
        while (Cart > 0)
        {
            Cart--;

            GameObject item = GameObject.Instantiate(ItemPrefab);
            item.SetActive(false);
            BagInventoryent.BagContent.Add(item);
            BoughtItems.Add(item); // Track item for saving/loading

            USSItem ussitm = item.GetComponent<USSItem>();
            ussitm.InBag = true;
            ussitm.OriginShop = this;
            yield return null;
        }
        yield break;
    }

    /// <summary>
    /// Add one item to the cart
    /// </summary>
    internal void Buy() // Put item in Cart
    {
        Stock--;
        Cart++;

        this.transform.GetChild(Cart - 1 + itemsBought).gameObject.SetActive(false);

        register.FsmVariables.GetFsmFloat("PriceTotal").Value += ItemPrice;
        register.SendEvent("PURCHASE");
    }


    /// <summary>
    /// Remove one item from the cart
    /// </summary>
    internal void Unbuy() // Take item back out of Cart
    {
        this.transform.GetChild(Cart - 1 + itemsBought).gameObject.SetActive(true);
        Stock++;
        Cart--;

        register.FsmVariables.GetFsmFloat("PriceTotal").Value -= ItemPrice;
        register.SendEvent("PURCHASE");
    }

    private void Pay() // Called on checkout at register
    {
        if (this.Cart > 0) OnBuy?.Invoke();
        
        if (!this.SpawnInBag) // When its a big item run this, if it spawns in bag SpawnBag gets called
        {
            for (int i = 0; i < Cart; i++)
            {
                GameObject item = GameObject.Instantiate(ItemPrefab);
                BoughtItems.Add(item);
                item.transform.position = bigItemSpawnPosition;
                item.SetActive(true);
            }
            itemsBought += Cart;
            Cart = 0;
        }
    }
#endif
}