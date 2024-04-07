using UnityEngine;
using MSCLoader;
using System.Collections;

using FridgeAPI;

namespace UniversalShoppingSystem
{
    public class USSItem : MonoBehaviour
    {
        public bool CanSpoil = false;
        
        // None of those should be set up in unity editor, they're set on runtime. Therefore not included in mini dll!
        public bool InBag;
        public string BagID;
        public ItemShop OriginShop;
        public float Condition = 100f;
        public bool Spoiled;


        private Transform fridge = GameObject.Find("YARD").transform.Find("Building/KITCHEN/Fridge/FridgePoint/ChillArea");
        private bool inFAPIFridge = false;

        private float globalSpoilingRate = 0.06f;
        private float spoilingRateFridge = 0.001f;
        private float FAPISpoilingRate;
        
        private Coroutine spoiling;
        
        public void StartSpoiling() { if (spoiling == null && CanSpoil) spoiling = StartCoroutine(Spoil()); }

        public IEnumerator Spoil()
        {
            while (true)
            {
                if ((transform.position - fridge.position).sqrMagnitude < 0.20249999f) Condition -= spoilingRateFridge; // When in vanilla fridge
                else if (inFAPIFridge) Condition -= FAPISpoilingRate; // When in FridgeAPI fridge
                else Condition -= globalSpoilingRate; // Else must be uncooled
                
                if (Condition < 1f) break; // When the item is rotten
                yield return new WaitForSeconds(1f);
            }

            Condition = 0f;

            if (!gameObject.name.ToLower().Contains("spoiled"))
            {
                gameObject.name = "Spoiled " + gameObject.name;
            }

            Spoiled = true;
        }

        
        private void OnTriggerEnter(Collider coll)
        {
            if (ModLoader.IsModPresent("FridgeAPI"))
            {
                Fridge fridge = coll.GetComponent<Fridge>();
                if (fridge != null)
                {
                    inFAPIFridge = true;
                    FAPISpoilingRate = fridge.FridgeSpoilingRate;
                }
            }
        }

        private void OnTriggerExit(Collider coll)
        {
            if (ModLoader.IsModPresent("FridgeAPI")) if (coll.GetComponent<Fridge>()) inFAPIFridge = false;
        }

        private void OnTriggerStay(Collider coll)
        {
            if (ModLoader.IsModPresent("FridgeAPI"))
            {
                Fridge fridge = coll.GetComponent<Fridge>();
                if (fridge != null) FAPISpoilingRate = fridge.FridgeSpoilingRate;  
            }
        }
    }
}