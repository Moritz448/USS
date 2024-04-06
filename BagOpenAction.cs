using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using System.Collections.Generic;

using ExpandedShop;

namespace UniversalShoppingSystem
{
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
            moditm.Condition = Fsm.Variables.FindFsmFloat("Condition").Value;
        }

        private void TakeOutOne()
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
                ussitm.Condition = Fsm.Variables.FindFsmFloat("Condition").Value;
            }
            else if (ModLoader.IsModPresent("ExpandedShop") && CheckForModItem(itm)) TakeModItemOut(itm); // else it has to be an expanded shop item.

            inv.BagContent.Remove(itm.gameObject);

            if (CheckVanillaEmpty()) MasterAudio.PlaySound3DAndForget("HouseFoley", inv.transform, false, 1f, 1f, 0f, "plasticbag_open2");

            FsmVariables.GlobalVariables.FindFsmString("GUIinteraction").Value = "";
            if (inv.BagContent.Count == 0)
            {
                if (CheckVanillaEmpty()) Fsm.Event("GARBAGE");
                Object.Destroy(inv);
            }
            Fsm.Event("FINISHED");

        }
        public override void OnEnter()
        {
            if (!OpenAll)
            {
                if (inv.BagContent.Count > 0)
                {
                    TakeOutOne();
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
}