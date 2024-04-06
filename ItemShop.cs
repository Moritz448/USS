using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using MSCLoader;
using System.Collections;
using System.Linq;
using HutongGames.PlayMaker.Actions;


namespace UniversalShoppingSystem
{
    public class ItemShop : MonoBehaviour
    {
        // Settings to change in Unity
        [Header("Shop Settings")]
        public string ItemName = "Shop Name";
        public float ItemPrice;
        [Header("Relative to store_inside")]
        public Vector3 TriggerPosition;
        public Vector3 TriggerRotation;

        public GameObject ItemPrefab;
        public bool SpawnInBag;

        [HideInInspector]
        public List<GameObject> BoughtItems = new List<GameObject>(); // Used for saving, no need to fill up in inspector!

        [HideInInspector]
        private Vector3 bigItemSpawnPosition = new Vector3(-1551.303f, 4.88f, 1181.904f);

        // Only important on runtime, handled automatically. No need to change anything here!
        [HideInInspector]
        public int Stock;
        [HideInInspector]
        public int Cart;
        private int itemsBought;

        private PlayMakerFSM register; // Required to hook paying mechanics
        private PlayMakerFSM registerData;
        private GameObject vanillaShopInventory; // Required to hook restock mechanics


        public class CreateBagAction : FsmStateAction
        {
            public ItemShop shop;
            public FsmInt check;

            public override void OnEnter()
            {
                if (shop.Cart > 0) if (check.Value == 0) Fsm.Event("FINISHED");
                Finish();
            }
        }

        public class RestockAction : FsmStateAction
        {
            public ItemShop shop;
            public override void OnEnter()
            {
                shop.Restock();
                Finish();
            }
        }

        private void Awake()
        {
            if (!GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>()) GameObject.Find("PLAYER").AddComponent<ItemShopRaycast>();
            GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>().Shops.Add(this); // Add shop for commands etc

            register = GameObject.Find("STORE/StoreCashRegister/Register").GetComponent<PlayMakerFSM>();
            vanillaShopInventory = GameObject.Find("STORE/Inventory");
            registerData = GameObject.Find("StoreCashRegister").transform.GetChild(2).GetPlayMaker("Data");
            registerData.InitializeFSM();

            GameHook.InjectStateHook(register.gameObject, "Purchase", () => { Pay(); });
            vanillaShopInventory.GetPlayMaker("Logic").GetState("Items").InsertAction(0, new RestockAction { shop = this });

            this.transform.SetParent(GameObject.Find("STORE").transform.Find("LOD").transform.Find("GFX_Store").transform.Find("store_inside"), false);
            this.transform.localEulerAngles = TriggerRotation;
            this.transform.localPosition = TriggerPosition;
            this.transform.SetParent(null, true); // Needs to be parented to root in order to stay active all the time to not cause problems with restocking; Might change.

            Stock = this.transform.childCount;
            Cart = 0;
            itemsBought = 0;
            Restock(); // Might change later for more convenient save loading

            if (SpawnInBag) // Only setup the whole stuff when items should spawn in bags
            {
                // Bag Spawning Setup
                GameObject store = GameObject.Find("STORE");
                PlayMakerFSM fsm = store.transform.Find("LOD/ShopFunctions/BagCreator").GetPlayMaker("Create");
                fsm.InitializeFSM();
                fsm.GetState("Copy contents").InsertAction(0, new USSBagSetupAction
                {
                    bag = (fsm.GetState("Copy contents").Actions.First(action => action is ArrayListCopyTo) as ArrayListCopyTo).gameObjectTarget.GameObject,
                    shop = this
                });
                // Abusing the oil filter shop for our purposes
                registerData.GetState("Oil filter").InsertAction(0, new CreateBagAction
                {
                    shop = this,
                    check = registerData.FsmVariables.FindFsmInt("QOilfilter")
                });
            }
        }

        private void Restock()
        {
            StartCoroutine(RestockShop());
            itemsBought = 0;
            Stock = this.gameObject.transform.childCount;
        }

        private IEnumerator RestockShop()
        {
            for (int i = 0; i < this.gameObject.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(true);

            yield return null;
        }

        public void SpawnBag(USSBagInventory inv)
        {
            if (this.SpawnInBag) StartCoroutine(BagSpawner(inv));
        }

        IEnumerator BagSpawner(USSBagInventory invent)
        {
            yield return new WaitForSeconds(0.31f);
            itemsBought += Cart;
            while (Cart > 0)
            {
                Cart--;
                GameObject item = GameObject.Instantiate(ItemPrefab);
                item.SetActive(false);
                invent.BagContent.Add(item);
                BoughtItems.Add(item); // For Saving/Loading
                USSItem ussitm = item.GetComponent<USSItem>();
                ussitm.InBag = true;
                ussitm.OriginShop = this;
                yield return null;
            }

            yield break;
        }

        public void Buy() // Put item in Cart
        {
            Stock--;
            Cart++;

            this.transform.GetChild(Cart - 1 + itemsBought).gameObject.SetActive(false);

            register.FsmVariables.GetFsmFloat("PriceTotal").Value += ItemPrice;
            register.SendEvent("PURCHASE");
        }

        public void Unbuy() // Take item back out of Cart
        {
            this.transform.GetChild(Cart - 1 + itemsBought).gameObject.SetActive(true);
            Stock++;
            Cart--;

            register.FsmVariables.GetFsmFloat("PriceTotal").Value -= ItemPrice;
            register.SendEvent("PURCHASE");
        }

        private void Pay()
        {
            if (this.Cart > 0)
            {
                // Ability to register actions on buying the stuff
            }

            if (!this.SpawnInBag) // When its a big item...
            {
                for (int i = 0; i < Cart; i++)
                {
                    GameObject item = GameObject.Instantiate(ItemPrefab);
                    this.BoughtItems.Add(item);
                    item.transform.position = bigItemSpawnPosition;
                    item.GetComponent<Rigidbody>().isKinematic = false;
                    item.SetActive(true);
                }
                itemsBought += Cart;
                Cart = 0;
            }
        }
    }
}