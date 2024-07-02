using UnityEngine;
using MSCLoader;
using System.Collections;

using FridgeAPI;

namespace UniversalShoppingSystem
{
    public class USSItem : MonoBehaviour
    {
        public bool CanSpoil = false;
        public float SpoilingMultiplicator = 1f;
        
        // None of those should be set up in unity editor, they're set on runtime. Therefore not included in mini dll!
        public bool InBag;
        public string BagID;
        public ItemShop OriginShop;
        public float Condition = 100f;
        public bool Spoiled { get; private set; }

        public bool Cooled { get; private set; } // Used to communicate to the outside whether the item is cooled or not.
        private bool inFAPIFridge = false;
        private bool inVanillaFridge = false;

        private readonly Transform fridge = GameObject.Find("YARD").transform.Find("Building/KITCHEN/Fridge/FridgePoint/ChillArea");    

        private readonly float globalSpoilingRate = 0.06f;
        private readonly float spoilingRateFridge = 0.001f;
        private float FAPISpoilingRate;
        
        private Coroutine spoiling;
        
        public void StartSpoiling() 
        { 
            if (spoiling == null && CanSpoil) spoiling = StartCoroutine(Spoil());
        }

        private void Update()
        {
            inVanillaFridge = (transform.position - fridge.position).sqrMagnitude < 0.20249999f;
            Cooled = inVanillaFridge || inFAPIFridge; 
        }

        public IEnumerator Spoil()
        {
            while (true)
            {
                if ((transform.position - fridge.position).sqrMagnitude < 0.20249999f) Condition -= spoilingRateFridge * SpoilingMultiplicator; // When in vanilla fridge
                else if (inFAPIFridge) Condition -= FAPISpoilingRate * SpoilingMultiplicator; // When in FridgeAPI fridge
                else Condition -= globalSpoilingRate * SpoilingMultiplicator; // Else must be uncooled
                
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