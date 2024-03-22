
// Container for all constant values, like statistic IDs
namespace MonsterMashup
{
    public class ModTags
    {
        public const string NoGroundAlign = "mmash_no_ground_align";
    }

    public class ModStats
    {
        public const string Linked_Parent_Actor_UID = "MMASH_Linked_Parent_Actor_UID"; // string 
        public const string Linked_Parent_MechComp_UID = "MMASH_Linked_Parent_MechComp_UID"; // string 
        public const string Linked_Child_UID = "MMASH_Linked_Child_GUID"; // string 

        public const string Weapon_Restricted_Firing_Arc = "MMASH_RestrictedFiringArc"; // float, arc of restriction

        public const string Crush_On_Move = "MMASH_Crush_On_Move"; // bool; if true, taunt
        public const string Prevent_Melee = "MMASH_Prevent_Melee"; // bool; if true, cannot be targeted by melee pathnodes

        public const string Flee_On_Round = "MMASH_Flee_On_Round"; // int; combat ends on this turn
        // HBS Values
        public const string HBS_HeatSinkCapacity = "HeatSinkCapacity";
    }

    public class ModConsts
    {
        public const string Footprint_GO = "mmash_footprint_viz_";
        public const float MetersPerHex = 24f;
    }
}
