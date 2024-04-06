using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using System.Collections;

namespace UniversalShoppingSystem
{
    public class USSBagSetupAction : FsmStateAction
    {
        public FsmGameObject Bag;
        public ItemShop Shop;
        public override void OnEnter()
        {
            USSBagInventory BagInventory = Bag.Value.GetComponent<USSBagInventory>();
            if (BagInventory == null)
            {
                BagInventory = Bag.Value.AddComponent<USSBagInventory>();
                if (Shop.SpawnInBag) Shop.SpawnBag(BagInventory);

                USSBagSetupOpenAction act = Bag.Value.AddComponent<USSBagSetupOpenAction>();
                act.Bag = Bag.Value;
                act.BagInventory = BagInventory;
            }
            else if (Shop.SpawnInBag) Shop.SpawnBag(BagInventory);
                
            Finish();
        }
    }
    public class USSBagSetupOpenAction : MonoBehaviour
    {
        PlayMakerFSM use;
        public USSBagInventory BagInventory;
        public GameObject Bag;

        void Start() => StartCoroutine(Setup());
        
        private IEnumerator Setup()
        {
            yield return new WaitForSeconds(0.4f);

            use = Bag.GetComponent<PlayMakerFSM>();

            use.GetState("Spawn one").InsertAction(0, new USSBagOpenAction
            {
                Arrays = Bag.GetComponents<PlayMakerArrayListProxy>(),
                OpenAll = false,
                BagInventory = BagInventory
            });

            use.GetState("Spawn all").InsertAction(0, new USSBagOpenAction
            {
                Arrays = Bag.GetComponents<PlayMakerArrayListProxy>(),
                OpenAll = true,
                BagInventory = BagInventory
            });

            Object.Destroy(this);

            yield break;
        }
    }
}