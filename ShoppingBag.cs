using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;
using MSCLoader;

namespace UniversalShoppingSystem
{
    public class ModShopBagInv : MonoBehaviour
    {
        public List<GameObject> shoplist = new List<GameObject>();

        void Start()
        {
            StartCoroutine(CheckItems());
        }
        IEnumerator CheckItems()
        {
            yield return new WaitForSeconds(0.5f);
            // Destroy bag when its empty
            if (shoplist.Count == 0) Destroy(this);
            else
            {
                foreach (GameObject obj in shoplist)
                {
                    obj.GetComponent<USSItem>().BagID = gameObject.GetPlayMaker("Use").FsmVariables.FindFsmString("ID").Value;
                }
            }
            yield break;
        }
    }
    public class BagSetupAction : FsmStateAction
    {
        public FsmGameObject bag;
        public ItemShop shop;
        public override void OnEnter()
        {
            ModShopBagInv inv = bag.Value.AddComponent<ModShopBagInv>();
            if (shop.SpawnInBag) shop.SpawnBag(inv);
            BagSetupOpenAction act = bag.Value.AddComponent<BagSetupOpenAction>();
            act.bag = bag.Value;
            act.inv = inv;
            Finish();
        }
    }
    public class BagOpenAction : FsmStateAction
    {
        public ModShopBagInv inv;
        public bool OpenAll = false;
        public PlayMakerArrayListProxy[] Arrays;
        public override void OnEnter()
        {
            if (!OpenAll)
            {
                if (inv.shoplist.Count > 0)
                {
                    Transform itm = inv.shoplist[0].transform;
                    itm.position = new Vector3(Fsm.GameObject.transform.position.x, Fsm.GameObject.transform.position.y + 0.1f, Fsm.GameObject.transform.position.z);
                    itm.eulerAngles = Vector3.zero;
                    itm.gameObject.SetActive(true);
                    USSItem ussitm = itm.GetComponent<USSItem>();
                    ussitm.InBag = false;
                    ussitm.OriginShop.BoughtItems.Add(itm.gameObject);
                    inv.shoplist.Remove(itm.gameObject);
                    if (CheckEmpty()) MasterAudio.PlaySound3DAndForget("HouseFoley", inv.transform, false, 1f, 1f, 0f, "plasticbag_open2");
                    FsmVariables.GlobalVariables.FindFsmString("GUIinteraction").Value = "";
                    if (inv.shoplist.Count == 0 && CheckEmpty()) Fsm.Event("GARBAGE");
                    Fsm.Event("FINISHED");
                }
            }
            else
            {
                for (int i = 0; i < inv.shoplist.Count; i++)
                {
                    inv.shoplist[i].transform.position = inv.gameObject.transform.position;
                    inv.shoplist[i].transform.eulerAngles = Vector3.zero;
                    inv.shoplist[i].SetActive(true);
                    USSItem ussitm = inv.shoplist[i].GetComponent<USSItem>();
                    ussitm.InBag = false;
                    ussitm.OriginShop.BoughtItems.Add(inv.shoplist[i].gameObject);
                }
                inv.shoplist = new List<GameObject>();
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
    public class BagSetupOpenAction : MonoBehaviour
    {
        PlayMakerFSM use;
        public ModShopBagInv inv;
        public GameObject bag;
        void Start()
        {
            StartCoroutine(Setup());
        }
        IEnumerator Setup()
        {
            yield return new WaitForSeconds(0.5f);
            use = bag.GetComponent<PlayMakerFSM>();
            use.GetState("Spawn one").InsertAction(0, new BagOpenAction
            {
                Arrays = bag.GetComponents<PlayMakerArrayListProxy>(),
                OpenAll = false,
                inv = inv
            });
            use.GetState("Spawn all").InsertAction(0, new BagOpenAction
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