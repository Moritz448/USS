using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalShoppingSystem;

[System.Serializable]
public class ShopSave
{
    public int Stock;
    public int ItemsBought;

    public ShopSave() { }

    public ShopSave(int stock, int itemsBought)
    {
        Stock = stock;
        ItemsBought = itemsBought;
    }
}

[System.Serializable]
public class FleamarketShopSave : ShopSave
{
    public string Shelf;
    public string ShelfParent;
    public string ShelfGrandparent;
    public FleamarketShopSave() { }

    public FleamarketShopSave(int stock, int itemsBought, string shelf, string shelfParent, string shelfGrandparent) : base(stock, itemsBought)
    {
        Shelf = shelf;
        ShelfParent = shelfParent;
        ShelfGrandparent = shelfGrandparent;
    }
}

[System.Serializable]
public class ItemSave
{
    public List<Vector3> Position;
    public List<Vector3> Rotation;

    public ItemSave()
    {
        Position = [];
        Rotation = [];
    }

    public ItemSave(List<Vector3> position, List<Vector3> rotation)
    {
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Rotation = rotation ?? throw new ArgumentNullException(nameof(rotation));
    }
}


[System.Serializable]
public class USSItemSave : ItemSave
{
    public List<bool> InBag;
    public List<string> BagID;
    public List<float> Condition;

    public USSItemSave() : base()
    {
        InBag = [];
        BagID = [];
        Condition = [];
    }

    public USSItemSave(List<Vector3> position, List<Vector3> rotation, List<bool> inBag, List<string> bagID, List<float> condition) : base(position, rotation)
    {
        InBag = inBag ?? throw new ArgumentNullException(nameof(inBag));
        BagID = bagID ?? throw new ArgumentNullException(nameof(bagID));
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }
}

