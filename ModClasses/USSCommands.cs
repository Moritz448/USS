using MSCLoader;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalShoppingSystem
{
    public class USSCommands : ConsoleCommand
    {
        List<GameObject> shopGameObjects = new List<GameObject>();

        public override string Name => "uss";
        public override string Help => "'uss list': Returns a list of all loaded USS shops with their index required for uss move\n'uss move [index]': Parents a USS shop specified to store_inside to make positioning via Developer Toolset easier. When finished, copy the transform values with devtoolset and enter them in the unity component.";

        public override void Run(string[] args)
        {
            switch (args[0])
            {
                case "list":
                    List<ItemShop> shops = GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>().Shops;
                    ModConsole.Log("All USS shops:");
                    for (int i = 0; i < shops.Count; i++)
                    {
                        shopGameObjects.Add(shops[i].gameObject);
                        ModConsole.Log(i + ": " + shops[i].gameObject.name);
                    }                    
                    break;

                case "move":
                    if (args[1] == "")
                    {
                        ModConsole.Log("Please specify the shop index ('uss shop list')");
                        break;
                    }
                    GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>().Shops[int.Parse(args[1])].transform.SetParent(GameObject.Find("STORE").transform.Find("LOD").transform.Find("GFX_Store").transform.Find("store_inside"), true);
                    ModConsole.Log("Parented shop to store_inside; Adjust position to your likings and change values in unity script to your values");
                    break;

                default:
                    ModConsole.Log("Please specify at least one argument");
                    break;
            }
        }
    }
}