using UnityEngine;
using System.Collections;
#if !MINI
using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using MSCLoader;
#endif

namespace UniversalShoppingSystem;

public enum ItemSizes
{
    Small,
    Medium,
    Large,
    FullShelf
}

public class FleamarketShop : ShopBase
{
    public ItemSizes ItemSize;

    [Range(0f, 1f)]
    public float AvailabilityChance = .2f;
    [Header("If true, chance for subsequent items to spawn exponentially decreases.")]
    public bool ChainedChance = true;

    [Header("Price Settings, see documentation for price calculation info")]
    [UnityEngine.Tooltip("Base price, scarcity will add on this price and random fluctuation might subtract")]
    public float BasePrice;
    [UnityEngine.Tooltip("Aggressiveness of price change based on availability. Set to 1 to disable.")]
    public float ScarcityFactor = 1.5f;
    [UnityEngine.Tooltip("Random price variation by ±x, set to 0 to disable randomness.")]
    public float PriceRandomnessRange = 0;
    [Header("Maximum price the item will resell for")]
    public float MaxResalePrice;

#if !MINI
    // FOLLOWING NOT NEEDED FOR UNITY SETUP; THEREFORE NOT INCLUDED IN MINI DLL
    private float ItemPrice;
    public override float GetItemPrice() => ItemPrice;

    private PlayMakerFSM register;
    private FsmFloat registerTotal;

    private Transform ItemSpawnPosition;

    private const string ShopSaveKey = "USS_fleamarket{0}";
    private const string ItemSaveKey = "USS_fleamarket{0}_items";

    /// <summary>
    /// Save the shop and all bought items
    /// </summary>
    /// <param name="mod">Mod Class the method is called from</param>
    public void SaveShop(Mod mod)
    {
        // SHOP SAVING
        try
        {
            FleamarketShopSave saveData = new(Stock + Cart, itemsBought, transform.parent.name, transform.parent.parent.name, transform.parent.parent.parent.name);
            SaveLoad.SerializeClass(mod, saveData, String.Format(ShopSaveKey, ShopID));
        }
        catch (Exception ex) { ModConsole.LogError($"[USS] Failed to save shop: {ex.Message}\n{ex.StackTrace}"); }

        // BOUGHT ITEMS SAVING
        try
        {
            int itemCount = BoughtItems.Count;

            List<Vector3> pos = new(itemCount);
            List<Vector3> rot = new(itemCount);

            foreach (GameObject obj in BoughtItems)
            {
                pos.Add(obj.transform.position);
                rot.Add(obj.transform.eulerAngles);
            }

            ItemSave saveData = new(pos, rot);
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
            FleamarketShopSave shopData = SaveLoad.DeserializeClass<FleamarketShopSave>(mod, String.Format(ShopSaveKey, ShopID));
            if (shopData != null)
            {
                FleamarketRestock.RandomizeShelf(this, shopData.Shelf, shopData.ShelfParent, shopData.ShelfGrandparent);
                Restock(shopData.Stock);
                itemsBought = shopData.ItemsBought;
            }
            else ModConsole.LogError($"[USS] Failed to load shop data for {ShopID}: Save data is null or corrupted.");

            // ITEMS LOADING
            if (SaveLoad.ValueExists(mod, String.Format(ItemSaveKey, ShopID)))
            {
                ItemSave itemData = SaveLoad.DeserializeClass<ItemSave>(mod, String.Format(ItemSaveKey, ShopID));

                int itemCount = itemData.Position.Count;
                if (itemData.Rotation.Count != itemCount)
                {
                    ModConsole.LogError($"[USS] Item save data mismatch for {ShopID}. Skipping item loading.");
                    return;
                }

                for (int i = 0; i < itemCount; i++)
                {
                    GameObject obj = GameObject.Instantiate(ItemPrefab);
                    obj.transform.position = itemData.Position[i];
                    obj.transform.eulerAngles = itemData.Rotation[i];

                    BoughtItems.Add(obj); // Keep the item tracked for next save
                }
            }
        }
        else FleamarketRestock.RandomizeShelf(this);
    }

