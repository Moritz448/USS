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
    public Vector3 TriggerPosition, TriggerRotation;

    public GameObject ItemPrefab;
    public bool SpawnInBag;

#if !MINI
    // FOLLOWING NOT NEEDED FOR UNITY SETUP; THEREFORE NOT INCLUDED IN MINI DLL
    public List<GameObject> BoughtItems = [];

    public event ShopEvent OnBuy, OnRestock;

    public int Stock, Cart;
    private int itemsBought;    

    private Vector3 bigItemSpawnPosition = new Vector3(-1551.303f, 4.88f, 1181.904f);

    private PlayMakerFSM register, registerData;           // Required to hook paying mechanics & bag creation mechanics
    private GameObject vanillaShopInventory; // Required to hook restock mechanics

    internal static Dictionary<Collider, ItemShop> ShopLookup = [];

    private const string ShopSaveKey = "USS_{0}";
    private const string ItemSaveKey = "USS_{0}_items";

    /// <summary>
    /// Save the shop and all bought items
    /// </summary>
    /// <param name="mod">Mod Class the method is called from</param>
    public void SaveShop(Mod mod)
    {
        // SHOP SAVING
        try
        {
            ShopSave saveData = new(Stock + Cart, itemsBought);
            SaveLoad.SerializeClass(mod, saveData, String.Format(ShopSaveKey, ShopID));
        }
        catch (Exception ex) { ModConsole.LogError($"[USS] Failed to save shop: {ex.Message}\n{ex.StackTrace}"); }

        // BOUGHT ITEMS SAVING
        try
        {
            int itemCount = BoughtItems.Count;

            List<Vector3> pos = new(itemCount);
            List<Vector3> rot = new(itemCount);
            List<bool> inBag = new(itemCount);
            List<string> bagID = new(itemCount);
            List<float> condition = new(itemCount);

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

            ItemSave saveData = new(pos, rot, inBag, bagID, condition);
            SaveLoad.SerializeClass(mod, saveData, String.Format(ItemSaveKey, ShopID));
        }
        catch (Exception ex) { ModConsole.LogError($"[USS] Failed to save {ShopID} items: {ex.Message}\n{ex.StackTrace}"); }
    }


    /// <summary>
    /// Load shop and all bought items
    /// </summary>
    /// <param name="mod">Mod class the method is called from</param>
    public void LoadShop(Mod mod)
    {
        if (SaveLoad.ValueExists(mod, String.Format(ShopSaveKey, ShopID)))
        {
            // SHOP LOADING
            ShopSave shopData = SaveLoad.DeserializeClass<ShopSave>(mod, String.Format(ShopSaveKey, ShopID));
            if (shopData != null)
            {
                Stock = shopData.Stock;
                for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(i >= (transform.childCount - Stock));
                itemsBought = shopData.ItemsBought;
            }
            else ModConsole.LogError($"[USS] Failed to load shop data for {ShopID}: Save data is null or corrupted.");

            // ITEMS LOADING
            if (SaveLoad.ValueExists(mod, String.Format(ItemSaveKey, ShopID)))
            {
                ItemSave itemData = SaveLoad.DeserializeClass<ItemSave>(mod, String.Format(ItemSaveKey, ShopID));
                List<GameObject> shoppingBags = [.. (from gameObject in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>()
                                                 where gameObject.name.Contains("shopping bag") && gameObject.GetComponent<PlayMakerFSM>() != null
                                                 select gameObject)];

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
        // BACKWARDS COMPATIBILITY FOR PRE-1.0.6
        else if (SaveLoad.ValueExists(mod, $"USS_{ShopID}_stock")) LegacyLoad(mod);
    }

    [Obsolete]
    private void LegacyLoad(Mod mod) {      
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

        List<GameObject> shoppingBags = [.. (from gameObject in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>()
                                         where gameObject.name.Contains("shopping bag") && gameObject.GetComponent<PlayMakerFSM>() != null
                                         select gameObject)];

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
            if (Shop.Cart > 0 && Check.Value == 0) Fsm.Event("FINISHED");
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
        if (ShopID == "Unique ID for Save/Load management") ModConsole.Error($"[USS]: ShopID of {ItemName} is still default!");

        if (!ShopLookup.ContainsKey(GetComponent<Collider>())) ShopLookup[GetComponent<Collider>()] = this;

        GameObject player = GameObject.Find("PLAYER");
        ItemShopRaycast itemShopRaycast = player.GetComponent<ItemShopRaycast>() ?? player.AddComponent<ItemShopRaycast>();
        itemShopRaycast.Shops.Add(this); // Register shop for commands, unique id check etc.

        GameObject store = GameObject.Find("STORE");

        register = store.transform.Find("StoreCashRegister/Register").GetComponent<PlayMakerFSM>();
        registerData = GameObject.Find("StoreCashRegister").transform.GetChild(2).GetPlayMaker("Data");
        registerData.InitializeFSM();
        vanillaShopInventory = store.transform.Find("Inventory").gameObject;

        register.FsmInject("Purchase", Pay);
        vanillaShopInventory.GetPlayMaker("Logic").GetState("Items").InsertAction(0, new RestockAction { shop = this }); // Inject paying and restock mechanics

        transform.SetParent(store.transform.Find("LOD").transform.Find("GFX_Store").transform.Find("store_inside"), false);
        transform.localEulerAngles = TriggerRotation;
        transform.localPosition = TriggerPosition;

        Stock = transform.childCount;
        Cart = itemsBought = 0;
        Restock(); // Fully restock, save data overrides the values

        if (SpawnInBag) SetupBagSpawning(store); // Only setup the whole stuff when items should spawn in bags     
    }

    private void OnDestroy() => ShopLookup.Remove(GetComponent<Collider>());

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
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(true); itemsBought = 0;
        Stock = transform.childCount;
        OnRestock?.Invoke(); // Run user-provided actions
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