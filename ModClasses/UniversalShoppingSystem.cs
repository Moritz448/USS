using MSCLoader;

namespace UniversalShoppingSystem
{
    public class UniversalShoppingSystem : Mod
    {
        public override string ID => "USS_DummyMod"; //Your mod ID (unique)
        public override string Name => "USS Dummy Mod"; //You mod name
        public override string Author => "Honeycomb936"; //Your Username
        public override string Version => "0.1"; //Version
        public override string Description => "USS is not a mod. Move it to the References folder or delete it and let MSCLoader download the latest version automatically."; //Short description of your mod

        public override void ModSetup()
        {
            SetupFunction(Setup.OnMenuLoad, Mod_OnMenuLoad);
        }

        private void Mod_OnMenuLoad()
        {
            ModUI.ShowCustomMessage("USS is not a mod. Move it to the References folder or delete it and let MSCLoader download the latest version automatically.", "READ ME", new MsgBoxBtn[]
                {
                    ModUI.CreateMessageBoxBtn("I will", () => { }, false)
                });
        }
    }
}