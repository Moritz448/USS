using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalShoppingSystem;

[System.Serializable]
public class ShopSave
{
    public List<bool> ActiveItems { get; private set; }
    public int Stock { get; private set; }
    public int ItemsBought { get; private set; }

    public ShopSave()
    {
        ActiveItems = new List<bool>();
    }

    public ShopSave(List<bool> activeItems, int stock, int itemsBought)
    {
        ActiveItems = activeItems ?? throw new ArgumentNullException(nameof(activeItems));
        Stock = stock;
        ItemsBought = itemsBought;
    }
}

public class ItemSave
{
    public List<Vector3> Position { get; private set; }
    public List<Vector3> Rotation { get; private set; }
    public List<bool> InBag { get; private set; }
    public List<string> BagID { get; private set; }
    public List<float> Condition { get; private set; }

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

