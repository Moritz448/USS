/*using UnityEngine;
using MSCLoader;
using System.Collections;
using System.Collections.Generic;

using ExpandedShop;

namespace UniversalShoppingSystem
{
    public class USSBagInventory : MonoBehaviour
    {
        public List<GameObject> BagContent = new List<GameObject>();

        private void Start() => StartCoroutine(InitiateBag());

        private void TakeESOver() // Copys ExpandedShop items over to USS bag inventory
        {
            ModShopBagInv es = this.gameObject.GetComponent<ModShopBagInv>();

            if (es !=null && es.shoplist.Count > 0)
            {
                this.BagContent.AddRange(es.shoplist);

                for (int i = 0; i < this.BagContent.Count; i++)
                {
                    ModItem moditm = BagContent[i].GetComponent<ModItem>();
                    if (moditm != null)
                    {
                        moditm.BagID = gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                        moditm.BagCountInt = BagContent.IndexOf(moditm.gameObject);
                    }

                    else if (!BagContent[i].GetComponent<USSItem>()) ModConsole.LogError("UniversalShoppingSystem: Found no shop system item behaviour on item " + i + "!");
                    es.shoplist.Clear();
                }
            }
        }

        private IEnumerator InitiateBag()
        {
            yield return new WaitForSeconds(0.5f);

            // Set BagID for ever USS item in the bag
            for (int i = 0; i < this.BagContent.Count; i++) if (BagContent[i].GetComponent<USSItem>()) BagContent[i].GetComponent<USSItem>().BagID = this.gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;

            // Take over all ExpandedShop items if ES is loaded
            if (ModLoader.IsModPresent("ExpandedShop")) TakeESOver(); 
            
            if (BagContent.Count == 0) Destroy(this); 

            yield break;
        }
    }
}*/