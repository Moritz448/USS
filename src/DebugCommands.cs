#if !MINI
using System.Linq;
using MSCLoader;
using UnityEngine;

namespace UniversalShoppingSystem;

public class DebugCommands : ConsoleCommand
{
    public override string Name => "uss";
    public override string Help => "Usage:\ndebug <shopID>\nprints debug info for each shop, or specified shopID";

    public override void Run(string[] args)
    {
        if (args.Length > 0 && args[0] == "debug")
        {
            if (ModLoader.CurrentScene != CurrentScene.Game)
            {
                ModConsole.LogError("Must be in Game!");
                return;
            }
            if (args.Length == 1) GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>()?.Shops?.ForEach(shop => shop.PrintDebugInfo());
            else if (args.Length == 2) GameObject.Find("PLAYER").GetComponent<ItemShopRaycast>()?.Shops?.Where(shop => shop.ShopID == args[1]).ToList().ForEach(shop => shop.PrintDebugInfo());

        }
        else ModConsole.Log(Help);
    }
}
#endif