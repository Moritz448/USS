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

public class ItemSave
{
    public List<Vector3> Position;
    public List<Vector3> Rotation;
    public List<bool> InBag;
    public List<string> BagID;
    public List<float> Condition;

    public ItemSave()
    {
        Position = new List<Vector3>();
        Rotation = new List<Vector3>();
        InBag = new List<bool>();
        BagID = new List<string>();
        Condition = new List<float>();
    }

    public ItemSave(List<Vector3> position, List<Vector3> rotation, List<bool> inBag, List<string> bagID, List<float> condition)
    {
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Rotation = rotation ?? throw new ArgumentNullException(nameof(rotation));
        InBag = inBag ?? throw new ArgumentNullException(nameof(inBag));
        BagID = bagID ?? throw new ArgumentNullException(nameof(bagID));
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }
}

