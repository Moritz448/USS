using UnityEngine;

namespace UniversalShoppingSystem
{
    public class USSItem : MonoBehaviour
    {
        [HideInInspector]
        public bool InBag;
        public string BagID;
        public ItemShop OriginShop;
    }
}