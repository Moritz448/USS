using UnityEngine;
using System.Linq;
#if !MINI
using MSCLoader;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
#endif

namespace UniversalShoppingSystem;

public class FleamarketSellable : MonoBehaviour
{
    [Header("WARNING: Flea market works name-based")]
    [Header("If name changes at runtime, defaults to max 5 Mk (vanilla default)")]
    public float MaxSalePrice;

#if !MINI
    private void Start()
    {
        MaxSalePrice = Mathf.Max(0, MaxSalePrice);

        Transform saleTable = GameObject.Find("FleaMarket").transform.Find("SaleTable");

        PlayMakerHashTableProxy priceGuide = saleTable.GetComponents<PlayMakerHashTableProxy>().First(x => x.referenceName == "PriceGuide");

        string name = gameObject.name.ToLower();
        name = name.Substring(0, name.Length - 7); // remove (xxxxx)

        if (!priceGuide.hashTable.ContainsKey(name))
        {
            priceGuide.hashTable.Add(name, MaxSalePrice);
            priceGuide.TakeSnapShot();
        }

        PlayMakerFSM fleamarketLogic = saleTable.GetPlayMaker("Logic");
        FsmState removeObject = fleamarketLogic.GetState("Remove object");

        SendEventByName action = removeObject.GetAction<SendEventByName>(1);

        if (!removeObject.Actions.Any(a => a is DestroySoldObject)) removeObject.InsertAction(4, new DestroySoldObject(action));

        Object.Destroy(this);               
    }

    private class DestroySoldObject(SendEventByName Action) : FsmStateAction
    {
        public override void OnEnter()
        {
            GameObject objToDestroy = Action.eventTarget.gameObject.GameObject.Value;
            if (objToDestroy != null) GameObject.Destroy(objToDestroy);
            Finish();
        }
    }
#endif
}