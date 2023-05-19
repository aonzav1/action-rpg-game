# Action-rpg-game

**A game prototype of action RPG game by Aon Burapa. (aon3975)** <br>
brief: A small map that contain 1 player character and 1 monster. The monster is AI that will attack player

I extend from the brief above, I've created a map with 3 consequentive rooms
1. Start room : consists of dummy enemies to test basic functionality of hit, multiple enemy hit in one attack, multiplayer synchronization. This is safe room.
2. Easy bot room : this is where the monster as in brief stays. The monster will be activate once
player enter its radius.
3. Hard bot room : There is another monster in this room. The monster will be activate once
player enter its radius like easy bot but this bot has more attack, very agressive and fast.

<img src="https://github.com/aonzav1/action-rpg-game/assets/47867831/62ded0e9-4817-4367-9b4e-d94aa28008d1" width="50%">

Watch demo on youtube:
https://youtu.be/XDbeiVryDvA

**Tools & packages:**
- Unity 2022.2.16f start with 3D URP Project.
- Text Mesh Pro
- Fish Networking
- Kevin Iglesias Animation
- Mixamo
- Visual Studio 2022

**Player character feature:**
- 3rd person controller, control with mouse and keyboard. I use camera as a controller and create
MoveUnit and CombatUnit components to be slave unit on Entities. So, player can control any Entity (as if authorized).
- Stats stored in Entity component. HP, MP, Stamina. MP and stamina can be regenerate over time.
Player(or any entity) is died when HP falls to 0.
- Player has 3 skills : Spinning around, Range attack, Heavy punch. MP is cunsumed when skills used.
- Dodge and jump which will consume stamina. It is directional movement. Dodging can be input by
direction move dodge and jumping will be consider of which character is facing to. Controlled by
MoveUnit.
- EXP and money. (Still have no usage)

**Monster requirement:**
- Entity with HP, MP, Stamina stats (MP and stamina has no use). Remaining HP is shown by Image bar over head.
- AI. Any entity aside from player can be controlled by AI script. I've implement a simple AI
 script as rule-based. AI scripts act like PlayerController script which will command slave units
(MoveUnit and CombatUnit) to do actions. I have created 2 AIs. (Inherit from baseAI script)
	1.Easy AI
		- Has 3 attack style. Simple hit, Charge attack, Hit and stepback.
		- Generally chase player.
		- Activate when player enter its radius.
		- If there's more than 1 player nearby it will seek for new target if the current target is missing or running around too long.
		- Will regenerate to full HP when HP less than 50% (can be done once).
		- Running around the room when HP below 20% and then fight again.
	2.Hard AI
		- Has 3 attack style. Simple hit, Spin attack, Range attack, Heavy attack.
		- Generally chase player.
		- Activate when player enter its radius.
		- If there's more than 1 player nearby it will seek for new target if the current target is missing or running around too long.
		- Will regenerate to full HP when HP less than 50% (can be done once).
		- Running around the room when HP below 20% and then fight again.
- Give EXP and Money will getting killed by Player.

*Heavy attack and Charge attack will caused the hit enemy pushed away really far, stuns and fall to the ground.*

**Other:**
- Projectile is available for 2 types (use in this prototype only 1)
	1. Can pass through things. (Used)
	2. Destroy on hit.
- If an entity is getting hit while attacking, the attack got interrupted.
- Entities respawn after 5 seconds. (By just reset HP/State and relocate)
- Entities can't get hit when dodging (invincible frame).
- Attacks use method of enabling Hit box collider and detect OnTriggerStay with registering objects that already hit (To be sure that it hits and hits only once). That hit box collider is not attached to part of animated character, as animations are not reliable on server (as if we use distance-based network observation)
- Unity AI Navigation system is not used in this prototype, but can be used with MoveUnit to create point to move movement style and better obstacle avoidance to monster AI. Two things to consider:
	1.MMORPG is expected to be big map if you going to do good pathfinding, better to provide a server for it.
	2.Keep it simple, server don't have to be calculate so much on weak monsters.
- Players are client authorized and Monsters are server authorized.
- Fall guard implemented, when any entity falls the map. It will be repositioned. (As the walls are thin, and no wall hit stop force. Any entities can fall the map)
- No VFX & SFX.
- This demo supports both Host and Client & Server.

