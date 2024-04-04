using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;
using MSCLoader;
using ExpandedShop;
using System.Linq;

namespace UniversalShoppingSystem
{
    public class USSBagInventory : MonoBehaviour
    {
        public List<GameObject> BagContent = new List<GameObject>();

        void Start()
        {
            //StartCoroutine(CheckItems());
            //StartCoroutine(HomuraCheck());

            StartCoroutine(InitiateBag());
        }

        IEnumerator InitiateBag()
        {
            yield return new WaitForSeconds(0.5f);
            if (this.gameObject.GetComponent<ModShopBagInv>())
            {
                ModConsole.Log("This bag already has an expanded shop behaviour on it; mergin lists");
                ModShopBagInv es = gameObject.GetComponent<ModShopBagInv>();
                USSItem ussitm;
                ModItem moditm;
                this.BagContent.AddRange(es.shoplist);
                for (int i = 0; i < this.BagContent.Count; i++)
                {
                    if (BagContent[i].GetComponent<USSItem>())
                    {
                        ussitm = BagContent[i].GetComponent<USSItem>();
                        ussitm.BagID = this.gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                    }
                    else if (BagContent[i].GetComponent<ModItem>()) // Expanded shop 
                    {
                        moditm = BagContent[i].GetComponent<ModItem>();
                        moditm.BagID = gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                        moditm.BagCountInt = BagContent.IndexOf(moditm.gameObject);
                    }
                    else ModConsole.LogError("Found no item behaviour");

                    es.shoplist.Clear();
                }

                ModConsole.Log("Moved items over");
                if (BagContent.Count == 0) Destroy(this); // If there are no items in here, just destroy the behaviour.
            }

            else
            {
                foreach (GameObject obj in BagContent)
                {
                    obj.GetComponent<USSItem>().BagID = gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                }
            }
            yield break;
        }
        IEnumerator CheckItems()
        {
            yield return new WaitForSeconds(0.5f);
            // Destroy bag when its empty
            if (BagContent.Count == 0) Destroy(this);
            else
            {
                foreach (GameObject obj in BagContent)
                {
                    obj.GetComponent<USSItem>().BagID = gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                }
            }
            yield break;
        }

        IEnumerator HomuraCheck()
        {   
            if (this.gameObject.GetComponent<ModShopBagInv>())
            {
                ModConsole.LogWarning("This bag already has an expanded shop behaviour on it; mergin lists");
                ModShopBagInv es = gameObject.GetComponent<ModShopBagInv>();
                ModItem itm;
                for (int i = 0; i < es.shoplist.Count; i++)
                {
                    this.BagContent.Add(es.shoplist[i]);
                    itm = es.shoplist[i].GetComponent<ModItem>();
                    itm.BagID = this.gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                    itm.BagCountInt = this.BagContent.IndexOf(itm.gameObject);
                    es.shoplist.Remove(es.shoplist[i]);
                }

                Destroy(es);
                ModConsole.LogWarning("Deleted expanded shop behaviour from bag and moved items over");
                if (BagContent.Count == 0) Destroy(this); // If there are no items in here, just destroy the behaviour.
            }
            yield return null;
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
                    }
                    else // else it has to be an expanded shop item.
                    {
                        ModItem esitm = itm.GetComponent<ModItem>();
                        esitm.InBag = false;
                    }
                    inv.BagContent.Remove(itm.gameObject);
                    if (CheckEmpty()) MasterAudio.PlaySound3DAndForget("HouseFoley", inv.transform, false, 1f, 1f, 0f, "plasticbag_open2");
                    FsmVariables.GlobalVariables.FindFsmString("GUIinteraction").Value = "";
                    if (inv.BagContent.Count == 0 && CheckEmpty()) Fsm.Event("GARBAGE");
                    if (inv.BagContent.Count == 0) Object.Destroy(inv);
                    ModConsole.LogWarning("Sending Finished.");
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
                    USSItem ussitm = inv.BagContent[i].GetComponent<USSItem>();
                    ussitm.InBag = false;
                    ussitm.OriginShop.BoughtItems.Add(inv.BagContent[i].gameObject);
                }
                inv.BagContent = new List<GameObject>();
                FsmVariables.GlobalVariables.FindFsmString("GUIinteraction").Value = "";
                if (CheckEmpty())
                {
                    Fsm.Event("GARBAGE");
                    MasterAudio.PlaySound3DAndForget("HouseFoley", inv.transform, false, 1f, 1f, 0f, "plasticbag_open1");
                }
            }
            Finish();
        }
        private bool CheckEmpty()
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
            yield return new WaitForSeconds(0.5f);
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