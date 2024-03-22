using System.Collections.Generic;

namespace MonsterMashup
{
    public class ModText
    {
        public class QuipsConfig
        {
            public List<string> Crush = new();
            public List<string> SupportSpawn = new();
        }
        public QuipsConfig Quips = new();

        public Dictionary<string, string> Labels = new Dictionary<string, string>
        {

        };

        public void LogConfig()
        {
            Mod.Log.Info?.Write("=== MOD TEXT BEGIN ===");


        }

        // Newtonsoft seems to merge values into existing dictionaries instead of replacing them entirely. So instead
        //   populate default values in dictionaries through this call instead
        public void InitUnsetValues()
        {
            if (this.Quips.Crush.Count == 0)
            {
                this.Quips.Crush = new List<string>()
                {
                    "Feel the weight of your failure",
                    "Dreams - crushed",
                    "Bigger is better",
                    "Another gnat squashed",
                    "What was that bump?",
                    "Damn, that scratched the paint",
                    "Not very nimble, are you?",
                    "Try dodging next time",
                    "You'll need a scraper for that salvage"
                };
            }

            if (this.Quips.SupportSpawn.Count == 0)
            {
                this.Quips.SupportSpawn = new List<string>()
                {
                    "Get out there and defend me!",
                    "Scrambling support units!",
                    "Launch support units!",
                    "Need coverage on the flank!",
                    "Deploy when ready!"
                };
            }
        }
    }
}
