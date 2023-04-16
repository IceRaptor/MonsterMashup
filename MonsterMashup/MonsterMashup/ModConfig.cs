namespace MonsterMashup
{

    public class ModConfig
    {
        public bool Debug = false;
        public bool Trace = false;

        public DeveloperOpts DeveloperOptions = new DeveloperOpts();

        public bool LinkChildInitiative = true;

        public void LogConfig()
        {
            Mod.Log.Info?.Write("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info?.Write($"  Debug: {this.Debug} Trace: {this.Trace}");
            Mod.Log.Info?.Write($"  LinkChildInitiative: {LinkChildInitiative}");
            Mod.Log.Info?.Write("");

            Mod.Log.Info?.Write(" -- Developer Options --");
            Mod.Log.Info?.Write($" EnableFootprintVisualization: {this.DeveloperOptions.EnableFootprintVis}");
            Mod.Log.Info?.Write($" DebugDamageLocationKillsLinkedTurrets: {this.DeveloperOptions.DebugDamageLocationKillsLinkedTurrets}");

            Mod.Log.Info?.Write("=== MOD CONFIG END ===");
        }

        public class DeveloperOpts
        {
            public bool EnableFootprintVis = true;
            public bool DebugDamageLocationKillsLinkedTurrets = true;

        }

        // Newtonsoft seems to merge values into existing dictionaries instead of replacing them entirely. So instead
        //   populate default values in dictionaries through this call instead
        public void InitUnsetValues()
        {
        }
    }


}
