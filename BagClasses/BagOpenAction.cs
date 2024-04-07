using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;

using ExpandedShop;

namespace UniversalShoppingSystem
{
    public class USSBagOpenAction : FsmStateAction
    {
        public USSBagInventory BagInventory;
        public bool OpenAll = false;
        public PlayMakerArrayListProxy[] Arrays;

        private bool CheckForModItem(Transform item) { return item.GetComponent<ModItem>(); }
        private void TakeModItemOut(Transform item) // For Expanded Shop
        {
            ModItem moditm = item.GetComponent<ModItem>();
            moditm.InBag = false;
            moditm.Condition = Fsm.Variables.FindFsmFloat("Condition").Value;
        }

        private void TakeOutItem() // For USS items
        {
            Transform itm = BagInventory.BagContent[0].transform;
            itm.position = new Vector3(Fsm.GameObject.transform.position.x, Fsm.GameObject.transform.position.y + 0.1f, Fsm.GameObject.transform.position.z);
            itm.eulerAngles = Vector3.zero;
            itm.gameObject.SetActive(true);

            if (itm.GetComponent<USSItem>()) // If its an USS item...
            {
                USSItem ussitm = itm.GetComponent<USSItem>();
                ussitm.InBag = false;
                ussitm.OriginShop.BoughtItems.Add(itm.gameObject);
                ussitm.Condition = Fsm.Variables.FindFsmFloat("Condition").Value;
                ussitm.StartSpoiling();
            }
            else if (ModLoader.IsModPresent("ExpandedShop") && CheckForModItem(itm)) TakeModItemOut(itm); // else it has to be an expanded shop item.

            BagInventory.BagContent.Remove(itm.gameObject);

            if (CheckVanillaEmpty()) MasterAudio.PlaySound3DAndForget("HouseFoley", BagInventory.transform, false, 1f, 1f, 0f, "plasticbag_open2");

            FsmVariables.GlobalVariables.FindFsmString("GUIinteraction").Value = "";
            if (BagInventory.BagContent.Count == 0)
            {
                if (CheckVanillaEmpty()) Fsm.Event("GARBAGE");
                Object.Destroy(BagInventory);
            }

            itm.GetComponent<USSItem>().OriginShop.TookOutOfBag(); // Run user-provided actions
            Fsm.Event("FINISHED");

        }
        public override void OnEnter()
        {
            if (!OpenAll && BagInventory.BagContent.Count > 0) TakeOutItem();
            
            if (OpenAll)
            {
                for (int i = 0; i < BagInventory.BagContent.Count; i++)
                {
                    BagInventory.BagContent[i].transform.position = BagInventory.gameObject.transform.position;
                    BagInventory.BagContent[i].transform.eulerAngles = Vector3.zero;
                    BagInventory.BagContent[i].SetActive(true);

                    USSItem ussitm = BagInventory.BagContent[i].GetComponent<USSItem>();
                    if (ussitm != null) // If its an USS item...
                    {
                        ussitm.InBag = false;
                        ussitm.OriginShop.BoughtItems.Add(BagInventory.BagContent[i].gameObject);
                        ussitm.Condition = Fsm.Variables.FindFsmFloat("Condition").Value;
                        ussitm.StartSpoiling();
                        ussitm.OriginShop.TookOutOfBag(); // Run user-provided actions
                    }
                    else if (ModLoader.IsModPresent("ExpandedShop")) TakeModItemOut(BagInventory.BagContent[i].transform); // Else it has to be an expanded shop item.
                }

                BagInventory.BagContent.Clear();
                FsmVariables.GlobalVariables.FindFsmString("GUIinteraction").Value = "";
                if (CheckVanillaEmpty())
                {
                    Fsm.Event("GARBAGE");
                    MasterAudio.PlaySound3DAndForget("HouseFoley", BagInventory.transform, false, 1f, 1f, 0f, "plasticbag_open1");
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