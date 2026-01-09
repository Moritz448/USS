#if !MINI
using MSCLoader;

namespace UniversalShoppingSystem;

public class UniversalShoppingSystem : Mod
{
    public override string ID => "USS_DummyMod";
    public override string Name => "USS Dummy Mod"; 
    public override string Author => "Honeycomb936";
    public override string Version => "1.0";
    public override string Description => "USS is not a mod. Move it to the References folder.";
    public override Game SupportedGames => Game.MySummerCar_And_MyWinterCar;

    public override void ModSetup() => SetupFunction(Setup.OnMenuLoad, Mod_OnMenuLoad);
    
    private void Mod_OnMenuLoad() => ModUI.ShowCustomMessage("USS is not a mod. Move it to the References folder.",
        "READ ME", [ModUI.CreateMessageBoxBtn("I will", () => { }, false)]);  
}
#endif