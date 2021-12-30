using System.Collections.Generic;

namespace MonsterMashup
{
    public class ModText
    {
       

        public Dictionary<string, string> Labels = new Dictionary<string, string>
        {

        };

        // Newtonsoft seems to merge values into existing dictionaries instead of replacing them entirely. So instead
        //   populate default values in dictionaries through this call instead
        public void InitUnsetValues()
        {
           
        }
    }
}
