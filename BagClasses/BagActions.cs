using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using System.Collections;

namespace UniversalShoppingSystem
{
    public class USSBagSetupAction : FsmStateAction
    {
        public FsmGameObject bag;
        public ItemShop shop;
        public override void OnEnter()
        {
            USSBagInventory inv = bag.Value.GetComponent<USSBagInventory>();
            if (inv == null)
            {
                inv = bag.Value.AddComponent<USSBagInventory>();
                if (shop.SpawnInBag) shop.SpawnBag(inv);
                USSBagSetupOpenAction act = bag.Value.AddComponent<USSBagSetupOpenAction>();

                act.bag = bag.Value;
                act.inv = inv;
            }
            else if (shop.SpawnInBag) shop.SpawnBag(inv);
                
            Finish();
        }
    }
    public class USSBagSetupOpenAction : MonoBehaviour
    {
        PlayMakerFSM use;
        public USSBagInventory inv;
        public GameObject bag;

        void Start() => StartCoroutine(Setup());
        
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