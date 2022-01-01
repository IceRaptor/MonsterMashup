# MonsterMashup
This mod for the [HBS BattleTech](http://battletechgame.com/) game 

## TODO
- Link init of units. Maybe use support unit construct to avoid init tracking?
  - Otherwise, have to sync
- Should component death == dead turret? In case of location destruction, yes
- On parent death, kill turret
- Allow setting LoS on component
- How to allow mutable pilotDefs?
- Handle units that spawn during play (do we want to allow this)
- Should we be able to disable 'back' / 'side' bonuses?
- Mark spaces as occupied; maybe
- Clicking on model can be pretty tough; have to use tab to select reliably
- Rename spawned turret GO to reflect parent + attach? 
- Camera can fly into the mesh

public void RebuildPathingForNoMovement()
{
	List<Rect> list = new List<Rect>();
	float num = Radius * 2f;
	list.Add(Rect.MinMaxRect(CurrentPosition.x - num, CurrentPosition.z - num, CurrentPosition.x + num, CurrentPosition.z + num));
	Combat.MessageCenter.PublishMessage(new RecomputePathingMessage(GUID, list));
}