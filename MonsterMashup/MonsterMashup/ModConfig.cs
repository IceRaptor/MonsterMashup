using BattleTech;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonsterMashup
{

    public class ModConfig
    {

        public bool Debug = false;
        public bool Trace = false;

        public void LogConfig()
        {
            Mod.Log.Info?.Write("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info?.Write($"  Debug: {this.Debug} Trace: {this.Trace}");
            Mod.Log.Info?.Write("");

            Mod.Log.Info?.Write("=== MOD CONFIG END ===");
        }

        // Newtonsoft seems to merge values into existing dictionaries instead of replacing them entirely. So instead
        //   populate default values in dictionaries through this call instead
        public void InitUnsetValues()
        {
        }
    }

   
}
