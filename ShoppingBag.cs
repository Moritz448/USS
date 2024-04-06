using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;
using MSCLoader;
using ExpandedShop;

namespace UniversalShoppingSystem
{
    public class USSBagInventory : MonoBehaviour
    {
        public List<GameObject> BagContent = new List<GameObject>();

        void Start()
        {
            StartCoroutine(InitiateBag());

        }

        private void TakeESOver()
        {
            if (this.gameObject.GetComponent<ModShopBagInv>() && this.gameObject.GetComponent<ModShopBagInv>().shoplist.Count > 0)
            {
                ModShopBagInv es = gameObject.GetComponent<ModShopBagInv>();
                ModItem moditm;

                this.BagContent.AddRange(es.shoplist);

                for (int i = 0; i < this.BagContent.Count; i++)
                {

                    if (BagContent[i].GetComponent<ModItem>())
                    {
                        moditm = BagContent[i].GetComponent<ModItem>();
                        moditm.BagID = gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                        moditm.BagCountInt = BagContent.IndexOf(moditm.gameObject);
                    }

                    else if (!BagContent[i].GetComponent<USSItem>()) ModConsole.LogError("UniversalShoppingSystem: Found no shop system item behaviour on item " + i + "!");

                    es.shoplist.Clear();
                }
            }
        }
        IEnumerator InitiateBag()
        {
            yield return new WaitForSeconds(0.5f);

            

            // Set BagID for ever USS item in the bag
            for (int i = 0; i < this.BagContent.Count; i++) if (BagContent[i].GetComponent<USSItem>()) BagContent[i].GetComponent<USSItem>().BagID = this.gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                
            if (ModLoader.IsModPresent("ExpandedShop")) TakeESOver(); // Take over all ExpandedShop items if ES is loaded
            

            if (BagContent.Count == 0) Destroy(this); // If there are no items in here, just destroy the behaviour.

            yield break;
        }
    }
    public class USSBagSetupAction : FsmStateAction
    {
        public FsmGameObject bag;
        public ItemShop shop;
        public override void OnEnter()
        {
            USSBagInventory inv = bag.Value.AddComponent<USSBagInventory>();
            if (shop.SpawnInBag) shop.SpawnBag(inv);
            USSBagSetupOpenAction act = bag.Value.AddComponent<USSBagSetupOpenAction>();
            act.bag = bag.Value;
            act.inv = inv;
            Finish();
        }
    }
    public class USSBagOpenAction : FsmStateAction
    {
        public USSBagInventory inv;
        public bool OpenAll = false;
        public PlayMakerArrayListProxy[] Arrays;

        private bool CheckForModItem(Transform item)
        {
            return item.GetComponent<ModItem>();
        }

        private void TakeModItemOut(Transform item)
        {
            ModItem moditm = item.GetComponent<ModItem>();
            moditm.InBag = false;
        }
        public override void OnEnter()
        {
            if (!OpenAll)
            {
                if (inv.BagContent.Count > 0)
                {
                    Transform itm = inv.BagContent[0].transform;
                    itm.position = new Vector3(Fsm.GameObject.transform.position.x, Fsm.GameObject.transform.position.y + 0.1f, Fsm.GameObject.transform.position.z);
                    itm.eulerAngles = Vector3.zero;
                    itm.gameObject.SetActive(true);

                    if (itm.GetComponent<USSItem>()) // If its an USS item...
                    {
                        USSItem ussitm = itm.GetComponent<USSItem>();
                        ussitm.InBag = false;
                        ussitm.OriginShop.BoughtItems.Add(itm.gameObject);
                        ussitm.Condition = base.Fsm.Variables.FindFsmFloat("Condition").Value;
                    }
                    else if (ModLoader.IsModPresent("ExpandedShop") && CheckForModItem(itm)) TakeModItemOut(itm); // else it has to be an expanded shop item.

                    inv.BagContent.Remove(itm.gameObject);

                    if (CheckVanillaEmpty()) MasterAudio.PlaySound3DAndForget("HouseFoley", inv.transform, false, 1f, 1f, 0f, "plasticbag_open2");
                    FsmVariables.GlobalVariables.FindFsmString("GUIinteraction").Value = "";
                    if (inv.BagContent.Count == 0 && CheckVanillaEmpty()) Fsm.Event("GARBAGE");
                    if (inv.BagContent.Count == 0) Object.Destroy(inv);
                    Fsm.Event("FINISHED");

                }

            }
            else
            {
                for (int i = 0; i < inv.BagContent.Count; i++)
                {
                    inv.BagContent[i].transform.position = inv.gameObject.transform.position;
                    inv.BagContent[i].transform.eulerAngles = Vector3.zero;
                    inv.BagContent[i].SetActive(true);
                    if (inv.BagContent[i].GetComponent<USSItem>()) // If its an USS item...
                    {
                        USSItem ussitm = inv.BagContent[i].GetComponent<USSItem>();
                        ussitm.InBag = false;
                        ussitm.OriginShop.BoughtItems.Add(inv.BagContent[i].gameObject);
                    }
                    else if (ModLoader.IsModPresent("ExpandedShop")) TakeModItemOut(inv.BagContent[i].transform);// else it has to be an expanded shop item.
                }

                inv.BagContent = new List<GameObject>();
                FsmVariables.GlobalVariables.FindFsmString("GUIinteraction").Value = "";
                if (CheckVanillaEmpty())
                {
                    Fsm.Event("GARBAGE");
                    MasterAudio.PlaySound3DAndForget("HouseFoley", inv.transform, false, 1f, 1f, 0f, "plasticbag_open1");
                }
            }
            Finish();
        }
        private bool CheckVanillaEmpty()
        {
            int num = 0;
            PlayMakerArrayListProxy[] array = Arrays;
            for (int i = 0; i < array.Length; i++)
            {
                foreach (int array2 in array[i].arrayList)
                {
                    if (array2 > num)
                    {
                        num = array2;
                    }
                }
            }
            return num == 0;
        }
    }
    public class USSBagSetupOpenAction : MonoBehaviour
    {
        PlayMakerFSM use;
        public USSBagInventory inv;
        public GameObject bag;
        void Start()
        {
            StartCoroutine(Setup());
        }
        IEnumerator Setup()
        {
            yield return new WaitForSeconds(0.4f);
            use = bag.GetComponent<PlayMakerFSM>();
            use.GetState("Spawn one").InsertAction(0, new USSBagOpenAction
            {
                Arrays = bag.GetComponents<PlayMakerArrayListProxy>(),
                OpenAll = false,
                inv = inv
            });
            use.GetState("Spawn all").InsertAction(0, new USSBagOpenAction
            {
                Arrays = bag.GetComponents<PlayMakerArrayListProxy>(),
                OpenAll = true,
                inv = inv
            });
            Object.Destroy(this);
            yield break;
        }
    }
}