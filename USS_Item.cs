using UnityEngine;

namespace UniversalShoppingSystem
{
    public class USSItem : MonoBehaviour
    {
        [HideInInspector]
        public bool InBag;
        [HideInInspector]
        public string BagID;
        [HideInInspector]
        public ItemShop OriginShop;
        [HideInInspector]
        public float Condition;
    }
}