using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterMashup.AI
{
    public static class MM_AI_ImmobileTurret
    {
        public static BehaviorNode InitRootNode(BehaviorTree behaviorTree, AbstractActor unit, GameInstance game)
        {

            SortEnemiesByThreatNode sortEnemiesByThreatNode = new SortEnemiesByThreatNode("sort_enemies_by_threat", behaviorTree, unit);
            ExecuteStationaryAttackNode executeStationaryAttackNode = new ExecuteStationaryAttackNode("execute_stationary_attack", behaviorTree, unit);
            SequenceNode shootNode = new SequenceNode("sort_then_shoot", behaviorTree, unit);
            shootNode.AddChild(sortEnemiesByThreatNode);
            shootNode.AddChild(executeStationaryAttackNode);

            BraceNode braceNode = new BraceNode("brace_node", behaviorTree, unit);
            SelectorNode selectorNode = new SelectorNode("fire_or_brace", behaviorTree, unit);
            selectorNode.AddChild(shootNode);
            selectorNode.AddChild(braceNode);

            return selectorNode;
        }
    }
}
