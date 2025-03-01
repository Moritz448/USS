#if !MINI
using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using System;

namespace UniversalShoppingSystem;

internal class USSBagOpenAction : FsmStateAction
{
    public USSBagInventory BagInventory;
    public bool OpenAll = false;
    public PlayMakerArrayListProxy[] Arrays;

    private static readonly bool ESPresent = ModLoader.IsModPresent("ExpandedShop");

    private static readonly Type ModItemType = ESPresent ? Type.GetType("ExpandedShop.ModItem, ExpandedShop") : null;
    private bool CheckForModItem(Transform item) => ESPresent && item.GetComponent(ModItemType) != null;
    private void TakeModItemOut(Transform item) // For Expanded Shop
    {
        if (!ESPresent || ModItemType == null) return;
        Component modItem = item.GetComponent(ModItemType);

        ModItemType.GetField("InBag")?.SetValue(modItem, false);

        float conditionValue = Fsm.Variables.FindFsmFloat("Condition").Value;
        ModItemType.GetField("Condition")?.SetValue(modItem, conditionValue);    
    }

    private void TakeOutItem() // For USS items
    {
        Transform itm = BagInventory.BagContent[0].transform;
        itm.position = new Vector3(Fsm.GameObject.transform.position.x, Fsm.GameObject.transform.position.y + 0.1f, Fsm.GameObject.transform.position.z);
        itm.eulerAngles = Vector3.zero;
        itm.gameObject.SetActive(true);

        USSItem ussitm;
        if (ussitm = itm.GetComponent<USSItem>()) // If its an USS item...
        {
            ussitm.InBag = false;
            ussitm.Condition = Fsm.Variables.FindFsmFloat("Condition").Value;
            ussitm.StartSpoiling();
        }
        else if (ESPresent && CheckForModItem(itm)) TakeModItemOut(itm); // else it has to be an expanded shop item.

        BagInventory.BagContent.Remove(itm.gameObject);
        if (CheckVanillaEmpty()) MasterAudio.PlaySound3DAndForget("HouseFoley", BagInventory.transform, false, 1f, 1f, 0f, "plasticbag_open2");

        if (BagInventory.BagContent.Count == 0)
        {
            if (CheckVanillaEmpty()) Fsm.Event("GARBAGE");
            UnityEngine.Object.Destroy(BagInventory);
        }            
        
        Fsm.Event("FINISHED");
    }

    public override void OnEnter()
    {
        if (!OpenAll && BagInventory.BagContent.Count > 0)
        {
            TakeOutItem();
            return;
        }
        
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
                    ussitm.Condition = Fsm.Variables.FindFsmFloat("Condition").Value;
                    ussitm.StartSpoiling();
                }
                else if (ESPresent) TakeModItemOut(BagInventory.BagContent[i].transform); // Else it has to be an expanded shop item.
            }

            BagInventory.BagContent.Clear();

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

        for (int i = 0; i < array.Length; i++) foreach (int array2 in array[i].arrayList) if (array2 > num) num = array2; 
        
        return num == 0;
    }
}
#endif