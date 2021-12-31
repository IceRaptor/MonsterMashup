using CustomComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterMashup.Component
{
    [CustomComponent("LinkedTurret")]
    internal class LinkedTurretComponent : SimpleCustomComponent
    {
        public string TurretDefId = "";
        public string PilotDefId = "";

        public string AttachPoint = "";

        public bool LinkInitiative = true;
    }

}
