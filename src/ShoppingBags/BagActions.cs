#if !MINI
using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using System.Collections;

namespace UniversalShoppingSystem;

internal class USSBagSetupAction : FsmStateAction
{
    public FsmGameObject Bag;
    public ItemShop Shop;
    public override void OnEnter()
    {
        if (Bag?.Value == null || Shop == null)
        {
            Finish();
            return;
        }

        USSBagInventory BagInventory = Bag.Value.GetComponent<USSBagInventory>()
                                   ?? Bag.Value.AddComponent<USSBagInventory>();

        if (Shop.SpawnInBag) Shop.SpawnBag(BagInventory);

        if (!Bag.Value.GetComponent<USSBagSetupOpenAction>())
        {
            USSBagSetupOpenAction act = Bag.Value.AddComponent<USSBagSetupOpenAction>();
            act.Bag = Bag.Value;
            act.BagInventory = BagInventory;
        }

        Finish();
    }
}
internal class USSBagSetupOpenAction : MonoBehaviour
{
    private PlayMakerFSM use;
    public USSBagInventory BagInventory;
    public GameObject Bag;

    void Start() => StartCoroutine(Setup());
    
    private IEnumerator Setup()
    {
        yield return new WaitForSeconds(0.4f);

        use = Bag.GetComponent<PlayMakerFSM>();

        use.GetState("Spawn one").InsertAction(0, new USSBagOpenAction
        {
            Array = Bag.GetArrayListProxy("Values"),
            OpenAll = false,
            BagInventory = BagInventory
        });

        use.GetState("Spawn all").InsertAction(0, new USSBagOpenAction
        {
            Array = Bag.GetArrayListProxy("Values"),
            OpenAll = true,
            BagInventory = BagInventory
        });

        Object.Destroy(this);

        yield break;
    }
}
#endif