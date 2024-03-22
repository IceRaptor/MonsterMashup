# MonsterMashup
This mod for the [HBS BattleTech](http://battletechgame.com/) game that allows mod makers to 'mash' multiple CustomUnit units into a single entity. This allows for 'boss' units, such as the Overlord or Union dropships. These units have a central core unit, with attached units that are fixed in place around it. If the central core dies, all linked units die as well. 

This mod requires strong coordination between modeller and modder. Many connections are made using Unity transform names, which can be found in the Unity Editor or AssetBundles. It's a brittle ecosystem that works well enough, but be prepared for some flakiness.

:exclamation: This mod requires, and could not exist without KMission's [CustomUnits](https://github.com/BattletechModders/CustomAmmoCategories/). Many thanks to the Omnissiah for making this possible!

## Implementation
The core unit will be referred to as the parent, and children as `linkedActors`. LinkedActors are defined in HBS UpdateDefs. The UpdateDef needs a Custom Components block for the component MM_LinkedActor. The properties of this component are:

* **CUVehicleDefID**: The CustomUnits vehicle you want to attach to the parent
* **PilotDefId**: The pilotDef you want spawned in control of the CUVehicleDefId
* **AttachPoint**: The transform name where the linkedActor should be attached to the parent. Throws an error if this is undefined, or missing.
* **LinkInitiative**: If true, the base initiative value of linkedActors will be overriden with the base initiative of the parent unit.

When the parent is spawned, the child's position and rotation will be aligned to the **local** orientation of the AttachPoint on the parent. The child's position will be updated every time the parent moves to keep this relationship linked. You're strongly encouraged to add the **irbtmu_immobile_unit** tag to linkedActors to prevent them from moving on their own. 

UpdateDefs are used because they send a signal when the unit is destroyed that allows the children to be cleaned up.

An example of this binding can be found below:

```json
  "Custom": 
  {
	"MM_LinkedActor": 
	{
	  "CUVehicleDefId": "vehicledef_OVERLORD_TURRET_AFT",
	  "PilotDefId": "pilot_d3_turret",
	  "AttachPoint": "turret_attach_aft_left",
	  "LinkInitiative": true
	}
  },
  "Description": 
  {
    ...
    "Id": "Gear_Overlord_Turret_Aft_Left",
    "Name": "Overlord Aft Turret",
    "Details": "The aft left turret on the fearsome Overlord drop ship.",
    "Icon": "uixSvgIcon_special_Equipment"
  },  

```


### Parent Notes

Parents are treated like any other unit, with a few exceptions:

If a large **Radius** value is defined in the parent's *vehiclechassisdef*, access to the hexes within that radius will be prohibited by this mod. Units can move next to it, but won't be able to reach it's internal position. This effectively prevents melee attacks in most cases. HBS sets most radius values to 4-8.

If you set the **MM_PreventMelee** CustomComponent on the chassis, any attempt to find a melee position will fail. This should automatically as per the note above, but this is a safeguard for units that have small radius values.

You can set the **mmash_no_ground_align** tag in a parent vehicledef to prevent it from aligning to the terrain. This is only useful with static parents like the Dropships, but prevents them from tilting like the Tower of Pisa.

#### MM_CombatSpawn Component

The **MM_CombatSpawn** CustomComponent allows you to define spawns that occur one at time during combat. These will be spawned near transform points on the model when the parent is in combat. The component itself takes a single property named *Spawns*. You MUST NOT use multiple spawns for a single transform. Each transform will spawn one unit at a time, that it. The properties for a *Spawn* are:

* **CUVehicleDefID**: The CustomUnits vehicle you want to attach to the parent
* **PilotDefId**: The pilotDef you want spawned in control of the CUVehicleDefId
* **AttachPoint**: The transform name where the linkedActor should be attached to the parent. Throws an error if this is undefined, or missing.
* **MaxSpawns**: The total number of vehicleDefs that will be spawned from this transform.
* **RoundsBetweenSpawns**: The number of rounds to wait between spawns on this transform. Out of combat turns count towards this limit. 

An example of use:

```json
	  "MM_CombatSpawn" : {
		"Spawns" : [
		  { "AttachPoint": "overlord_spawn_lower_front", "CUVehicleDefId" : "vehicledef_DEMOLISHER", "PilotDefId" : "pilot_d10_brawler", "MaxSpawns": 4, "RoundsBetweenSpawns": 2 },
		  { "AttachPoint": "overlord_spawn_lower_rear", "CUVehicleDefId" : "vehicledef_DEMOLISHER", "PilotDefId" : "pilot_d10_brawler", "MaxSpawns": 4, "RoundsBetweenSpawns": 2 },
		  { "AttachPoint": "overlord_spawn_lower_left", "CUVehicleDefId" : "vehicledef_DEMOLISHER", "PilotDefId" : "pilot_d10_brawler", "MaxSpawns": 4, "RoundsBetweenSpawns": 2 },
		  { "AttachPoint": "overlord_spawn_lower_right", "CUVehicleDefId" : "vehicledef_DEMOLISHER", "PilotDefId" : "pilot_d10_brawler", "MaxSpawns": 4, "RoundsBetweenSpawns": 2 },
		  { "AttachPoint": "overlord_spawn_lower_helipad", "CUVehicleDefId" : "vehicledef_YELLOWJACKET", "PilotDefId" : "pilot_d10_brawler", "MaxSpawns": 4, "RoundsBetweenSpawns": 1 }
		]
	  }
```

#### MM_CrushOnCollision Component

The **MM_CrushOnCollision** CustomComponent allows you to define an auto-kill radius for the parent unit. Any *enemy* units with the defined radius will insta-killed for each position update of the parent. The pilot will be killed, with a source type of Dropship and label of 'Crushed!'. The properties for this component are:

* **Radius**: The radius from the center-point of the model that suffers the insta-kill effect. 
* **PlayTaunt**: If true, a randomly selected quote from the Crushed quips in mod_localized_text will be displayed when an enemy unit is destroyed.
* **ShowRadius**: If true, a simple ring will be displayed around the parent indicating the approximate crush radius defined. Requires **RadiusTransform** to be set as well.
* **RadiusTransform**: The transformID for the crush radius mesh. 

An example of use:

```json
	  "MM_CrushOnCollision": {
		"Taunt" : true,
		"Radius" : 100,
		"ShowRadius" : true,
		"RadiusTransform" : "crush_warning_ring"
	  },
```

### LinkedActor Notes

LinkedActors are the same as any other CU Vehicledef. It's recommended to use the `CustomParts.UnitTypeName = "Turret"` to have the paperdoll display a Turret instead of a vehicle, but that's up to you. 


### MM_WeaponController Component

The **MM_WeaponController** CustomComponent allows modders to define weapon-specific arcs. These arcs use the orientation and position of a defined transform on the model. Transforms are linked to weapons through the HardpointID specified in the *vehicledef*. This mapping is fickle and prone to breakage. If you define a hardpoint in a MM_WeaponController, it MUST exist in the vehicledef. I've used hardpoints up to 27 in the vehicledef without worry. You can use hardpointIDs of -1 for anything you don't want to bind. 

The component takes a single property called *AttachMappings*. Each *AttachMapping* has the following properties:  
  
  * **Transform**: The attach transform that should be used to set the orientation and position of the weapon.
  * **HardpointID**: An array of hardpointsIDs in the vehicledef that should all use this transform and restriction.
  * **RestrictedFiringArc**: The weapon-specific firing arc to set. Make sure this transform and arc isn't outside the model's overall firing arc!

:exclamation: If you use this value, you almost certainly want to set the parent or linkedActor to have a 360 firing arc in their CustomParts descriptor!

An example of use:  
  
 ```json  
 vehicledef:
 
 		{
		  "ComponentDefID": "Weapon_Laser_LargeLaser_0-STOCK",
		  "ComponentDefType": "Weapon",
		  "MountedLocation": "Front",		  
		  "HardpointSlot": 1,
		  "DamageLevel" : "Functional"
		},		
		{
		  "ComponentDefID": "Weapon_Laser_LargeLaser_0-STOCK",
		  "ComponentDefType": "Weapon",
		  "MountedLocation": "Front",		  
		  "HardpointSlot": 2,
		  "DamageLevel" : "Functional"
		},				
		{
		  "ComponentDefID": "Weapon_Laser_LargeLaser_0-STOCK",
		  "ComponentDefType": "Weapon",
		  "MountedLocation": "Front",		  
		  "HardpointSlot": 3,
		  "DamageLevel" : "Functional"
		},	
		{
		  "ComponentDefID": "Weapon_Ballistic_AMS_0-STOCK",
		  "ComponentDefType": "Weapon",
		  "MountedLocation": "Front",		  
		  "HardpointSlot": 4,
		  "DamageLevel" : "Functional"
		},		
		{
		  "ComponentDefID": "Weapon_Ballistic_AMS_0-STOCK",
		  "ComponentDefType": "Weapon",
		  "MountedLocation": "Front",		  
		  "HardpointSlot": 5,
		  "DamageLevel" : "Functional"
		},	
 
 vehiclechassisdef:
 	  "MM_WeaponController": {
		  "AttachMappings" : [
			{ "Transform": "weapons_attach_body_front_center", "HardpointID": [1], "RestrictedFiringArc": 45 },
			{ "Transform": "weapons_attach_front_forward_left_turret", "HardpointID": [2], "RestrictedFiringArc": 45 },
			{ "Transform": "weapons_attach_front_forward_right_turret", "HardpointID": [3], "RestrictedFiringArc": 45 },
			{ "Transform": "weapons_attach_front_upperdeck_left_ams", "HardpointID": [4], "RestrictedFiringArc": 90 },
			{ "Transform": "weapons_attach_front_upperdeck_right_ams", "HardpointID": [5], "RestrictedFiringArc": 90 }
			]
		}
 ```



## Modeller Notes

Building a MonsterMash unit is fairly straightforward, but there are some things to be aware of:  
  
* If the parent's collider eclipses a child collider, the parent wins. This can make the child unclickable by players
* Cameras can fly in meshes. I don't care enough to fix this