    protected override void Start()
    {
        base.Start();

        if (ModLoader.CurrentGame == Game.MySummerCar)
        {
            ModConsole.Error($"[USS]: Flea market is not available in MSC! Please remove Shop {ShopID}. Destroying Shop now to prevent fatal errors.");
            Destroy(this);
        }

        PriceRandomnessRange = Mathf.Abs(PriceRandomnessRange);
        ScarcityFactor = Mathf.Max(1, ScarcityFactor);

        Transform fleamarket = GameObject.Find("FleaMarket").transform;

        register = fleamarket.transform.Find("LOD/FleaCashRegister/CashRegisterLogic").GetPlayMaker("Data");
        register.InitializeFSM();
        registerTotal = register.FsmVariables.GetFsmFloat("Total");
        register.FsmInject("Purchase", Pay);

        if (!ItemPrefab.GetComponent<FleamarketSellable>())
        {
            FleamarketSellable sellable = ItemPrefab.AddComponent<FleamarketSellable>();
            sellable.MaxSalePrice = MaxResalePrice;
        } 

        Stock = Cart = itemsBought = 0;
        Restock(); // restock, save data overrides the values

        ItemSpawnPosition = fleamarket.Find("fleamarket_table/SpawnItemFleamarket");

        if (ItemSpawnPosition == null)
        {
            ItemSpawnPosition = new GameObject().transform;
            ItemSpawnPosition.name = "SpawnItemFleamarket";
            ItemSpawnPosition.SetParent(fleamarket.Find("store_table"));
            ItemSpawnPosition.transform.localPosition = new(0.1f, -0.15f, 0.8f);
            ItemSpawnPosition.transform.localEulerAngles = new(0, 270, 270);
        }

        GameObject.Find("MAP").transform.Find("PivotSun/Pivot/SUN").GetPlayMaker("Color").FsmInject("Next day", delegate
        {
            if (PlayMakerGlobals.Instance.Variables.GetFsmInt("GlobalDay").Value == 5) Restock();
        }, index: 0);
    }

    private void Restock(int count = -1)
    {
        Stock = 0;
        List<Transform> children = [.. transform.Cast<Transform>()];

        // shuffle
        for (int i = children.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Transform t = children[i];
            children[i] = children[j];
            children[j] = t;
        }

        List<Transform> activeItems = new List<Transform>();
        List<Transform> inactiveItems = new List<Transform>();

        foreach (Transform child in children)
        {
            if (count != -1)
            {
                if (activeItems.Count < count)
                {
                    child.gameObject.SetActive(true);
                    Stock++;
                    activeItems.Add(child);
                }
                else
                {
                    child.gameObject.SetActive(false);
                    inactiveItems.Add(child);
                }
                continue;
            }

            if (UnityEngine.Random.Range(0f, 1f) <= (ChainedChance ? Mathf.Pow(AvailabilityChance, Stock + 1) : AvailabilityChance)) // subsequent items get increasingly rare
            {
                child.gameObject.SetActive(true);
                Stock++;
                activeItems.Add(child);
            }
            else
            {
                child.gameObject.SetActive(false);
                inactiveItems.Add(child);
            }
        }

        for (int i = 0; i < activeItems.Count; i++) activeItems[i].SetSiblingIndex(i);
        for (int i = 0; i < inactiveItems.Count; i++) inactiveItems[i].SetSiblingIndex(activeItems.Count + i);

        itemsBought = 0;
        ItemPrice = BasePrice + Mathf.RoundToInt(ScarcityFactor * Mathf.Max(transform.childCount - Stock - 1, 0)) + Mathf.Round(UnityEngine.Random.Range(-PriceRandomnessRange, PriceRandomnessRange));
        if (Stock > 0) InvokeOnRestock(); // Run user-provided actions
    }

    /// <summary>
    /// Add one item to the cart
    /// </summary>
    internal override void Buy() // Put item in Cart
    {
        Stock--;
        Cart++;

        this.transform.GetChild(Cart - 1 + itemsBought).gameObject.SetActive(false);

        registerTotal.Value += ItemPrice;
        register.SendEvent("PURCHASE");
    }

    /// <summary>
    /// Remove one item from the cart
    /// </summary>
    internal override void Unbuy() // Take item back out of Cart
    {
        this.transform.GetChild(Cart - 1 + itemsBought).gameObject.SetActive(true);
        Stock++;
        Cart--;

        registerTotal.Value -= ItemPrice;
        register.SendEvent("PURCHASE");
    }

    private void Pay() // Called on checkout at register
    {
        if (this.Cart > 0) InvokeOnBuy();
        StartCoroutine(SpawnPurchase(Cart));
        itemsBought += Cart;
        Cart = 0;
    }

    private IEnumerator SpawnPurchase(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject item = GameObject.Instantiate(ItemPrefab);
            BoughtItems.Add(item);
            item.transform.position = ItemSpawnPosition.position;
            item.SetActive(true);
            yield return new WaitForSeconds(0.1f);
        }
        yield return null;
    }

    public override void PrintDebugInfo()
    {
        base.PrintDebugInfo();
        ModConsole.Log($"Price info:\nbase: {BasePrice}\nscarcity factor: {ScarcityFactor:F2}\nrandomness variation: ±{PriceRandomnessRange}Mk\ncurrent price: {ItemPrice:F2}Mk\n");
        ModConsole.Log($"item availability chance: {AvailabilityChance}");
    }
#endif
}