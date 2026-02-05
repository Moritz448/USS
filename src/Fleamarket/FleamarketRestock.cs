#if !MINI
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using MSCLoader;
using UnityEngine;

namespace UniversalShoppingSystem;

internal static class FleamarketRestock
{
    private static Dictionary<ItemSizes, List<Transform>> shelveSpots = new();
    private static HashSet<Transform> occupiedShelves = new();
    private static HashSet<FleamarketShop> placedShops = new();

    public static void Reset()
    {
        shelveSpots = new();
        occupiedShelves = new();
        placedShops = new();
    }

    private static void GetShelves()
    {
        List<Transform> shelves = [];

        foreach(Transform t in GameObject.Find("FleaMarket").transform.Find("LOD/SHELFS")) if (int.TryParse(t.name, out _)) shelves.Add(t);

        if (shelveSpots.Count > 0) return;

        foreach (Transform shelf in shelves)
        {
            foreach (Transform section in shelf)
            {
                if (!section.name.Contains("Section")) continue;

                foreach (Transform spot in section)
                {
                    ItemSizes? size = spot.name switch
                    {
                        "clothes" => ItemSizes.FullShelf,
                        "Stuff1" or "Stuff2" => ItemSizes.Medium,
                        "Stuff3" or "Stuff4" => ItemSizes.Small,
                        "Berrybox1" => ItemSizes.Large,
                        _ => null
                    };

                    if (size.HasValue)
                    {
                        if (!shelveSpots.ContainsKey(size.Value)) shelveSpots[size.Value] = new List<Transform>();

                        shelveSpots[size.Value].Add(spot);
                    }
                }
            }
        }
    }

    public static void RandomizeShelf(FleamarketShop shop, string shelfName = "", string shelfParent = "", string shelfGrandparent = "")
    {
        if (shelveSpots.Count == 0) GetShelves();

        if (!placedShops.Contains(shop))
        {
            Transform shelf;

            if (!(shelfName == "") && !(shelfParent == "") && !(shelfGrandparent == "")) 
            {
                shelf = shelveSpots
                .SelectMany(kvp => kvp.Value)
                .FirstOrDefault(t =>
                    (t.name == shelfName) &&
                    (t.parent.name == shelfParent) &&
                    (t.parent.parent.name == shelfGrandparent)
                );
            }

            else shelf = PickRandomSpot(shop.ItemSize);

            if (shelf == null)
            {
                GameObject.Destroy(shop.gameObject);
                return;
            }

            placedShops.Add(shop);
            shop.transform.SetParent(shelf);
            shop.transform.localPosition = shop.ItemSize switch
            {
                ItemSizes.FullShelf => new(0, 0, -0.692f),
                ItemSizes.Large => new(0, 0, 0.073f),
                _ => new(0, 0, 0.123f)
            };
            shop.transform.localEulerAngles = new(270, 180, 0);
            shop.itemShopRaycast.StartCoroutine(ItemShopRaycast.DeleteVanillaStuff(shop));
        }
    }

    private static Transform PickRandomSpot(ItemSizes size)
    {
        if (!shelveSpots.ContainsKey(size) || shelveSpots[size].Count == 0) return null; // no free spots

        List<Transform> freeSpots;

        if (size == ItemSizes.Small)
        {
            freeSpots = shelveSpots[ItemSizes.Small].Where(s => !occupiedShelves.Contains(s)).ToList();
            freeSpots.AddRange(shelveSpots[ItemSizes.Medium].Where(s => !occupiedShelves.Contains(s)).ToList());
        }
        else freeSpots = shelveSpots[size].Where(s => !occupiedShelves.Contains(s)).ToList();

        if (freeSpots.Count == 0) return null; // all spots occupied

        Transform spot = freeSpots[UnityEngine.Random.Range(0, freeSpots.Count)];
        occupiedShelves.Add(spot);

        if (size == ItemSizes.Large)
        {
            spot.GetComponent<MeshRenderer>().enabled = false;
            spot.localEulerAngles = Vector3.zero;
        }
        else if (size == ItemSizes.FullShelf) GameObject.Destroy(spot.parent.Find("Potatobox1").gameObject);

        return spot;
    }
}
#endif