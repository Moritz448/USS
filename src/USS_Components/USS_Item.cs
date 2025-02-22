﻿using UnityEngine;

#if !MINI
using MSCLoader;
using System.Collections;

using FridgeAPI;
#endif

namespace UniversalShoppingSystem;

public class USSItem : MonoBehaviour
{
    public bool CanSpoil = false;
    public float SpoilingMultiplicator = 1f;

#if !MINI
    // None of those should be set up in unity editor, they're set on runtime. Therefore not included in mini dll!
    public bool InBag;
    public string BagID;
    public ItemShop OriginShop;
    public float Condition = 100f;
    public bool Spoiled { get; private set; }
    public bool Cooled { get; private set; } // Used to communicate to the outside whether the item is cooled or not.

    private bool inFAPIFridge, inVanillaFridge = false;

    private static readonly Transform fridge;

    private static readonly bool FAPIpresent = ModLoader.IsModPresent("FridgeAPI");

    private readonly float globalSpoilingRate = 0.06f;
    private readonly float spoilingRateFridge = 0.001f;
    private float FAPISpoilingRate;
    
    private Coroutine spoiling;
    
    static USSItem()
    {  
        fridge = GameObject.Find("YARD")?.transform.Find("Building/KITCHEN/Fridge/FridgePoint/ChillArea");
    }

    public void StartSpoiling() 
    { 
        if (spoiling == null && CanSpoil) spoiling = StartCoroutine(Spoil());
    }

    private void Update()
    {
        inVanillaFridge = (transform.position - fridge.position).sqrMagnitude < 0.2025f;
        Cooled = inVanillaFridge || inFAPIFridge; 
    }

    public IEnumerator Spoil()
    {
        while (Condition > 1f)
        {
            Condition -= (inVanillaFridge ? spoilingRateFridge : inFAPIFridge ? FAPISpoilingRate : globalSpoilingRate) * SpoilingMultiplicator;
            yield return new WaitForSeconds(1f);
        }

        Condition = 0f;

        if (!gameObject.name.ToLower().Contains("spoiled")) gameObject.name = "Spoiled " + gameObject.name;

        Spoiled = true;
    }
    
    private void OnTriggerEnter(Collider coll)
    {
        if (!FAPIpresent) return;
        Fridge fridge = coll.GetComponent<Fridge>();
        if (fridge != null)
        {
            inFAPIFridge = true;
            FAPISpoilingRate = fridge.FridgeSpoilingRate;
        }
    }

    private void OnTriggerExit(Collider coll)
    {
        if (!FAPIpresent) return;
        if (coll.GetComponent<Fridge>()) inFAPIFridge = false;
    }
#endif

}