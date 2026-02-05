using UnityEngine;
using System.Collections.Generic;
#if !Mini
using MSCLoader;
#endif

namespace UniversalShoppingSystem;

public abstract class ShopBase : MonoBehaviour
{
    public delegate void ShopEvent();

    // Settings to change in Unity
    [Header("Shop Settings")]
    public string ShopID = "Unique ID for Save/Load management";
    [Tooltip("Name to be displayed when looking at the item ingame.")]
    public string ItemName = "Shop Item Name";
    public bool SpawnInBag;
    [Space(10)]
    public GameObject ItemPrefab;
    

#if !Mini
    // FOLLOWING NOT NEEDED FOR UNITY SETUP; THEREFORE NOT INCLUDED IN MINI DLL
    public List<GameObject> BoughtItems = [];
    internal static Dictionary<Collider, ShopBase> ShopLookup = [];


    public event ShopEvent OnBuy, OnRestock;

    public int Stock, Cart;
    protected int itemsBought;

    protected Transform bigItemSpawnPosition;

    protected virtual void Start()
    {
        if (ShopID == "Unique ID for Save/Load management") ModConsole.Error($"[USS]: ShopID of {ItemName} is still default!");
        if (!ShopLookup.ContainsKey(GetComponent<Collider>())) ShopLookup[GetComponent<Collider>()] = this;

        GameObject player = GameObject.Find("PLAYER");
        ItemShopRaycast itemShopRaycast = player.GetComponent<ItemShopRaycast>() ?? player.AddComponent<ItemShopRaycast>();
        itemShopRaycast.Shops.Add(this); // Register shop for commands, unique id check etc.

    }

    protected virtual void OnDestroy() => ShopLookup.Remove(GetComponent<Collider>());

    protected void InvokeOnRestock() => OnRestock?.Invoke();
    protected void InvokeOnBuy() => OnBuy?.Invoke();

    internal abstract void Buy();
    internal abstract void Unbuy();

    public virtual void PrintDebugInfo()
    {
        ModConsole.Log($"[USS] Debug info for shop {this.ShopID}\n");
        ModConsole.Log($"Item name: {ItemName}\nItem prefab: {ItemPrefab}");
    }
#endif
}
