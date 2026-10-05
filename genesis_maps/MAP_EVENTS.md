# Genesis map events (plane 2 -> ECL handlers)

Search events: plane-2 byte & 0x3F indexes (0-based) the ONGOTO table at the start of the module's search entry. Step events: the module's run entry compares the plane-2 byte ([9AF9], full byte) or the code ([9E6F]) and the facing ([9AFA]: 0 N, 1 E, 2 S, 3 W) and jumps to a handler. A few codes are tested with a raw byte (e.g. module 11: [9AF9]==139 is code 0x0B with bit 7 set). Maps 51 and 53 share map 0x51: each module only handles its own subset of the codes, so some codes appear unhandled in one of the two.


## Module 00 / map 10 Chicagorg

No search dispatch (no `AND [9AF9],63` + `ONGOTO`): this map has no cell-event table. 

## Module 03 / map 03 Start / ship interior (module 03)

No search dispatch (no `AND [9AF9],63` + `ONGOTO`): this map has no cell-event table. Scripted cutscene walk (STEPFORWARD / SAVE facing), the map is only the backdrop.

## Module 10 / map 10 Chicagorg

Search: `ONGOTO [9E6F]`, 28 targets, code 0 and codes >= 28 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (4,3)o (5,3)o (6,3)o (8,3)o (3,4)o (9,4)o (3,5)o (11,5)o ... | L6E0D | "THE HEAT FROM THE EXPLOSIONS DRIVES YOU BACK. YOU ARE VERY EXPOSED OUT HERE ON THE TARMAC."; explosion effect; damages party; pushes party back |
| 02 | (7,10)o (8,10)o (7,11) (8,11) | L6E22 | no event here (just the random-encounter check) |
| 03 | (6,13) | L6E23 | "MEN'S ROOM" |
| 04 | (5,13) | L6E28 | "WOMEN'S ROOM" |
| 05 | (6,14) | L6E2D | once only (flag 97F7 bit mask 8); "SOMEONE TOSSED A GRENADE INTO THE ROOM. THE STALLS ARE SHATTERED AND WATER COATS THE FLOOR."; sets flag 97F7/=8 |
| 06 | (5,14) | L6E47 | once only (flag 97F7 bit mask 16); "YOU OPEN THE DOOR INTO THE MUZZLE OF A TERRINE'S PISTOL. HE SMILES AND SQUEEZES THE TRIGGER."; fight: Terrine Warrior; sets flag 97F7/=16, [9DC1]=1 |
| 07 | (2,14) | L6E79 | once only (flag 97F7 bit mask 32); "EXPLOSIONS HAVE SHATTERED THE ROOF AND THE DEBRIS BURIED SEVERAL BODIES."; sets flag 97F7/=32 |
| 08 | (1,14) | L6EA8 | "A NEO OFFICER LIES AMONG SOME WRECKAGE. 'I'M HOLDING ON HERE, BUT MY LEG IS SHATTERED.'" (+2 more texts); sets flag 97F7/=64 |
| 09 | (1,13) | L6EDF | once only (flag 97F7 bit mask 128); "YOU ARE AMBUSHED BY TERRINES!"; fight: 3x Terrine Warrior; sets flag 97F7/=128 |
| 0A | (2,10) | L6F0B | "LECTURE HALL" |
| 0B | (1,10) | L6F10 | once only (flag 97F8 bit mask 1); "AN ORDERLY IS CHECKING ON A YOUNG MAN LYING IN A FETAL POSITION. 'SOME OF THESE KIDS CAN'T TAKE REAL WAR. I'LL"...; sets flag 97F8/=1 |
| 0C | (4,10)o | L6F3A | once only (flag 97F8 bit mask 2); "TWO RECRUITS WERE SHOT IN THE BACK AS THEY TRIED TO ENTER THE BUILDING."; sets flag 97F8/=2 |
| 0D | (11,10)o | L6F62 | once only (flag 97F8 bit mask 4); "AS YOU APPROACH, TERRINES ATTACK FROM THE DOORWAY." (+1 more texts); fight: 4x Terrine Warrior + Terrine Leader; sets flag 97F8/=4 |
| 0E | (13,9) | L6FA2 | "AUTHORIZED PERSONNEL ONLY" |
| 0F | (14,9) | L6FA7 | "YOU BURST IN AMONGST A SQUAD OF TERRINES. IN THE BACKGROUND YOU CAN SEE A RAM TECHNICIAN WORKING ON A MISSILE"... (+19 more texts); fight: 6x Terrine Warrior + Terrine Leader; menu: Shoot / Charge / Take Cover; random roll; menu: Shoot / Charge / Flee; random roll; pushes party back; explosion effe... |
| 10 | (15,10) | L6FD0 | "WITH ONE HAND, THE TECHNICIAN BEGINS FIRING AT YOU. HE IS STILL MODIFYING THE CONTROLS WITH THE OTHER." (+18 more texts); fight; menu: Shoot / Charge / Take Cover; random roll; menu: Shoot / Charge / Flee; random roll; pushes party back; explosion effect; explosion effect; explosion effect; damages... |
| 11 | (13,13) | L71BF | "MEDICAL OFFICER" |
| 12 | (13,14) | L71C4 | "THE ROOM IS FULL OF WOUNDED TROOPS. AN ORDERLY SHOUTS, 'WE'RE FULL IN HERE. YOU'RE MORE USE OUT THERE!' SHE PU"... (+3 more texts); fight; explosion effect; explosion effect; treasure: credits, explosive grenade, explosive grenade; sets flag 97F8/=8, flag 97F9/=2 |
| 13 | (10,13) | L7249 | "WAITING ROOM" |
| 14 | (10,14) | L724E | once only (flag 97F8 bit mask 16); "ONLY THE DEAD ADORN THIS ROOM."; sets flag 97F8/=16 |
| 15 | (7,15) (8,15) | L6E22 | no event here (just the random-encounter check) |
| 16 | (6,7) | L7284 | once only (flag 97F8 bit mask 32); "YOU SEE MONITORS FOR THE FUELING TANKS. THEY ARE ALL RED-LINED -- THE FUEL STORES HAVE BEEN STRUCK IN THE ATTA"...; sets flag 97F8/=32 |
| 17 | (10,5) | L729E | once only (flag 97F8 bit mask 64); "WORKERS TRIED TO USE THIS GARAGE FOR COVER. TERRINES OBVIOUSLY SPOTTED THEM. THE WALLS ARE STAINED CRIMSON."; sets flag 97F8/=64 |
| 18 | (10,7) (11,7) | L72B8 | once only (flag 97F8 bit mask 128); "THIS BUILDING HOUSED FIREFIGHTING EQUIPMENT. THE TERRINES HAVE GUTTED THE STRUCTURE AND DESTROYED THE ROBOTS."; sets flag 97F8/=128 |
| 19 | (2,13) | L72D2 | "TO THE SOUTH IS THE OFFICER'S LOUNGE." (+1 more texts) |
| 1A | (5,5)o (4,6)o (6,6)o (5,7)o | L72DC | once only (flag 97F7 bit mask 1); "AN OFFICER COLLAPSED HERE, WOUNDED BY SHRAPNEL. HE COUGHS WEAKLY AND MOTIONS YOU CLOSE." (+2 more texts); sets flag 97F7/=1 |
| 1B | (12,10) | L6E22 | no event here (just the random-encounter check) |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9AF9]==130 | N | L6BDE | (7,11) (8,11) | "THE SECURITY DOORS ARE SEALED."; sets [9DBF]=255 |
| [9AF9]==2 | S | L6BDE | (7,10)o (8,10)o | "THE SECURITY DOORS ARE SEALED."; sets [9DBF]=255 |
| [9AF9]!=149 | S | L6BEA | (7,15) (8,15) | "THE DOOR HAS BEEN FUSED SHUT BY THE BATTLE."; sets [9DBF]=255 |
| [9AF9]==13 | E | L6C07 | (11,10)o | "THE DOOR HAS BEEN FUSED SHUT BY THE BATTLE."; sets [9DBF]=255 |
| [9E6F]==2 |  | L6D26 | (7,10)o (8,10)o (7,11) (8,11) | "TERRINES MOVE IN FOR THE KILL." (+1 more texts); fight: Terrine Warrior + Terrine Leader + Neo Warrior; random roll; random roll |
| [9E6F]==1 |  | L6D26 | (4,3)o (5,3)o (6,3)o (8,3)o (3,4)o (9,4)o (3,5)o (11,5)o ... | "TERRINES MOVE IN FOR THE KILL." (+1 more texts); fight: Terrine Warrior + Terrine Leader + Neo Warrior; random roll; random roll |
| [9E6F]==2 |  | L7162 | (7,10)o (8,10)o (7,11) (8,11) | "YOU ARE GIVEN A SMALL REWARD." (+1 more texts); fight; +2000 XP; -> module 11; treasure: 1000 cr |

## Module 11 / map 11 

Search: `ONGOTO [9E70]`, 11 targets, code 0 and codes >= 11 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (2,2) | L6E27 | "BEYOND THIS HATCHWAY LIES SALVATION'S HEADQUARTERS." |
| 02 | (1,2) | L6E34 | "'TAKE A TUG OUT AND EXPLORE THE DEBRIS IN THE EARTH'S NEAR ORBIT. IF YOU FIND SOMETHING UNUSUAL, INVESTIGATE.'" (+8 more texts); program 0; pushes party back; sets [9D9E]=127, [9BCB]=96 |
| 03 | (3,1) | L6F02 | "SALVATION'S DEPOT IS BEHIND THIS DOOR. NEO MEMBERS PURCHASE WEAPONS, AND ARMOR HERE." |
| 04 | (3,0) | L6F0F | "'COME AGAIN!'"; store 1; pushes party back; sets [9E63]=16, [9EEC]=1 |
| 05 | (6,1) | L6F25 | "SALVATION'S MEDICAL FACILITIES ARE HERE FOR YOUR USE." |
| 06 | (6,0) | L6F32 | "SALVATION'S MEDICS RESTORE THE ENTIRE TEAM TO PERFECT HEALTH."; NPC joins/appears; pushes party back; sets [9BF6]=1 |
| 07 | (9,1) | L6F70 | "YOU HEAR WILD MUSIC BEHIND THE DOOR." |
| 08 | (9,0) | L6F7D | "YOU ENTER THE SALVATION LOUNGE." (+12 more texts); menu: Mingle / Exit; random roll; random roll |
| 09 | (11,2) | L7110 | "THIS HATCHWAY LEADS TO THE ROCKETBAY." |
| 0A | (12,2) | L7120 | "YOU ARE IN THE SALVATION PORT AREA." (+8 more texts); menu: Launch / Resupply / Exit; menu: Launch / Exit; -> module 22; -> module 20; sets [9E08]=2, [9909]=2, [990E]=2, [9913]=5, [990A]=12, [990B]=5, [990F]=8, [9910]=7 |
| 0B | (6,3) | (falls through) | no search event (code beyond the table) |

Handlers no cell can reach: none

Codes beyond the table (fall through): 0B

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==2 |  | L6F68 | (1,2) | "SALVATION'S MEDICS RESTORE THE ENTIRE TEAM TO PERFECT HEALTH."; pushes party back |

## Module 20 / map 20 Spy Ship

Search: `ONGOTO [9E6F]`, 29 targets, code 0 and codes >= 29 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (14,5) (5,10) | L6B82 | once only (flag 9845 bit mask 128); "SIGNS OF FIERCE COMBAT ARE APPARENT. DARK SPHERES FLOAT PAST AND SMEAR AGAINST THE STAINED WALLS." (+3 more texts); sets flag 9845/=128 |
| 02 | (13,5) (4,10) | L7029 | once only (flag 9841 bit mask 2); "YOUR EYES RIVET ON A FLOATING BODY WRAPPED IN A STAINED COAT AND MISSING AN ARM." (+1 more texts); sets flag 9841/=2 |
| 03 | (13,4) (3,8) | L704F | once only (flag 9845 bit mask 1); "'...WE WILL BE ON SCHEDULE FOR DEPLOYMENT AFTER EARTH'S STERILIZATION.'" (+1 more texts); yes/no prompt; sets flag 9845/=1 |
| 04 | (12,4) (2,10) | L706D | once only (flag 9845 bit mask 2); "'OH, NO! POWER FLUCTUATIONS...DISRUPTION OF CONTAINMENT FIELDS...'" (+1 more texts); yes/no prompt; sets flag 9845/=2 |
| 05 | (12,5) (0,10) | L708B | once only (flag 9845 bit mask 4); "'SCOT! EMERGENCY SHUT DOWN NOW! WHERE ARE YOU?'" (+1 more texts); yes/no prompt; sets flag 9845/=4 |
| 06 | (12,6) (14,11) | L70A9 | once only (flag 9845 bit mask 8); "'SCOT IS NONFUNCTIONAL...SECURITY IS NOTIFIED...PREPARING HOLOGRAM...JUST A PRECAUTION...'" (+1 more texts); yes/no prompt; sets flag 9845/=8 |
| 07 | (13,6) (13,10) | L70C7 | once only (flag 9845 bit mask 16); "'PROJECTOR COMING ON LINE...WAIT! SO QUICKLY!? THE GLEAMING EYES...NO! GET BACK!" (+2 more texts); yes/no prompt; sets flag 9845/=16 |
| 08 | (12,7) (14,10) | L70EA | once only (flag 9845 bit mask 32); "'THIS IS DR. WILLIAMS AND MY SIGMA CODE IS...' A STRING OF NUMBERS FOLLOWS." (+1 more texts); yes/no prompt; sets flag 9845/=32 |
| 09 | (13,7) (13,8) | L7108 | once only (flag 9845 bit mask 64); "THIS ONE IS DATED EARLIER. 'THE ECG CANISTERS ARE LOADED FOR EARTH. THEY WILL MOLT ON PLANET.'" (+1 more texts); yes/no prompt; sets flag 9845/=64 |
| 0A | (15,3) (15,7) (14,8) | L700F | once only (flag 9841 bit mask 8); "THE TURRET CONTROLS ARE DEACTIVATED. YOUR SHIP IS NOT ON THE SCANNERS."; sets flag 9841/=8 |
| 0B | (7,0) (15,0) (5,8) (11,8) (15,8) (3,13) | L7131 | "YOU ARE AT A LADDER." (+3 more texts); menu: Up / Down / Stay; menu: Up / Stay; menu: Down / Stay; -> module 21; staircase; -> module 21 |
| 0C | (0,1) (8,1) (0,9) (6,9) (12,9) | L7213 | "YOU COULD JUST FIT INTO THE AIRSHAFT." (+14 more texts); monsters: 3x Sm. E.C. Gennie; skill check; yes/no prompt; unlocks door; menu: Up / Down / Stay; menu: Up / Stay; menu: Down / Stay; staircase; random roll; NPC joins/appears; duel; NPC joins/appears; +200 XP; NPC joins/appears; unlocks door; ... |
| 0D | (11,1) (3,14) | L74EE | "THE SHIP'S OASIS -- THE HYDROPONICS GARDEN -- SEEMS PEACEFUL." (+16 more texts); NPC joins/appears; menu: Attack / Flee / Watch; menu: Free Self / Watch; damages party; NPC joins/appears; damages party; +250 XP; sets [9E6F]=7, flag 9843/=1 |
| 0E | (8,6) (2,13) | L764B | once only (flag 9843 bit mask 4); "AN ECG STRIKES IMMEDIATELY."; fight: Lg. E.C. Gennie; sets flag 9843/=4 |
| 0F | (9,7) (1,14) | L7690 | once only (flag 9843 bit mask 8); "ANOTHER ECG APPROACHES WITH DRIPPING FANGS."; fight: Lg. E.C. Gennie; sets flag 9843/=8 |
| 10 | (11,7) (0,14) | L76CA | once only (flag 9842 bit mask 8); "THIS ROOM CONTAINS THE AIR SAMPLING EQUIPMENT FOR THE SHIP." (+5 more texts); +500 XP; sets flag 9842/=8 |
| 11 | (7,3) (2,8) | L7724 | "THE ENGINES HAVE BEEN SHUT DOWN. TO RESTART REQUIRES ACCESS TO THE CONTROL ROOM." (+15 more texts); fight: 8x Lg. E.C. Gennie + 4x Sm. E.C. Gennie; menu: Stand / Retreat; +1000 XP; skill check; sets flag 9842/=128, flag 9848/=2, flag 9848/=4, [9E70]=2 |
| 12 | (7,6) (4,8) | L7489 | "THIS ROOM IS FILLED WITH INNUMERABLE PIECES OF EQUIPMENT AND SUPPLIES." (+2 more texts); sets flag 9841/=128 |
| 13 | (6,0) (7,1) | L74C6 | "YOU COULD JUST FIT INTO THE AIRSHAFT." (+15 more texts); monsters: 3x Sm. E.C. Gennie; yes/no prompt; unlocks door; menu: Up / Down / Stay; menu: Up / Stay; menu: Down / Stay; staircase; random roll; NPC joins/appears; duel; NPC joins/appears; +200 XP; NPC joins/appears; unlocks door; -> module 21;... |
| 14 | (2,7) | L795D | no event here (cell is a plain marker) |
| 15 | (10,11) | L78C7 | once only (flag 9844 bit mask 1); "THE ROOM IS SPARTAN. IN THE BACK IS A FOLDER." |
| 16 | (8,11) | L78F2 | once only (flag 9844 bit mask 2); "THE ROOM LOOKS QUICKLY RANSACKED, CLOTHING AND PERSONAL EFFECTS FLOAT EVERYWHERE. YOU SPOT A DIARY TOWARDS THE"... |
| 17 | (6,11) | L791D | once only (flag 9844 bit mask 4); "THE ROOM IS FILLED WITH BODIES. ONLY ONE IS HUMAN. HE LEANS AGAINST THE FAR WALL CLASPING A LOG IN HIS HANDS." |
| 18 | (9,9) | L78AD | once only (flag 9843 bit mask 32); "SHATTERED TABLES FLOAT HERE SMEARED WITH STRANGE ICHOR. THE RAM FORCES MADE A STAND HERE."; sets flag 9843/=32 |
| 19 | (7,8) | L795D | no event here (cell is a plain marker) |
| 1A | (6,12) | L7943 | once only (flag 9844 bit mask 4); "THE LOG CONTAINS THE NAME VILNIKOV, A SIGMA NUMBER AND A PLAN TO CONTROL THE ECGS WITH HARMLESS GAS."; sets flag 9844/=4 |
| 1B | (8,12) | L7903 | once only (flag 9844 bit mask 2); "THE DIARY READS, 'SCOT IS ACTING STRANGELY. HE RHAPSODIZES ABOUT THE EARTH. THE ECGS WILL STERILIZE THE PLANET"...; sets flag 9844/=2 |
| 1C | (10,12) | L78D8 | once only (flag 9844 bit mask 1); "THE FOLDER READS, 'I HAVE DESIGNED THE ECGS TO DROP BRAIN PARASITES AS THEY TRAVEL, CREATING A MINEFIELD.'"; sets flag 9844/=1 |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]!=1 | E | L6C2D | (14,5) (5,10) | sets [9DBF]=255 |

## Module 23 / map 23 Asteroid Base

Search: `ONGOTO [9E6F]`, 11 targets, code 0 and codes >= 11 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (0,6) | L6C11 | "ECGS ATTEMPT TO SEIZE YOUR SHIP."; fight: 2x Lg. E.C. Gennie + 2x Stage 3  Ecg |
| 02 | (2,6) | L6C53 | once only (flag 984E bit mask 4); "THIS AIRLOCK DOOR IS LEAKING. SOMETHING HAS BEEN TEARING AT THE SEALS. A TATTERED BIT OF SPACESUIT FLOATS PAST"...; sets flag 984E/=4 |
| 03 | (4,4) | L6C6D | once only (flag 984E bit mask 8); "THIS IS THE COMMUNICATIONS ROOM. DAMAGED ELECTRONICS FLOAT EVERYWHERE." (+1 more texts); fight: 4x Sm. E.C. Gennie + Lg. E.C. Gennie; sets flag 984E/=8 |
| 04 | (5,3) | L6CA8 | once only (flag 984E bit mask 16); "A BLEEDING MINER IN A SPACESUIT WHISPERS, 'TALON BETRAYED US. THAT WAS NO TREASURE.' HE STOPS BREATHING."; sets flag 984E/=16 |
| 05 | (2,8) | L6CC2 | "SUDDENLY SHAPES WHIRL TOWARD YOU FROM EVERY DIRECTION." (+1 more texts); fight: 4x Sm. E.C. Gennie + Lg. E.C. Gennie; sets flag 984E/=32 |
| 06 | (3,6) | L6CF2 | once only (flag 984E bit mask 64); "SEVERAL MINERS WERE OVERCOME BY THE ECGS WHILE STRAPPED TO THEIR BUNKS." (+2 more texts); sets flag 984E/=64 |
| 07 | (6,6) | L6D16 | once only (flag 984E bit mask 128); "ECGS GUARD THIS DOORWAY. BEYOND YOU SEE CRUDE CORRIDORS WITH MUCH FLOATING DEBRIS."; fight: 3x Sm. E.C. Gennie + Lg. E.C. Gennie + Stage 3  Ecg; sets flag 984E/=128 |
| 08 | (7,8) | L6D53 | once only (flag 984F bit mask 1); "RANDOM OBJECTS HAVE BEEN CRUDELY LASHED TOGETHER HERE. ECGS BOIL OUT TO PROTECT SOMETHING WITHIN." (+1 more texts); fight: 3x Sm. E.C. Gennie + Lg. E.C. Gennie + Stage 3  Ecg; treasure: 2000 cr; sets flag 984F/=1, [9E72]=24 |
| 09 | (9,8) | L6DA4 | "RANDOM OBJECTS HAVE BEEN CRUDELY LASHED TOGETHER HERE. ECGS BOIL OUT TO PROTECT SOMETHING WITHIN." (+1 more texts); fight: 3x Sm. E.C. Gennie + Lg. E.C. Gennie + Stage 3  Ecg; treasure: 2000 cr; sets flag 984F/=2, [9E72]=90 |
| 0A | (9,4) | L6DC2 | "YOU FIND A CACHE OF EXPLOSIVES AND SEVERAL EMPTY CYLINDERS MARKED, 'PROPERTY OF DR. WILLIAMS. BIOHAZARD.'" (+4 more texts); fight; yes/no prompt; treasure: credits, explosive grenade, explosive grenade, explosive grenade, explosive grenade, explosive grenade, explosive grenade, explosive grenade, e... |

Handlers no cell can reach: none

## Module 30 / map 30 Asteroid Base, Level 1

Search: `ONGOTO [9E70]`, 26 targets, code 0 and codes >= 26 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (5,0) | L6E8A | "YOU ARE IN THE AIRLOCK OF A RAM DOCKING BAY." |
| 02 | (5,2) | L6E90 | "A RAM OFFICIAL IS WAITING FOR YOU. 'THANK GOD! WE HAVE A BIOEMERGENCY. SOME CHILDREN TRIGGERED THE RELEASE SYS"... (+6 more texts); fight: 2x Ram L.S. Robot + 2x Ram Warrior; menu: Bluff / Attack; skill check; NPC joins/appears; +500 XP |
| 03 | (2,3) | L6F4C | once only (flag 9861 bit mask 8); "SOLDIERS SALUTE AS YOU APPROACH. 'THE DIRECTOR SAID YOU WERE COMING. THE BRIEFING ROOM IS THROUGH THE NORTH DO"... (+3 more texts); fight: Ram H.S. Robot + Ram Warrior + Terrine Leader + Ram Technician; menu: Leave / Attack; menu: Fight / Leave; pushes party back; ... |
| 04 | (2,2) | L6F8C | "A MAN GREETS YOU. 'WE HAVE CHILDREN TRAPPED ON THE LOWER LEVEL. I'VE LOST TWO SQUADS OF MEN TRYING TO RETRIEVE"... (+1 more texts) |
| 05 | (0,8) | L6FBD | "THE COMMUNICATIONS GEAR IS AUTOMATICALLY TRANSMITTING, 'EB3 NOW IN QUARANTINE. ALL PERSONNEL EVACUATING.'" |
| 06 | (0,11) | L6FD7 | "A FRANTIC WOMAN CRIES, 'HAVE YOU SEEN THE CHILDREN? I HAVEN'T HEARD FROM THEM!' WHAT DO YOU SAY?" (+2 more texts); menu: Yes / No / Remain Silent |
| 07 | (1,13) | L7021 | once only (flag 9861 bit mask 4); "A TENSE WARRIOR SALUTES. 'YOU MUST BE THE SPECIALISTS. AS SOON AS YOU'RE DOWN, WE'LL DEMO THE SHAFT. TAKE THIS"...; fight; treasure: credits, demo charge, demo charge, rope, poison antidote, poison antidote, explosive grenade |
| 08 | (2,15) | L706A | "YOU HAVE ENTERED THE ELEVATOR." (+3 more texts); yes/no prompt; -> module 31 |
| 09 | (4,14) (6,14) | L70B1 | "POISONED WORKER GENNIES SPRAWL AROUND THE ROOM." (+7 more texts); menu: Heal / Leave; skill check; NPC joins/appears; NPC joins/appears; +2500 XP; +100 XP; pushes party back; sets [9E70]=1, flag 9862/=2, [9E78]=1 |
| 0A | (9,15) | L7178 | "YOU HAVE ENTERED THE ELEVATOR." (+3 more texts); yes/no prompt; -> module 31 |
| 0B | (14,11) (14,13) | L717C | once only (flag 9862 bit mask 4); "AN ELDERLY MAN POINTS A SMALL PISTOL AT YOU. HE DROPS IT AS JIM RUSHES FORWARD AND YELLS, 'GRANDPA!'" (+1 more texts); sets flag 9862/=4, [9E78]=1 |
| 0C | (14,9) | L71AA | "THE FLASHING OF A SMALL NOTEBOARD CATCHES YOUR EYE. IT READS, 'DAD, I HAVE CLASS AT THE GENNIE LABS. BACK SOON"... |
| 0D | (8,11) | L71CC | "A HOLOTERMINAL'S SCREEN FLASHES RED. 'CODE RED EVACUATION. BIOHAZARD ON LOWER LEVEL. RELOCATE TO GRADIVUS MONS"... |
| 0E | (8,3) | L71E6 | once only (flag 9862 bit mask 32); "THERE ARE GUARDS BLOCKING THE CORRIDOR." (+2 more texts); fight: Ram H.S. Robot + Ram Warrior + Terrine Leader + Ram Technician; menu: Leave / Attack; menu: Fight / Leave; pushes party back; random roll; random roll; random roll; random roll; sets flag 9861/=8, fl... |
| 0F | (3,6) | L727D | "THIS IS A WORKING MEDICAL OFFICE." (+2 more texts); yes/no prompt; NPC joins/appears; sets [9BF6]=1 |
| 10 | (3,10) | L728D | "SOME EQUIPMENT WAS LEFT HERE..."; fight; treasure: credits, stun grenade, stun grenade, dazzle grenade, ecm package, mini explosive |
| 11 | (9,2) | L72B8 | "GUARDS RUSH TO ENGAGE YOU AS OTHERS FLEE THIS CONTROL ROOM." (+1 more texts); fight: 4x Ram Warrior |
| 12 | (12,7) | L72F4 | "GUARDS RUSH TO DEFEND THIS ROOM." (+1 more texts); fight: 4x Ram Warrior |
| 13 | (13,4) (14,4) (13,5) (14,5) | L7330 | "YOU HAVE ENTERED A POWER ROOM. VARIOUS DISPLAYS LINE THE WALLS." (+2 more texts); skill check; NPC joins/appears; menu: Shut Down / Ignore; sets [9E6F]=1, [9E70]=2, [987A]=1 |
| 14 | (12,5) | L73A5 | "THIS IS THE PERSONNEL DIRECTOR'S OFFICE. SEARCHING THE FILES, REVEALS THAT THE LASER RESEARCH TEAM WAS REASSIG"... |
| 15 | (13,3) | L73BF | "JIM YELLS, 'THE EVACUATION SHIP SHOULD BE TO THE NORTH. WE'RE ALMOST OUT. YAY!'"; sets [9E78]=1 |
| 16 | (13,0) | L73DF | "YOU HEAR THROUGH THE SWIFTLY CLOSING AIRLOCK, 'HOSTILES IN THE BAY! LAUNCH!' A RUMBLE OF ROCKETS SIGNALS THE S"... (+1 more texts); sets [987B]=1, flag 9863/=16, [9E78]=1 |
| 17 | (15,10) | L7416 | "A CART HAS SPILLED, LEAVING USEFUL ITEMS."; fight; treasure: credits, aerosol mist grenade, aerosol mist grenade, poison antidote, poison antidote, rope |
| 18 | (8,8) | L7441 | "THERE ARE GUARDS BLOCKING THE CORRIDOR." (+2 more texts); fight: Ram H.S. Robot + Ram Warrior + Terrine Leader + Ram Technician; menu: Leave / Attack; menu: Fight / Leave; pushes party back; random roll; random roll; random roll; random roll; sets flag 9861/=8, flag 9861/=4, flag 9862/=32, [97FC]=2... |
| 19 | (7,14) | L7460 | "THERE ARE GUARDS BLOCKING THE CORRIDOR." (+2 more texts); fight: Ram H.S. Robot + Ram Warrior + Terrine Leader + Ram Technician; menu: Leave / Attack; menu: Fight / Leave; pushes party back; random roll; random roll; random roll; random roll; sets flag 9861/=8, flag 9861/=4, flag 9862/=32, flag 986... |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9AF9]!=129 | N | L6BE4 | (5,0) | "AN ESCAPE SHIP IS HERE. A PILOT STATES, 'THIS IS A RELIEF SHIP FOR THE CHILDREN.'" (+7 more texts); +1500 XP; sets [9DBF]=255, flag 9861/=1 |

## Module 31 / map 31 Asteroid Base, Level 2

Search: `ONGOTO [9E70]`, 31 targets, code 0 and codes >= 31 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (2,15) | L6D9C | "THE ELEVATOR IS SHATTERED AND THE SHAFT BLOCKED." (+2 more texts); explosion effect; sets flag 9865/=1, [987B]=1 |
| 02 | (5,14) | L6DAD | once only (flag 9864 bit mask 1); "A RAM WARRIOR STAGGERS TOWARD YOU AND GASPS, 'UNABLE TO RETRIEVE CHILDREN, SIR.' HE STOPS BREATHING."; sets flag 9864/=1, [9E78]=1 |
| 03 | (5,9) | L6DDC | once only (flag 9864 bit mask 1); "A RAM WARRIOR STAGGERS TOWARD YOU AND GASPS, 'UNABLE TO RETRIEVE CHILDREN, SIR.' HE STOPS BREATHING."; sets flag 9864/=1, [9E78]=1 |
| 04 | (6,14) | L6DE0 | "YOU FIND A SCHEMATIC FOR AN INCREDIBLY POWERFUL LASER. THERE IS MENTION OF A TINY PROTOTYPE IN THIS BASE." (+1 more texts); +250 XP |
| 05 | (5,15) | L6E04 | "AN UNUSUAL LASER DOMINATES THE ROOM." (+1 more texts) |
| 06 | (7,9) (6,12) | L6E29 | once only (flag 9864 bit mask 8); "KIDS LOOK YOU OVER. ONE COMES UP AND STATES, 'I'M MILO PHILLIPS, JR. THANK YOU FOR COMING TO OUR AID.'" (+2 more texts); sets flag 9864/=8 |
| 07 | (4,6) | L6E4D | "REFLECTORS FILL THE ROOM." (+1 more texts) |
| 08 | (14,14) | L6E76 | once only (flag 9864 bit mask 32); "MILO STEPS UP TO A LARGE MIRROR. 'AH HEM, SINCE MY FATHER IS NOT HERE I FEEL THAT I SHOULD GIVE A SPEECH." (+2 more texts); sets flag 9864/=32, [9E78]=1 |
| 09 | (12,15) | L6EA0 | once only (flag 9864 bit mask 64); "SPECIAL GLASS MOLDS LIE HERE. A NOTE READS, 'MOLDS REJECTED BY VENUS TEAM. INSUFFICIENT FAULT TOLERANCE.'"; sets flag 9864/=64 |
| 0A | (9,15) | L6EBA | "THE ELEVATOR IS DAMAGED. YOU NEED A ROPE." (+4 more texts); item search; yes/no prompt; -> module 30; explosion effect; sets flag 9864/=128, flag 9865/=1, [987B]=1, [9E78]=1 |
| 0B | (12,12) | L6F38 | once only (flag 9865 bit mask 2); "GENNIES ARE EXPLORING THIS LAB, POKING CLAWS INTO EVERYTHING." (+5 more texts); fight: 4x Hyperscorp + 2x Hypersnake; menu: Attack / Withdraw; +500 XP; pushes party back; sets flag 9865/=2, flag 9864/=16, [9E78]=1 |
| 0C | (14,11) | L6FCD | "THE LAB CONTAINS A PARTIALLY CONSTRUCTED SATELLITE, DESIGNED TO REFLECT A LARGE POWERFUL LASER BEAM." (+1 more texts); sets [9E78]=1 |
| 0D | (3,1) (5,1) (8,1) | L6FF2 | "A WORKING COMPUTER LAB. TERMINALS LIE ALONG THE BACK WALL." |
| 0E | (12,3) | L6FFF | "KILLER GENNIES APPROACH." (+5 more texts); fight: Hypercrab + Hyperscorp + Hypersnake; random roll; random roll; random roll; random roll; random roll; menu: Attack / Hide; skill check; random roll; pushes party back; sets [9E70]=1, [9E71]=2, [9DC1]=1, flag 9865/=8, [9E78]=1 |
| 0F | (14,4) | L701C | "A NOTEBOOK SHOWS THE FOLLOWING INFORMATION. 'HYPER-CRAB: SUPERIOR ARMOR; HYPER-SNAKE: SUPERIOR SPEED; HYPER-SC"... |
| 10 | (9,7) | L7036 | "A SAPPER TEAM WAS OVERWHELMED HERE BY HYPER-GENNIES."; fight; treasure: credits, demo charge, demo charge |
| 11 | (12,6) | L705B | "THE LAB SEEMS DISUSED. A SHIPPING INVOICE TO 'GRADIVUS MONS, MARS' LIES IN THE DUST." |
| 12 | (9,4) | L7075 | "THERE ARE BLUEPRINTS OF THE GENNIE CONTAINMENT CANISTER, SHOWING IT EQUIPPED WITH A TRANSCEIVER." (+1 more texts); sets [9E78]=1 |
| 13 | (12,10) | L709A | once only (flag 9866 bit mask 1); "THE LAB HAS A NUMBER OF RABBIT GENNIES IN CAGES HERE. JIM RUSHES FORWARD STATING, 'IF WE'RE ESCAPING, THEN SO"... (+2 more texts); fight: 6x Hypercrab; +750 XP; sets flag 9866/=1, [9E78]=1 |
| 14 | (9,10) | L70DC | "THIS OFFICE IS DISARRANGED. A NOTE READS, 'TALON IS A WEAK LINK. CAN HE BE TRUSTED WITH THE DL-SATS? MONEY IS"... |
| 15 | (9,11) | L70F6 | "THE REMAINS OF 50 LARGE CRATES MARKED FROM DL-SAT1 TO DL-SAT50 LIE AMONGST SHATTERED MIRRORS." |
| 16 | (4,10) | L7110 | "A NOTEPAD CONTAINS INFORMATION ON A NEW POWERFUL LASER AND THE LETTERS 'DNA'." |
| 17 | (11,1) | L712A | "BOXES WITH THE SHIPPING LABEL OF 'GRADIVUS MONS' LINE THE FLOOR." (+1 more texts); sets [9E78]=1 |
| 18 | (1,4) | L714F | "YOU FIND NOTES INDICATING THAT FOUR DIFFERENT GENNIE PROJECTS ARE UNDER WAY." |
| 19 | (13,1) | L7169 | "JIM WHISPERS, 'I BET YOU'RE NEO SPIES HERE TO STEAL THE COMPUTER INFO. THAT'S OKAY, I DON'T LIKE MILO OR HIS D"...; sets flag 9866/=64, [9E78]=1 |
| 1A | (1,1) | L7191 | once only (flag 9866 bit mask 128); "THIS ROOM IS CLEAN AND NOT RECENTLY USED. A NAME PLATE READS, 'DR. WILLIAMS.'"; sets flag 9866/=128 |
| 1B | (1,10) | L71AB | once only (flag 9867 bit mask 1); "A DAMAGED VIDEO SCREEN IN THIS LAB ENDLESSLY REPLAYS SCENES OF ECGS DEFEATING HYPER CREATURES."; sets flag 9867/=1 |
| 1C | (5,12) | L71C5 | once only (flag 9867 bit mask 2); "THE DOOR HERE, MARKED 'GENNIE DEVELOPMENT LAB 3', HAS BEEN BARRICADED. IT COULD BE OPENED BY A DEMO CHARGE. DO"... (+2 more texts); yes/no prompt; item search; explosion effect; explosion effect; destroys item; +750 XP; sets flag 9867/=2, [9E78]=1 |
| 1D | (3,0) (4,0) (5,0) (6,0) (7,0) (8,0) (9,0) | L7213 | "THE COMPUTER YIELDS NO NEW INFORMATION." (+12 more texts); menu: Insert Card / Reprogram; menu: Alpha / Ragon / Eliph / Brg; menu: Alpha / Dna / Ragon / Eliph / Brg; +750 XP; sets [9E70]=1, [9E71]=3, [9808]=1, [9876]=1, [9E78]=1 |
| 1E | (8,9) | L71C5 | once only (flag 9867 bit mask 2); "THE DOOR HERE, MARKED 'GENNIE DEVELOPMENT LAB 3', HAS BEEN BARRICADED. IT COULD BE OPENED BY A DEMO CHARGE. DO"... (+2 more texts); yes/no prompt; item search; explosion effect; explosion effect; destroys item; +750 XP; sets flag 9867/=2, [9E78]=1 |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]!=28 | E | L6B70 | (5,12) | once only (flag 9867 bit mask 2); sets [9DBF]=255 |
| [9E6F]==1 |  | L731F | (2,15) | "'GREETINGS MILO, YOU HAVE A MESSAGE.'" (+3 more texts); +750 XP; sets [9876]=1, [9E78]=1 |

## Module 32 / map 32 Pirate Ship, Levels 1-5

Search: `ONGOTO [9E6F]`, 17 targets, code 0 and codes >= 17 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,5) (2,6) (3,6) (4,6) | L7173 | "THIS IS A SMALL HOLDING CELL." (+11 more texts); menu: Bypass / Reprogram / Wait; skill check; skill check; NPC joins/appears; sets [9E71]=3, [9BCB]=32, [9870]=10 |
| 02 | (5,5) | L728E | "THIS IS A SMALL HOLDING CELL." (+12 more texts); fight; menu: Bypass / Reprogram / Wait; skill check; skill check; NPC joins/appears; NPC joins/appears; treasure: credits, demo charge, demo charge, ecm package, stun grenade, stun grenade; sets [9E71]=3, [9BCB]=32, [9870]=10, [97F8]=1, [9E72]=1 |
| 03 | (3,3) (10,3) (3,10) (10,10) | L72C5 | "YOU'RE ON A STAIRCASE. A SIGN READS: LEVEL" (+1 more texts); menu: Up / Down / Stay; menu: Up / Stay; menu: Down / Stay; moves party to (3,3) |
| 04 | (3,1) | L743D | "THIS IS A SMALL HOLDING CELL." (+20 more texts); fight: 5x Pirate Warrior + 5x Pirate Leader; menu: Bypass / Reprogram / Wait; skill check; skill check; NPC joins/appears; menu: Reprogram Doc / Heal Team / Leave; skill check; yes/no prompt; pushes party back; NPC joins/appears; sets [9E71]=3, [9BCB... |
| 05 | (1,1) | L7516 | "YOU'VE ENTERED A MEDICAL SUPPLY ROOM."; fight; treasure: credits, poison antidote, poison antidote; sets [97FC]=1 |
| 06 | (0,2) (6,2) (0,3) (6,3) (0,4) (6,4) | L7535 | "YOU'VE ENTERED A CABIN." (+1 more texts); fight: 4x Pirate Warrior + 3x Pirate Leader; random roll |
| 07 | (10,5) | L7577 | "YOU HAVE ENTERED THE DINING AREA. THE PIRATES WOULD MUCH RATHER FIGHT THAN EAT."; fight: 5x Pirate Warrior + 5x Pirate Leader; sets [97FB]=1 |
| 08 | (12,4) | L75A4 | "GARBAGE AND ROTTING FOOD ARE STREWN THROUGHOUT THE GALLEY." (+6 more texts); yes/no prompt; item search; destroys item; explosion effect; explosion effect; explosion effect; explosion effect; +5000 XP; sets [986F]=1 |
| 09 | (10,1) | L7612 | "TALON'S ROOM IS DECORATED WITH GROTESQUE WAR TROPHIES. A COMPUTER DOMINATES THE ROOM." (+7 more texts); yes/no prompt; NPC joins/appears; skill check; +1000 XP; yes/no prompt; NPC joins/appears; sets [9804]=1, [9E71]=2, [97FA]=1, [9E72]=1 |
| 0A | (8,3) | L76C4 | "YOU ENTER THE ARMORY." (+2 more texts); fight: 4x Pirate Warrior + 2x Pirate Leader + P. Combat Robot; NPC joins/appears; treasure: credits, cutlass, cutlass, cutlass, cutlass, cutlass, cutlass, cutlass, cutlass, cutlass, cutlass, needle gun, needle gun, needle gun, needle gun, needle gun, rocket r... |
| 0B | (6,9) (6,10) | L7792 | "YOU SMASH PANELS AND CONTROLS!" (+4 more texts); NPC joins/appears; sets [97FD]=1, [9809]=1, [9E72]=1 |
| 0C | (2,7) (3,7) (4,7) | L77CD | "THESE PANELS ARE SMASHED." (+4 more texts); NPC joins/appears; sets [97FE]=1, [9809]=1, [9E72]=1 |
| 0D | (1,8) (1,9) | L7803 | "THESE PANELS ARE SMASHED." (+4 more texts); NPC joins/appears; sets [97FF]=1, [9809]=1, [9E72]=1 |
| 0E | (9,7) (10,7) (11,7) (7,9) (13,9) (7,10) (13,10) (7,11) ... | L785B | "THE BRIDGE CONTROLS WERE DAMAGED IN THE BATTLE."; sets [9801]=1 |
| 0F | (3,11) | L7172 | no event here (cell is a plain marker) |
| 10 | (10,11) | L7172 | no event here (cell is a plain marker) |
| 11 | (13,2) | (falls through) | step event: facing E: "THE AIRLOCK DOORS SLIDE OPEN AND YOU ENTER YOUR SHIP." (+8 more texts); NPC joins/appears; -> module 11; +10000 XP; NPC joins/appears; NPC joins/appears; sets [9BBF]=1, [9E08]=1, [9DBF]=255, [9877]=1, [9E72]=1, [9BF6]=1 |
| 12 | (10,2) | (falls through) | step event: facing N: "THE ENTRANCE TO TALON'S QUARTERS APPEARS TO BE TRAPPED. TRY TO DISABLE THE SECURITY?" (+6 more texts); fight: 5x Pirate Warrior + Pirate Leader + P. Combat Robot; NPC joins/appears; yes/no prompt; NPC joins/appears; skill check; menu: You Try / Buck Tries; +1000 XP; sets [9E72... |
| 13 | (9,3) | (falls through) | step event: facing W: "YOU HEAR VOICES WITHIN THE ROOM. ENTER ANYWAY?" (+1 more texts); NPC joins/appears; yes/no prompt; yes/no prompt; sets [9E72]=1, [9DBF]=255 |

Handlers no cell can reach: none

Codes beyond the table (fall through): 11 12 13

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==17 | E | L6EA1 | (13,2) | "THE AIRLOCK DOORS SLIDE OPEN AND YOU ENTER YOUR SHIP." (+8 more texts); NPC joins/appears; -> module 11; +10000 XP; NPC joins/appears; NPC joins/appears; sets [9BBF]=1, [9E08]=1, [9DBF]=255, [9877]=1, [9E72]=1, [9BF6]=1 |
| [9E6F]==18 | N | L78CF | (10,2) | "THE ENTRANCE TO TALON'S QUARTERS APPEARS TO BE TRAPPED. TRY TO DISABLE THE SECURITY?" (+6 more texts); fight: 5x Pirate Warrior + Pirate Leader + P. Combat Robot; NPC joins/appears; yes/no prompt; NPC joins/appears; skill check; menu: You Try / Buck Tries; +1000 XP; sets [9E72]=1, [9DBF]=255, [9804... |
| [9E6F]==19 | W | L79D7 | (9,3) | "YOU HEAR VOICES WITHIN THE ROOM. ENTER ANYWAY?" (+1 more texts); NPC joins/appears; yes/no prompt; yes/no prompt; sets [9E72]=1, [9DBF]=255 |
| [9E6F]==3 |  | L6EEF | (3,3) (10,3) (3,10) (10,10) | "'THE BRIDGE IS TOO HEAVILY GUARDED! STORMING IT WOULD BE SUICIDE!'" (+6 more texts); fight: Talon + 5x Pirate Warrior + 25x Pirate Leader + 25x P. Combat Robot / or / Talon + 2x Pirate Warrior + Pirate Leader + 2x P. Combat Robot; menu: Charge Forward / Stay Back; pushes party back; +5000 XP; NPC j... |
| [9E6F]==3 |  | L6FFC | (3,3) (10,3) (3,10) (10,10) | "YOU HIDE IN THE STAIRWAY." (+6 more texts); fight: 10x Pirate Warrior + 8x Pirate Leader + 8x P. Combat Robot / or / 2x Pirate Warrior + 2x Pirate Leader + 2x P. Combat Robot; menu: Charge Forward / Stay Back; menu: Charge Forward / Stay Back; pushes party back; pushes party back; NPC joins/appears... |

## Module 34 / map 34 Pirate Ship, Levels 11-15

Search: `ONGOTO [9E6F]`, 10 targets, code 0 and codes >= 10 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,0) (4,2) (0,5) (6,5) (1,8) (8,8) (1,11) (6,14) ... | L6DF6 | "THE REMAINING FORCES ARE CUTTING THROUGH THE AIRLOCK." (+2 more texts); fight: monster 40562 + monster 40562 + monster 40563 + monster 40563; -> module 22; treasure: credits; sets [9858]=1, [9E08]=2 |
| 02 | (3,0) (7,1) (12,2) (3,4) (7,6) (3,8) (12,8) (7,11) ... | L6E44 | "YOU ARE AT A LADDER. WHERE DO YOU GO?" (+4 more texts); fight; menu: Up / Down / Stay; menu: Up / Stay; menu: Down / Stay; staircase; staircase; random roll; explosion effect; -> module 22; treasure: credits; sets [9858]=1, [9E08]=2 |
| 03 | (15,0) (15,4) (4,15) (8,15) | L6DF5 | no event here (cell is a plain marker) |
| 04 | (2,3) (1,15) | L6FAB | "THESE CONTROLS ARE DAMAGED." (+9 more texts); fight: monster 40562 + monster 40562 + monster 40563 + monster 40563; random roll; yes/no prompt; skill check; random roll; random roll; -> module 22; treasure: credits; sets [9E6F]=1, [9E70]=3, flag 9802/=16, flag 9802/=32, [9858]=1, [9E08]=2 |
| 05 | (11,0) (12,0) (5,10) (12,11) (13,11) | L7011 | "THESE CONTROLS ARE DAMAGED." (+9 more texts); fight: monster 40562 + monster 40562 + monster 40563 + monster 40563; random roll; yes/no prompt; skill check; random roll; random roll; -> module 22; treasure: credits; sets [9E6F]=1, [9E70]=3, flag 9802/=16, flag 9802/=32, [9858]=1, [9E08]=2 |
| 06 | (11,14) | L70FD | once only (flag 9802 bit mask 64); "A GUARD SHOUTS TO ANOTHER, 'KILL THE PRISONERS! WE'LL DELAY THEM.'" (+1 more texts); fight: monster 40562 + monster 40562 + monster 40563 + monster 40563; sets flag 9802/=64 |
| 07 | (10,13) | L7145 | "YOU ARE JUST IN TIME TO PREVENT A SLAUGHTER." (+2 more texts); fight: monster 40562 + monster 40562 + monster 40563 + monster 40563; random roll; -> module 22; treasure: credits; sets [9858]=1, [9E08]=2 |
| 08 | (6,6) | L718D | "A SIGN ABOVE THE DOOR READS," (+1 more texts) |
| 09 | (5,6) | L71A0 | "THE CAPTAIN'S GUARDS TRY TO STOP YOU." (+2 more texts); fight: monster 40562 + monster 40562 + monster 40563 + monster 40563; -> module 22; treasure: credits; sets [9858]=1, [9E08]=2 |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]!=1 |  | L6CF2 | (1,0) (4,2) (0,5) (6,5) (1,8) (8,8) (1,11) (6,14) ... | no event here (cell is a plain marker) |

## Module 41 / map 41 Desert Runner Village

Search: `ONGOTO [9E6F]`, 12 targets, code 0 and codes >= 12 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,7)o (0,8)o | L6D5B | once only (flag 97FA bit mask 1); "DESERT RUNNERS SURROUND YOU AS YOU ENTER. A PROUD WOMAN STEPS FORWARD. SHE SNARLS, 'WE ARE AT WAR AND YOU ARE"... (+9 more texts); fight: 12x D.R. Warrior / or / 5x Ram Warrior + 5x Ram Mar Cgennie + 6x D.R. Warrior / or / Ram Assault Bot; menu: Step Forward / Atta... |
| 02 | (8,7)o (8,8)o | L6E47 | "THE LARGEST OF THE DESERT RUNNERS STANDS BEFORE YOU. 'I AM TUSKON. I UNDERSTAND THAT ENEMY GLIDERS ARE APPROAC"... (+5 more texts); fight: 8x Ram Warrior + 3x Ram Mar Cgennie + 4x D.R. Warrior; pushes party back; sets [9DC1]=2, [97FC]=1 |
| 03 | (5,7)o (3,8)o | L6ECA | "A FEW GUARDS WAIT HERE NERVOUSLY. THEY EYE YOU SUSPICIOUSLY." |
| 04 | (0,7)o | L6B95 | no event here (cell is a plain marker) |
| 05 | (9,7) (9,8) | L6ED7 | "AN ELDERLY WHITE-FURRED DESERT RUNNER COMES UP. 'REST AND I WILL TEND YOUR WOUNDS.'" (+15 more texts); explosion effect; explosion effect; skill check; +1000 XP; skill check; +1000 XP; -> module 40; NPC joins/appears; sets [9E6F]=2, [97FC]=3, flag 97FA/=32, [9E70]=1, [987E]=1, flag 97FA/=2, [9BF6]=... |
| 06 | (9,5)o (10,5)o (12,7)o (12,8)o (9,10)o (10,10)o | L7094 | once only (flag 97FB bit mask 1); "A CROUCHING DESERT RUNNER SNARLS, 'GLIDERS HAVE LANDED AMONGST THE BUILDINGS. SEEK OUT THE ENEMY!'" (+1 more texts); sets flag 97FB/=1, flag 97FB/=2 |
| 07 | (10,1) (12,3) (7,4) | L70F1 | once only (flag 97FB bit mask 4); "RAM TROOPERS ARE ATTACKING WOMEN AND CHILDREN. THEY TURN AND ATTACK." (+2 more texts); fight: 6x Ram Warrior + 4x Ram G.D. Gennie / or / Ram Assault Bot; sets flag 97FB/=4, flag 97FB/=8 |
| 08 | (1,3) (2,5) (4,5) | L7167 | once only (flag 97FA bit mask 16); "YOU REALIZE THAT YOU HAVE ACTIVATED A BOOBY TRAP!" (+4 more texts); menu: Disarm / Flee; skill check; +1000 XP; random roll; explosion effect; damages party; pushes party back; explosion effect; sets flag 97FA/=16, [9E70]=1, [9E71]=2, [9E6F]=1, [9E6F]=3 |
| 09 | (15,3) (14,7) (14,10) (14,11) | L7217 | once only (flag 97FA bit mask 4); "THE BODY HOLDS A HOMING RADIO WHICH YOU PICK UP."; sets flag 97FA/=4, [9800]=1 |
| 0A | (6,13) (11,13) (9,14) (14,15) | L7246 | once only (flag 97FA bit mask 8); "INSIDE YOU FIND A PALLET FULL OF POWERFUL EXPLOSIVES. YOU HAUL THEM ALONG."; sets flag 97FA/=8, [9801]=1 |
| 0B | (1,10) (5,10) (1,14) | L726E | no event here (cell is a plain marker) |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==2 |  | L72A5 | (8,7)o (8,8)o | logic only (ENDFOR, RETURN) |

## Module 42 / map 42 Mars Base Gradiuvs Mons

Search: `ONGOTO [9E6F]`, 23 targets, code 0 and codes >= 23 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (2,12) | L6E77 | "CAGES FULL OF DESERT APES LINE THE WALLS." (+2 more texts); NPC joins/appears; sets [9E72]=1 |
| 02 | (3,11) | L6E9F | "ALL THE APES HAVE BEEN FREED! THEY SWARM OUT OF THE ROOM, AND IN SECONDS ALARMS BEGIN TO WAIL." (+12 more texts); fight: Desert Ape; yes/no prompt; NPC joins/appears; sets [9DBF]=255, flag 9899/=16, flag 9899/=1, flag 9899/=2, flag 9899/=4, flag 9899/=8, [9E6F]=12, [9E6F]=9, [9E6F]=6, [9E6F]=3, [9E... |
| 03 | (1,9) | L6FFD | "DESERT APES OVERWHELMED A LAB TECHNICIAN HERE. THE APES GLARE AT YOU MENACINGLY." (+4 more texts); fight: 5x Desert Ape; menu: Stay / Retreat; pushes party back; NPC joins/appears; treasure: credits, stun grenade, stun grenade, stun grenade, stun grenade, stun grenade; sets [9897]=1, [9E72]=1 |
| 04 | (3,9) | L709D | "THIS WAS A GUARD POST, BUT THE GUARDS APPEAR TO HAVE FLED FROM THE APES." (+2 more texts); fight: 7x Ram Mar Cgennie; sets [9896]=1 |
| 05 | (7,12) | L70DD | "THIS WAS A GUARD POST, BUT THE GUARDS APPEAR TO HAVE FLED FROM THE APES." (+1 more texts); fight: 6x Ram Warrior + 2x Ram Combat Bot; sets [9896]=2 |
| 06 | (7,1) (8,13) (2,14) | L7115 | "YOU FIND AN INTERCOM STATION. USE THE INTERCOM?" (+12 more texts); fight: 2x Ram Combat Bot + 3x Ram Mar Cgennie; yes/no prompt; menu: False Alarm / Jeer; yes/no prompt; +1500 XP; NPC joins/appears; sets [97FA]=1, [97FB]=1, [97F9]=1, [9E72]=1 |
| 07 | (0,0) (7,14) | L71DC | "A METAL LADDER LEADS DOWNWARD. CLIMB DOWN?" (+1 more texts); yes/no prompt; staircase; yes/no prompt; moves party to (0,0) facing S; sets [97FD]=1, [97FD]=2 |
| 08 | (5,3) (7,3) (5,5) (7,5) (10,13) (11,13) (12,13) (10,15) ... | L725C | "A SMALL OFFICE." (+3 more texts); fight: 7x Ram G.D. Gennie |
| 09 | (4,6) | L72AB | "ATHA IS HERE WITH SOME DESERT RUNNERS. 'THE LASER IS ABOVE US,' SHE SAYS." (+2 more texts); sets [9801]=1 |
| 0A | (6,7) (15,15) | L72EC | "A COMPLEX SECURITY SYSTEM PREVENTS YOU FROM USING THIS ELEVATOR." (+2 more texts); yes/no prompt; yes/no prompt; moves party to (6,7) facing W; sets [97FD]=3, [97FD]=2 |
| 0B | (10,4) | L737A | "BEFORE YOU ARE THE MASSIVE FRONT DOORS TO THE BASE. OUTSIDE WAIT THE DESERT RUNNERS. GUARDS ATTACK!" (+1 more texts); fight: 5x Ram Warrior + 5x Ram Mar Cgennie; sets [9893]=1 |
| 0C | (10,3) | L73AC | "THIS CONTROL PANEL OPENS THE FRONT DOORS...BUT IT MIGHT ALSO BE CONNECTED TO AN ALARM. OPEN THE DOORS?" (+6 more texts); fight: 10x D.R. Warrior + 6x Ram G.D. Gennie + Ram Combat Bot; yes/no prompt; NPC joins/appears; sets [9894]=1, [9E72]=1 |
| 0D | (12,11) | L7415 | "THE MINIATURE DOOMSDAY LASER IS MOUNTED IN THIS ROOM. ITS CONTROLS LIE JUST TO THE NORTH."; sets [97FE]=1 |
| 0E | (12,10) | L7434 | "THESE ARE THE CONTROLS FOR THE MINIATURE LASER." (+4 more texts); yes/no prompt; sets [9895]=1 |
| 0F | (8,6) | L7462 | "THIS IS A MEDICAL STATION. ITS CONTROLS ARE UNFAMILIAR TO YOU. TRY TO HEAL THE TEAM ANYWAY?" (+3 more texts); yes/no prompt; skill check; damages party; NPC joins/appears; yes/no prompt; sets [9E71]=2, [97FF]=1, [9BF6]=1 |
| 10 | (4,11) | L6E75 | no event here (cell is a plain marker) |
| 11 | (3,12) | L6E75 | no event here (cell is a plain marker) |
| 12 | (1,12) | L6E75 | no event here (cell is a plain marker) |
| 13 | (1,11) | L6E75 | no event here (cell is a plain marker) |
| 14 | (11,3) (11,4) (11,5) | L6E75 | no event here (cell is a plain marker) |
| 15 | (0,15) | L6E75 | no event here (cell is a plain marker) |
| 16 | (15,10) | L750D | "YOU FIND A PILE OF EQUIPMENT."; fight; treasure: credits, ecm package, ecm package, ecm package, ecm package, battle armor; sets [9802]=1 |
| 17 | (13,14) | (falls through) | step event: facing W: "THE DOOR IS LOCKED." (+4 more texts); menu: Pick Lock / Demo Charge / Leave; skill check; item search; destroys item; explosion effect; sets [9DBF]=255, [9E71]=2, [9803]=1 |

Handlers no cell can reach: none

Codes beyond the table (fall through): 17

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==16 | E | L6C23 | (4,11) | "CAGE 1." (+6 more texts); fight: 3x Desert Ape; yes/no prompt; NPC joins/appears; sets [9DBF]=255, flag 9899/=1, [9E72]=1 |
| [9E6F]==17 | E | L6C4F | (3,12) | "CAGE 2." (+6 more texts); fight: 3x Desert Ape; yes/no prompt; NPC joins/appears; sets [9DBF]=255, flag 9899/=2, [9E72]=1 |
| [9E6F]==18 | W | L6C7B | (1,12) | "CAGE 3." (+6 more texts); fight: 3x Desert Ape; yes/no prompt; NPC joins/appears; sets [9DBF]=255, flag 9899/=4, [9E72]=1 |
| [9E6F]==19 | W | L6CA7 | (1,11) | "CAGE 4." (+6 more texts); fight: 3x Desert Ape; yes/no prompt; NPC joins/appears; sets [9DBF]=255, flag 9899/=8, [9E72]=1 |
| [9E6F]==20 | E | L6D3A | (11,3) (11,4) (11,5) | "THE MASSIVE FRONT DOORS ARE SEALED." (+3 more texts); -> module 40; sets [9DBF]=255, [9855]=1 |
| [9E6F]==21 | S | L6C17 | (0,15) | "THIS DOOR WAS FUSED BY A LASER BOLT. YOU CAN'T LEAVE THIS WAY."; sets [9DBF]=255 |
| [9E6F]==23 | W | L6D8B | (13,14) | "THE DOOR IS LOCKED." (+4 more texts); menu: Pick Lock / Demo Charge / Leave; skill check; item search; destroys item; explosion effect; sets [9DBF]=255, [9E71]=2, [9803]=1 |
| [9E6F]==2 |  | L74FF | (3,11) | logic only (ENDFOR, CONTINUE, ENCEXIT) |

## Module 43 / map 43 More Asteroid Bases

Search: `ONGOTO [9E6F]`, 51 targets, code 0 and codes >= 51 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (4,15) | L6D9C | once only (flag 97FC bit mask 1); "RAM HAS A RECEPTION COMMITTEE WAITING FOR YOU." (+1 more texts); fight: Ram Combat Bot + 5x Ram Mar Cgennie; sets flag 97FC/=1 |
| 02 | (6,13) | L6DD4 | once only (flag 97FC bit mask 2); "RAM FORCES ATTACK."; fight: Ram Combat Bot + 5x Ram Mar Cgennie; sets flag 97FC/=2 |
| 03 | (5,13) | L6E07 | once only (flag 97FC bit mask 4); "A HORDE OF RABID DESERT APES IS DRIVEN FORWARD BY A ROBOT." (+10 more texts); fight: 9x Desert Ape + Ram Combat Bot / or / Ram Combat Bot; -> module 22; treasure: 2000 cr, rocket launcher; sets flag 97FC/=4, flag 9838/=4, [9E08]=2 |
| 04 | (1,12) | L6E56 | "RAM FORCES ATTACK." (+9 more texts); fight: Ram Combat Bot + 5x Ram Mar Cgennie / or / Ram Combat Bot; -> module 22; treasure: 2000 cr, rocket launcher; sets flag 97FC/=8, flag 9838/=4, [9E08]=2 |
| 05 | (1,9) | L6E8B | "RAM FORCES ATTACK." (+9 more texts); fight: Ram Combat Bot + 5x Ram Mar Cgennie / or / Ram Combat Bot; -> module 22; treasure: 2000 cr, rocket launcher; sets flag 97FC/=16, flag 9838/=4, [9E08]=2 |
| 06 | (7,9) | L6EC0 | once only (flag 97FC bit mask 32); "THE COMMANDER SAYS, 'GETTING WARMER, BUT WRONG.' ANOTHER MINE EXPLODES."; damages party; sets flag 97FC/=32 |
| 07 | (6,8) | L6EE8 | "THE COMMANDER SAYS, 'GETTING WARMER, BUT WRONG.' ANOTHER MINE EXPLODES."; damages party; sets flag 97FC/=64 |
| 08 | (12,15) | L6F00 | once only (flag 9851 bit mask 1); "RAM WARRIORS RUSH IN FROM THE OPPOSITE DOOR." (+2 more texts); fight: 5x Ram Warrior + 3x Ram G.D. Gennie; sets flag 9851/=1 |
| 09 | (12,13) | L6F3D | "THE COMMANDER LOOKS STARTLED WHEN YOU ENTER. 'THIS IS HIGHLY IRREGULAR.'" (+1 more texts); fight: 5x Ram Warrior + 3x Ram G.D. Gennie; sets flag 9851/=2 |
| 0A | (10,13) | L6F67 | once only (flag 9851 bit mask 4); "DOG GENNIES ATTACK."; fight: 7x Ram G.D. Gennie; sets flag 9851/=4 |
| 0B | (10,14) | L6F93 | once only (flag 9851 bit mask 8); "THIS ROOM ONCE CONTAINED BUNKS. SHREDDED BEDDING AND BROKEN FRAMES FLOAT AT RANDOM."; sets flag 9851/=8 |
| 0C | (8,15) | L6FAD | once only (flag 9851 bit mask 16); "THE ROOM IS FULL OF ROBOT MAINTENANCE EQUIPMENT. NO ROBOTS ARE IN EVIDENCE."; sets flag 9851/=16 |
| 0D | (8,12) | L6FC7 | once only (flag 9851 bit mask 32); "THIS IS AN OLD MINE AREA. OLD TAILINGS FLOAT IN THE AIR."; sets flag 9851/=32 |
| 0E | (9,9) | L6FE1 | once only (flag 9851 bit mask 64); "TECHNICIANS ARE WORKING HERE. GUARDS TURN AND ATTACK."; fight: 5x Ram Warrior + 3x Ram Technician + 2x Ram G.D. Gennie; sets flag 9851/=64 |
| 0F | (9,8) | L701B | once only (flag 9851 bit mask 128); "REALIZES THAT THIS CONSOLE IS RELAYING INFORMATION TO SOMETHING NEARBY. YOU CANNOT PINPOINT THE TARGET." (+2 more texts); skill check; NPC joins/appears; +500 XP; sets flag 9851/=128, [9E6F]=1, [9E70]=3 |
| 10 | (9,10) | L706E | once only (flag 9852 bit mask 1); "NOTES THAT THIS INTERNAL SENSOR SYSTEM IS FEEDING INFORMATION TO THE CONSOLE TO ITS RIGHT." (+2 more texts); skill check; NPC joins/appears; +500 XP; sets flag 9852/=1, [9E6F]=1, [9E70]=3 |
| 11 | (11,8) | L70C1 | once only (flag 9852 bit mask 2); "THIS IS AN INACTIVE" (+1 more texts); sets flag 9852/=2 |
| 12 | (11,10) | L70E0 | "THE COMMANDER SCUTTLES THROUGH THE FAR DOOR. HIS GUARDS ATTACK."; fight: 5x Ram Warrior + 3x Ram G.D. Gennie; sets flag 9852/=4 |
| 13 | (14,10) | L7105 | "MORE FORCES ATTACK."; fight: 5x Ram Warrior + 3x Ram G.D. Gennie; sets flag 9852/=8 |
| 14 | (14,13) | L712A | once only (flag 9852 bit mask 16); "THE COMMANDER MARSHALS HIS PERSONAL FORCES." (+1 more texts); fight: 3x Ram Mar Cgennie + 4x Ram Warrior; treasure: 100 cr, grenade launcher, explosive grenade, explosive grenade, chaff grenade, chaff grenade, chaff grenade, chaff grenade; sets flag 9852/=16 |
| 15 | (13,14) | L7175 | once only (flag 9852 bit mask 32); "A VOICE ISSUES FROM THE CONSOLE, 'I'M GLAD TO SEE MY ENEMIES FIGHTING." (+2 more texts); fight: Ram Combat Bot; sets flag 9852/=32 |
| 16 | (12,0) | L71AE | once only (flag 9852 bit mask 64); "'READINGS INDICATE HIGH RADIATION. THIS IS DEFINITELY A RADIUM MINE. DON'T WASTE TIME!'"; sets flag 9852/=64 |
| 17 | (12,1) | L71CB | "A ROBOT INTONES, 'THIS IS A RESTRICTED AREA. I WILL USE DEADLY FORCE.'"; fight: Ram Combat Bot; sets flag 9852/=128 |
| 18 | (12,4) | L71F0 | once only (flag 9853 bit mask 1); "AN ALARM HAS BEEN RAISED. ROBOTS CONVERGE ON YOU."; fight: 2x Ram Assault Bot; sets flag 9853/=1 |
| 19 | (10,4) | L7227 | once only (flag 9853 bit mask 2); "THE WALLS ARE MARKED WITH RADIATION SIGNS. YOUR SKIN PRICKLES."; sets flag 9853/=2 |
| 1A | (9,2) | L7241 | once only (flag 9853 bit mask 4); "HAIRLESS BODIES ARE VIRTUALLY MUMMIFIED IN THE DRY AIR."; sets flag 9853/=4 |
| 1B | (9,7) | L725B | once only (flag 9853 bit mask 8); "A MAN IS CRUSHED BETWEEN TWO BOULDERS. HE HAS SOME EQUIPMENT."; fight; treasure: credits, chaff grenade, chaff grenade, chaff grenade, stun grenade, mini explosive, mini explosive; sets flag 9853/=8 |
| 1C | (11,6) | L7288 | "ANOTHER ROBOT BLOCKS YOUR PATH."; fight: Ram Combat Bot; sets flag 9853/=16 |
| 1D | (12,7) | L72AD | "ANOTHER ROBOT BLOCKS YOUR PATH."; fight: Ram Combat Bot; sets flag 9853/=32 |
| 1E | (12,6) | L72C5 | once only (flag 9853 bit mask 64); "THIS RADIATION MONITOR IS WELL INTO THE FATAL RANGE."; sets flag 9853/=64 |
| 1F | (13,6) | L72DF | "THIS CONSOLE CONTROLS AN INTERNAL DEFENSE MECHANISM. DO YOU WANT TO REPROGRAM IT TO ACCEPT YOU?" (+2 more texts); fight: Ram Combat Bot; yes/no prompt; skill check; NPC joins/appears; +500 XP; sets [9E6F]=1, [9E70]=3, flag 9853/=128 |
| 20 | (15,4) | L733F | "AN AUTOMATIC DEFENSE SYSTEM LOCKS ONTO YOU AND FIRES." (+2 more texts); damages party; pushes party back |
| 21 | (14,0) (14,1) (14,2) (14,3) (14,4) | L736A | "YOU FIND JASON DUPARE. HE IS EXTREMELY EMACIATED. 'RAM IS BUILDING A DEADLY DEVICE OVER MERCURY. WARN NEO.'" (+2 more texts); sets flag 9838/=1, flag 9838/=2 |
| 22 | (0,7) | L73A6 | once only (flag 9836 bit mask 1); "'INTRUDER ALERT!' AS YOU ENTER THE CRUDE BASE, ALARMS BLARE. A WALL PANEL OPENS AND A ROBOT EMERGES." (+1 more texts); fight: Ram Combat Bot; sets flag 9836/=1 |
| 23 | (3,8) (4,8) | L73F2 | once only (flag 9836 bit mask 2); "THE WALLS ARE LINED WITH CAGED DESERT APES. THEIR TRAINER OPENS THE CAGES AND RUNS AWAY."; fight: 8x Desert Ape; sets flag 9836/=2 |
| 24 | (4,11) | L7421 | "SHRIEKING DESERT APES THROW HANDFULS OF DIRT AND PEBBLES AT THE TEAM, THEN RUN AWAY." (+1 more texts); damages party; sets flag 9836/=4 |
| 25 | (2,11) | L7467 | once only (flag 9836 bit mask 8); "DESERT APES WATCH YOU PITEOUSLY FROM TINY, DIRTY CAGES. OPEN THE CAGES?" (+1 more texts); fight: 8x Desert Ape; yes/no prompt; pushes party back; sets flag 9836/=8 |
| 26 | (7,6) | L74AB | "THIS ROOM IS FULL OF BACKUP COMMUNICATIONS EQUIPMENT. IT IS GUARDED BY A ROBOT!" (+1 more texts); fight: Ram Combat Bot; sets flag 9836/=16 |
| 27 | (7,3) | L74CE | once only (flag 9836 bit mask 32); "YOU STARTLE THE BASE COMMANDER. HE RETREATS THROUGH THE FAR DOOR, LEAVING HIS GUARDS TO FIGHT."; fight: 8x Ram Warrior + Ram Mar Cgennie; sets flag 9836/=32 |
| 28 | (6,2) | L7501 | once only (flag 9837 bit mask 1); "YOU USE THE CONTROL PANEL TO CONTACT NEO. THEY CONGRATULATE YOU AND BEGIN TO REVIEW PAST COMMUNICATIONS FOR SE"... (+1 more texts); sets flag 9837/=1 |
| 29 | (5,2) | L7520 | "THE COMMANDER HASTENS THROUGH ANOTHER DOOR. A GUARD ROBOT INTERRUPTS YOUR PURSUIT." (+1 more texts); fight: Ram Combat Bot; sets flag 9837/=2 |
| 2A | (4,1) | L7543 | once only (flag 9837 bit mask 4); "THE COMMANDER FLEES INTO YET ANOTHER ROOM. NO GUARDS ARE PRESENT. YOU DESTROY THE EQUIPMENT IN THE ROOM."; sets flag 9837/=4 |
| 2B | (6,0) | L755D | once only (flag 9837 bit mask 8); "THE COMMANDER SUMMONS THE LAST OF HIS GUARDS, A SMALL GROUP OF UNPREPARED TECHNICIANS." (+2 more texts); fight: 4x Ram Technician; explosion effect; sets flag 9837/=8 |
| 2C | (2,2) | L75A2 | once only (flag 9837 bit mask 16); "SAND SQUIDS ARE BEING USED TO DRILL TUNNELS IN THE ASTEROID. THEIR TRAINERS DIRECT THEM TOWARDS THE TEAM."; fight: 6x Sand Squid + 3x Ram Warrior; sets flag 9837/=16 |
| 2D | (0,6) | L75D5 | once only (flag 9837 bit mask 32); "YOU NOTICE A CACHE OF HIDDEN GRENADES."; fight; skill check; treasure: credits, chaff grenade, chaff grenade, chaff grenade, chaff grenade, explosive grenade, explosive grenade; sets [9E6F]=1, [9E70]=2, flag 9837/=32 |
| 2E | (3,14) | L761F | once only (flag 97FC bit mask 128); "FROM YOUR RIGHT COMES SNIPER FIRE."; damages party; sets flag 97FC/=128 |
| 2F | (5,12) | L7647 | once only (flag 97FD bit mask 1); "YOU CATCH UP WITH THE SNIPERS. FROM BEHIND, MORE ENEMY APPEAR." (+1 more texts); fight: 5x Ram Mar Cgennie + 2x Ram Assault Bot; sets flag 97FD/=1 |
| 30 | (0,14) | L767F | once only (flag 97FD bit mask 2); "ROBOTS EMERGE FROM BENEATH THE TUNNEL. GILBERT LAUGHS, 'ARE THINGS GETTING WARMER?'"; fight: Ram Assault Bot + 2x Ram Combat Bot; sets flag 97FD/=2 |
| 31 | (6,10) | L76B2 | once only (flag 97FD bit mask 4); "A MINE EXPLODES!" (+2 more texts); fight: 8x Ram Mar Cgennie; damages party; sets flag 97FD/=4 |
| 32 | (2,8) | L76F6 | "A ROBOT CUTS OFF YOUR ESCAPE."; fight: Ram Combat Bot; sets flag 97FD/=8 |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]!=1 | S | L6C44 | (4,15) | "DO YOU WISH TO RETURN TO YOUR SHIP?"; yes/no prompt; -> module 22; sets [9E08]=2, [9DBF]=255 |
| [9E6F]!=8 | S | L6C58 | (12,15) | "DO YOU WISH TO RETURN TO YOUR SHIP?"; yes/no prompt; -> module 22; sets [9E08]=2, [9DBF]=255 |
| [9E6F]!=22 | N | L6C6C | (12,0) | "DO YOU WISH TO RETURN TO YOUR SHIP?"; yes/no prompt; -> module 22; sets [9E08]=2, [9DBF]=255 |

## Module 51 / map 51 Lowlander Village, Venusian Space Elevator Ruins

Search: `ONGOTO [9E6F]`, 14 targets, code 0 and codes >= 14 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,1) (3,15) | L6E7D | "THIS IS A SUPPLY SHED." (+5 more texts); menu: Leave It / Open It / Take It; menu: Pick It Up / Leave It / Taste Food; damages party; menu: Take The Bag / Leave The Bag; sets [989E]=2, [989E]=1 |
| 02 | (13,4) (3,5) | L6F1C | "A TINY LOWLANDER STOPS CRYING AND BEGINS TO BABBLE IN A LANGUAGE YOU DON'T RECOGNIZE." (+4 more texts); NPC joins/appears; yes/no prompt; sets [98A7]=1 |
| 03 | (9,1) | L74B2 | no event here (cell is a plain marker) |
| 04 | (3,1) | L74B2 | no event here (cell is a plain marker) |
| 05 | (2,2) (6,9) | L6FE8 | "ACID FROGS NOW INFEST THE STOREROOM. THE SUPPLIES ARE COATED WITH ACID SLIME FROM THE FROGS." (+8 more texts); fight: 11x Swamp Hornet; yes/no prompt; +5000 XP; treasure: credits, demo charge, demo charge; sets [98A8]=1, [98B6]=1, [989E]=3, [9802]=1, [98AA]=1, [9802]=2, [98B7]=1 |
| 06 | (3,2) (10,11) | L713C | "YOU FIND THE BODIES OF A SLAIN FAMILY. A LOWLANDER BURSTS OUT AND AIMS A RIFLE AT THE TEAM. 'WHHO ARE YOU?'" (+5 more texts); menu: Back Away / Talk; pushes party back; pushes party back; +500 XP; sets [98A9]=1 |
| 07 | (0,0) (14,2)o | L71A9 | "A GROUP OF LOWLANDERS ARE HUDDLED HERE, TENDING TO A WOUNDED FEMALE." (+10 more texts); fight; +3000 XP; treasure: credits, item 55, item 83; sets [98AB]=1, [98AB]=3 |
| 08 | (1,6) (10,14) | L7263 | "A RAM OFFICER IS SNORING WITH HIS FEET ON A DESK. A HUGE KEYRING IS FASTENED TO HIS BELT." (+7 more texts); fight: 3x Ram H.S. Robot + Ram Warrior + 2x Ven. H.S. Robot + 2x Acid Frog; menu: Steal Keys / Kill The Officer / Leave; pushes party back; skill check; menu: Leave / Kill The Officer; sets [... |
| 09 | (3,0) (3,12) | L73BC | "THE MEDICAL SUPPLIES HERE ARE FOR LOWLANDERS, BUT YOU FIND POISON ANTIDOTES, AND TWO WEAPONS." (+1 more texts); fight; treasure: credits, poison antidote, poison antidote, poison antidote, item 54, item 67; sets [98AB]=2 |
| 0A | (9,0) (5,11)o | L73E6 | "THIS BUILDING APPEARS TO HAVE BEEN THE CENTER OF GOVERNMENT." (+1 more texts) |
| 0B | (2,5) (2,7) (3,7) (11,7) (14,7) (2,14) (3,14) | L740A | "LASER BURNS MAR THE WALLS OF THIS HUT." (+7 more texts); random roll; sets [9801]=1 |
| 0C | (2,6) (12,6)o | L7490 | "THIS IS THE LARGEST HUT IN TOWN. IT APPEARS RELATIVELY UNDAMAGED." (+1 more texts) |
| 0D | (12,12)o (8,15)o | L6C5E | "YOU'RE AT THE MAIN GATE." (+2 more texts); yes/no prompt; -> module 50; sets [9DBF]=255 |
| 0E | (7,14)o | (falls through) | step event: facing W: "THIS IS AN ACID FROG STOCKADE. THE FROGS HAVE BEEN MADDENED BY THE SOUNDS OF BATTLE." (+8 more texts); fight: 6x Acid Frog; yes/no prompt; damages party; sets [9DBF]=255, [989B]=1, [97F8]=1 |
| 0F | (5,1) (6,10)o | (falls through) | step event: facing N: "THIS IS A STOREROOM. YOU SEE THROUGH THE WINDOW THAT IT'S OVERRUN WITH SWAMP HORNETS." (+2 more texts); yes/no prompt; sets [9DBF]=255 |
| 10 | (9,3) (8,11)o | (falls through) | step event: facing E: "THIS DOOR HAS BEEN BARRICADED SO STURDILY THAT BREAKING IT DOWN WOULD TOPPLE THE ENTIRE STRUCTURE."; sets [9DBF]=255 |
| 11 | (2,12) | (falls through) | step event: facing E: "THIS MEDICAL SUPPLY CLOSET IS LOCKED WITH LOWLANDER TECHNOLOGY YOU DON'T RECOGNIZE. YOU NEED A KEY AND A SECUR"... (+4 more texts); sets [9DBF]=255 |
| 13 | (5,2) | (falls through) | no search event (code beyond the table) |
| 14 | (8,3) | (falls through) | no search event (code beyond the table) |

Handlers no cell can reach: none

Codes beyond the table (fall through): 0E 0F 10 11 13 14

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==14 | W | L6DBB | (7,14)o | "THIS IS AN ACID FROG STOCKADE. THE FROGS HAVE BEEN MADDENED BY THE SOUNDS OF BATTLE." (+8 more texts); fight: 6x Acid Frog; yes/no prompt; damages party; sets [9DBF]=255, [989B]=1, [97F8]=1 |
| [9E6F]==15 | N | L6FB1 | (5,1) (6,10)o | "THIS IS A STOREROOM. YOU SEE THROUGH THE WINDOW THAT IT'S OVERRUN WITH SWAMP HORNETS." (+2 more texts); yes/no prompt; sets [9DBF]=255 |
| [9E6F]==16 | E | L7129 | (9,3) (8,11)o | "THIS DOOR HAS BEEN BARRICADED SO STURDILY THAT BREAKING IT DOWN WOULD TOPPLE THE ENTIRE STRUCTURE."; sets [9DBF]=255 |
| [9E6F]==17 | E | L735C | (2,12) | "THIS MEDICAL SUPPLY CLOSET IS LOCKED WITH LOWLANDER TECHNOLOGY YOU DON'T RECOGNIZE. YOU NEED A KEY AND A SECUR"... (+4 more texts); sets [9DBF]=255 |

## Module 52 / map 52 Venus RAM Base

Search: `ONGOTO [9E6F]`, 21 targets, code 0 and codes >= 21 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (7,0) (15,0) (7,8) (15,8) | L6F15 | "THERE ARE FOUR BUTTONS IN THE ELEVATOR." (+6 more texts); menu: 1 / 2 / 3 / 4 / None; staircase; staircase; staircase; staircase; moves party to (15,8) facing S; sets [98AE]=1, [98AE]=2, [98AE]=3, [98AE]=4 |
| 02 | (12,9) | L705E | "'ACIDICIUM COMPOUND AHEAD.'" (+1 more texts) |
| 03 | (12,8) | L708F | "ACIDICIUM OOZE TOWARDS YOU IN A SLIMY MASS." (+1 more texts); fight: 5x Acidicium; sets [97F7]=1 |
| 04 | (12,10) | L70BD | "GUARD ROBOTS ZERO IN ON YOU." (+1 more texts); fight: 7x Ram H.S. Robot; sets [97F8]=1 |
| 05 | (0,9) | L715B | "THE LOWLANDER SCIENTISTS WILL STAY HERE UNTIL YOU BRING WORD TO THEM FROM LANDON." (+4 more texts); sets [98AF]=1, [98AF]=4 |
| 06 | (15,12) | L71C5 | "YOU TAP THE LOWLANDER CODE, AND LANDON CAUTIOUSLY APPROACHES." (+14 more texts); NPC joins/appears; NPC joins/appears; +1500 XP; NPC joins/appears; +2000 XP; NPC joins/appears; NPC joins/appears; sets [98AF]=2, [98AF]=3, [98AF]=5 |
| 07 | (1,2) (10,4) | L73BE | "UNDER A FLOOR PANEL YOU FIND A RAM RETINAL LOCKPICK! YOU NEVER WOULD HAVE FOUND IT WITHOUT LANDON'S DIRECTIONS"... (+6 more texts); fight: 9x Ram Warrior; menu: Surrender / Run / Fight; damages party; pushes party back; damages party; sets [98B0]=1, [97FD]=2, [97FD]=1, [97FE]=1 |
| 08 | (11,0) (12,0) (8,2) (12,2) (13,2) (14,2) (15,2) (8,3) ... | L74C9 | "THIS IS AN EIGHT-MAN RAM BARRACK." (+1 more texts); fight: Ram Assassin; random roll; random roll; sets [97FB]=1 |
| 09 | (2,10) (3,10) (4,10) (5,10) (0,11) (2,11) (3,11) (4,11) ... | L7516 | "THIS IS A SMALL OFFICE." (+1 more texts); fight: Ram Assassin; random roll; random roll; sets [97FC]=1 |
| 0A | (2,14) | L7562 | "THREE RAM WARRIORS REACH FOR THEIR WEAPONS, BUT HESITATE AFTER ASSESSING THE TEAM'S STRENGTH. YOU'RE AT A STAN"... (+7 more texts); fight: 3x Ram Warrior; menu: Leave / Attack / Intimidate Them; skill check; menu: Show Mercy / Attack; +1000 XP; sets [98B3]=1, [9E6F]=1, [9E70]=2, [98B4]=1 |
| 0B | (5,4) | L7687 | "MAIN LAB" |
| 0C | (2,0) | L7694 | "LAB 2" |
| 0D | (2,7) | L7699 | "LAB 3" |
| 0E | (1,0) (4,4) (1,7) | L769E | "BROKEN BEAKERS AND BITS OF EQUIPMENT LITTER THE FLOOR." |
| 0F | (3,14) | L76A4 | "COMPUTER ROOM 1" |
| 10 | (4,14) | L76A9 | "COMPUTER ROOM 2" |
| 11 | (9,13) | L76AE | "THERE IS A DIRT FLOOR HERE, AND A DEEP ACID POOL. AN ACID PUMP NEARBY IS BADLY MANGLED. INVESTIGATE?" (+1 more texts); fight: 9x Ursadder; yes/no prompt; sets [97FA]=1 |
| 12 | (8,15) | L76E1 | "AN ACID-FILLED TUNNEL LEADS TO THE JUNGLE. THE ACID IS SO STRONG THAT EVEN IF YOU SURVIVED, YOUR EQUIPMENT WOU"... |
| 13 | (0,15) | L7644 | "THREE RAM WARRIORS ARE BOUND AND GAGGED ON THE FLOOR OF THIS CLOSET." (+2 more texts); menu: Kill Them / Leave; sets [98B4]=2 |
| 14 | (7,1) (15,1) (7,9) (15,9) | L7029 | "'FLOOR 1 -- LABS.'" (+3 more texts) |
| 16 | (13,4) (7,7) | (falls through) | step event: "THIS SECURITY DOOR OPENS WHEN YOU FIND THE CORRECT KEY IN YOUR KEYRING." (+2 more texts); sets [9DBF]=255 |
| 17 | (0,4) | (falls through) | step event: facing W: "THIS IS THE MAIN EXIT FROM THE BASE, LEADING UP TO THE VENUSIAN JUNGLE. WILL YOU LEAVE THE BASE?"; yes/no prompt; -> module 50; sets [9DBF]=255 |
| 18 | (15,7) | (falls through) | step event: facing E: "BEYOND THIS DOOR LIES A DARK TUNNEL DRIPPING WITH ACID. ENTER THE TUNNEL?"; yes/no prompt; -> module 53; sets [9DBF]=255 |
| 19 | (11,8) | (falls through) | step event: facing W: "EXAMINING THE WALL CLOSELY, YOU FIND A CONCEALED LATCH. THIS MUST BE THE SECRET DOOR!"; sets [98B1]=1 |
| 1A | (6,4) | (falls through) | step event: no event here (cell is a plain marker) |

Handlers no cell can reach: none

Codes beyond the table (fall through): 16 17 18 19 1A

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==22 |  | L70EB | (13,4) (7,7) | "THIS SECURITY DOOR OPENS WHEN YOU FIND THE CORRECT KEY IN YOUR KEYRING." (+2 more texts); sets [9DBF]=255 |
| [9E6F]==23 | W | L713A | (0,4) | "THIS IS THE MAIN EXIT FROM THE BASE, LEADING UP TO THE VENUSIAN JUNGLE. WILL YOU LEAVE THE BASE?"; yes/no prompt; -> module 50; sets [9DBF]=255 |
| [9E6F]==24 | E | L7147 | (15,7) | "BEYOND THIS DOOR LIES A DARK TUNNEL DRIPPING WITH ACID. ENTER THE TUNNEL?"; yes/no prompt; -> module 53; sets [9DBF]=255 |
| [9E6F]==25 | W | L73B2 | (11,8) | "EXAMINING THE WALL CLOSELY, YOU FIND A CONCEALED LATCH. THIS MUST BE THE SECRET DOOR!"; sets [98B1]=1 |
| [9E6F]==26 |  | L76E7 | (6,4) | no event here (cell is a plain marker) |

## Module 53 / map 51 Lowlander Village, Venusian Space Elevator Ruins

Search: `ONGOTO [9E6F]`, 11 targets, code 0 and codes >= 11 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,1) (3,15) | L6E97 | "MUTATED GENNIES SWARM UP A LADDER! WITH HISSES AND HOWLS OF GLEE, THEY ATTACK." (+2 more texts); fight: 7x Ursadder; yes/no prompt; sets [98A5]=2 |
| 02 | (13,4) (3,5) | L6ECF | "THIS COMPUTER ROOM IS OCCUPIED BY A LONE RAM TECHNICIAN. HE PROMISES NOT TO REVEAL YOUR PRESENCE IF YOU LET HI"... (+11 more texts); fight: 5x Ram H.S. Robot + 3x Ram Warrior; menu: Let Him Go / Interrogate Him / Kill Him; +1250 XP; menu: Let Him Go / Tie Him Up / Kill Him; sets [989F]=1, [98A3]=1,... |
| 03 | (9,1) | L6FDA | "YOU FIND A DETONATOR KEYCARD, BUT YOU NEED THE CORRESPONDING CONTROL BOX TO USE IT." (+5 more texts); yes/no prompt; +5000 XP; sets [98A0]=1, [98A2]=1, [97F9]=1 |
| 04 | (3,1) | L6FF9 | "YOU PICK UP A PORTABLE CONTROL BOX. ITS FUNCTION IS TO CAUSE THE GLIDERS TO SELF-DESTRUCT IN THE EVENT OF CAPT"... (+7 more texts); fight; yes/no prompt; +5000 XP; treasure: credits, explosive grenade, explosive grenade, explosive grenade, explosive grenade; sets [98A1]=1, [98A2]=1, [97F9]=1 |
| 05 | (2,2) (6,9) | L70DD | "A TANGLE OF CABLES COLLAPSED IN THE EXPLOSION. ONE TEAM MEMBER CAN TRY TO CLIMB OVER AND CLEAR A PATH." (+2 more texts); skill check; damages party; sets [9E70]=1, [97F7]=1 |
| 06 | (3,2) (10,11) | L714A | "THIS IS A RAM MEETING ROOM. CIGARETTE BUTTS LITTER THE FLOOR." (+2 more texts); yes/no prompt |
| 07 | (0,0) (14,2)o | L6C7F | "FROM HERE YOU CAN CLIMB UP TO THE JUNGLE. LEAVE THE RUINS?"; yes/no prompt; -> module 50 |
| 08 | (1,6) (10,14) | L6C89 | "A LADDER LEADS TO A DEVELOPED AREA UNDERGROUND. GO DOWN?"; yes/no prompt; moves party to (3,0) facing W; sets [97DC]=2, [97A1]=1 |
| 09 | (3,0) (3,12) | L6CC4 | "A LADDER LEADS UP INTO DARK RUINS. CLIMB UP?"; yes/no prompt; moves party to (1,6) facing S; sets [97DC]=130 |
| 0A | (9,0) (5,11)o | L6CFF | "YOU DISCOVER THE ENTRANCE TO AN UNDERGROUND TUNNEL. WILL YOU ENTER?"; yes/no prompt; -> module 52 |
| 0B | (2,5) (2,7) (3,7) (11,7) (14,7) (2,14) (3,14) | (falls through) | step event: facing S: "THIS IS NO WAY TO GET PAST THIS SECURITY DOOR WITHOUT A SECURITY CARD." (+2 more texts); sets [9DBF]=255, [98A4]=1 |
| 0C | (2,6) (12,6)o | (falls through) | step event: facing N: "THE SECURITY DOOR IS UNLOCKED." (+1 more texts); sets [9DBF]=255 |
| 0D | (12,12)o (8,15)o | (falls through) | no search event (code beyond the table) |
| 0E | (7,14)o | (falls through) | no search event (code beyond the table) |
| 0F | (5,1) (6,10)o | (falls through) | no search event (code beyond the table) |
| 10 | (9,3) (8,11)o | (falls through) | no search event (code beyond the table) |
| 11 | (2,12) | (falls through) | no search event (code beyond the table) |
| 13 | (5,2) | (falls through) | step event: "RAM GLIDERS FILL THIS HANGAR. A GLASS DOME ABOVE IS CAMOUFLAGED BY THE SWAMP. YOU FIND NO WAY TO OPEN THE DOME"... (+1 more texts); sets [97F8]=1, [9806]=1 |
| 14 | (8,3) | (falls through) | step event: "RAM GLIDERS FILL THIS HANGAR. A GLASS DOME ABOVE IS CAMOUFLAGED BY THE SWAMP. YOU FIND NO WAY TO OPEN THE DOME"... (+1 more texts); sets [97F8]=1, [9806]=1 |

Handlers no cell can reach: none

Codes beyond the table (fall through): 0B 0C 0D 0E 0F 10 11 13 14

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==11 | S | L6D11 | (2,5) (2,7) (3,7) (11,7) (14,7) (2,14) (3,14) | "THIS IS NO WAY TO GET PAST THIS SECURITY DOOR WITHOUT A SECURITY CARD." (+2 more texts); sets [9DBF]=255, [98A4]=1 |
| [9E6F]==12 | N | L6D44 | (2,6) (12,6)o | "THE SECURITY DOOR IS UNLOCKED." (+1 more texts); sets [9DBF]=255 |
| [9E6F]==19 |  | L70AA | (5,2) | "RAM GLIDERS FILL THIS HANGAR. A GLASS DOME ABOVE IS CAMOUFLAGED BY THE SWAMP. YOU FIND NO WAY TO OPEN THE DOME"... (+1 more texts); sets [97F8]=1, [9806]=1 |
| [9E6F]==20 |  | L70AA | (8,3) | "RAM GLIDERS FILL THIS HANGAR. A GLASS DOME ABOVE IS CAMOUFLAGED BY THE SWAMP. YOU FIND NO WAY TO OPEN THE DOME"... (+1 more texts); sets [97F8]=1, [9806]=1 |
| [9E6F]==21 |  | L70C9 | (none) | "AROUND YOU IS THE WRECKAGE OF THE GLIDER HANGAR. TWISTED BITS OF GLIDER LIE STREWN THROUGHOUT THE ROOM."; sets [9806]=1 |

## Module 5F / map 63 Enemy Ships

No search dispatch (no `AND [9AF9],63` + `ONGOTO`): this map has no cell-event table. 

## Module 60 / map 60 Mercury Merchants Area

Search: `ONGOTO [9E6F]`, 42 targets, code 0 and codes >= 42 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (0,14) | L72E6 | no event here (just the random-encounter check) |
| 02 | (0,11) | L72E6 | no event here (just the random-encounter check) |
| 03 | (1,12) | L6CD8 | "A SENSOR BEAM SCANS YOUR STOLEN BADGES. A MECHANICAL VOICE SAYS, 'PASSED. WELCOME TO MERCURY. HAVE A NICE DAY."... (+1 more texts) |
| 04 | (2,12) | L6CF2 | once only (flag 97FB bit mask 1); "COLONEL WILMA DEERING STEPS UP THE MOMENT YOU CLEAR SECURITY." (+2 more texts); sets flag 97FB/=1 |
| 05 | (1,9) (7,9) (4,15) | L6D23 | once only (flag 97FB bit mask 2); "A MAN BUSTLES PAST AND MUMBLES," (+7 more texts); menu: Ignore / Speak / Accost; yes/no prompt; sets flag 97FB/=2 |
| 06 | (0,4) (5,7) | L6D92 | once only (flag 97FB bit mask 4); "WELCOME TO MARAT'S BAZAAR. ANYTHING YOU COULD WISH IN THE WAY OF ARCANA IS HERE."; sets flag 97FB/=4 |
| 07 | (3,3) | L6DAF | once only (flag 97FB bit mask 8); "A PARROT IS SITTING ON A WOODEN POST HERE, SQUAWKING LOUDLY." (+9 more texts); menu: Feed / Play / Ignore; skill check; NPC joins/appears; NPC joins/appears; damages party; sets flag 97FB/=8, [9E6F]=1, [9E6F]=2 |
| 08 | (4,2) | L6E80 | once only (flag 97FB bit mask 16); "AN OLD DESKCOMP SITS HERE, COMPLETE WITH ARCHAIC KEYBOARD. AS YOU PEER AT THE DUSTY LENS, THE VIDEO SCREEN BEG"... (+3 more texts); sets flag 97FB/=16 |
| 09 | (4,7) | L6EA9 | "THROUGH THE DUST ON THE DOOR YOU MAKE OUT, 'TO RUE DE S.'" |
| 0A | (8,4) | L6EAF | once only (flag 97FB bit mask 64); sets flag 97FB/=64 |
| 0B | (8,3) | L72E6 | no event here (just the random-encounter check) |
| 0C | (6,8) (8,11) | L6F7E | "A HOLOGRAM FORMS OVER THE DOOR:" (+1 more texts) |
| 0D | (7,8) | L6F89 | once only (flag 97FD bit mask 1); "A SECURITY WARNING BLARES. THE GUARDS REACH FOR THEIR WEAPONS."; fight: 5x Mer. Warrior + Mer. Leader; sets flag 97FD/=1 |
| 0E | (9,11) | L6FA1 | once only (flag 97FD bit mask 2); "A SECURITY WARNING BLARES. THE GUARDS REACH FOR THEIR WEAPONS."; fight: 5x Mer. Warrior + Mer. Leader; sets flag 97FD/=2 |
| 0F | (10,13) | L6FD4 | "THE SIGN ABOVE THE DOOR READS" (+4 more texts); fight: 7x Mer. Warrior + Mer. Leader + 6x Mer. H.S. Robot; pushes party back; sets flag 97FD/=4, [9DC1]=1 |
| 10 | (10,14) | L7030 | "GUARDS BURST OUT FROM EVERYWHERE, GUNS BLAZING." (+2 more texts); fight: 7x Mer. Warrior + Mer. Leader + 6x Mer. H.S. Robot; pushes party back; sets [9DC1]=1 |
| 11 | (6,6) | L7039 | once only (flag 97FC bit mask 4); "THIS CORRIDOR IS DUSTY AND DISUSED."; sets flag 97FC/=4 |
| 12 | (4,9) (3,11) | L7053 | "MAINTENANCE ROBOTS ARE LINED UP HERE." |
| 13 | (5,14) | L7059 | "MAINTENANCE ROBOTS ARE LINED UP HERE." |
| 14 | (3,15) | L705D | "A HOLOGRAM FORMS OVER THE DOOR:" (+1 more texts) |
| 15 | (3,14) | L7068 | once only (flag 97FC bit mask 8); "MAGNETIZED BOOTS OF ALL DESCRIPTIONS ADORN THE SHELVES. NO ONE IS IN THE STORE."; sets flag 97FC/=8 |
| 16 | (5,10) | L7082 | "A HOLOGRAM FORMS OVER THE DOOR:" (+1 more texts) |
| 17 | (5,11) | L708D | once only (flag 97FC bit mask 16); "SPRAY CANS OF FAKE MARBLE AND LITTLE SCULPTURES LIE ABOUT THE CLOSED STORE."; sets flag 97FC/=16 |
| 18 | (8,10) | L70A7 | "A HOLOGRAM FORMS OVER THE DOOR:" (+1 more texts) |
| 19 | (7,10) | L70B2 | once only (flag 97FC bit mask 32); "THE CLOSED STORE HAS QUICK PLASTIC SURGERY KITS. A SPECIAL IS OFFERED ON ELVIS LOOKALIKES."; sets flag 97FC/=32 |
| 1A | (0,7) | L70CC | "A HOLOGRAM FORMS OVER THE DOOR:" (+1 more texts) |
| 1B | (1,7) | L70D7 | once only (flag 97FD bit mask 32); "THE MOMENT YOU ENTER, YOU COME FACE TO FACE WITH YOURSELF. THE HOLOGRAMS MATCH YOUR EVERY MOVEMENT UNTIL YOU L"...; sets flag 97FD/=32 |
| 1C | (5,4) | L70F1 | once only (flag 97FC bit mask 64); "YOU ARE BURIED UNDER A HUGE PILE INCLUDING AN OLD BLACKBOARD, TWO SPITTOONS AND A COPY OF THE MONA LISA."; sets flag 97FC/=64 |
| 1D | (11,1) | L710B | once only (flag 97FB bit mask 128); "THIS SHOP IS CLOSED. IT SELLS CHEAP HOLOGRAMS OF MERCURIAN SIGHTS."; sets flag 97FB/=128 |
| 1E | (11,6) | L7125 | once only (flag 97FC bit mask 1); "THE ROOM IS DARK. STALE DOUGHNUTS LINE A COUNTER."; sets flag 97FC/=1 |
| 1F | (10,10) | L713F | once only (flag 97FC bit mask 128); "IN THIS OFFICE ARE A NUMBER OF INTERESTING RECORDS. TWO OF THEM CATCH YOUR EYE." (+2 more texts); sets flag 97FC/=128 |
| 20 | (7,7) | L7163 | once only (flag 97FD bit mask 8); sets flag 97FD/=8 |
| 21 | (8,5) (8,8) (11,12) | L72E6 | no event here (just the random-encounter check) |
| 22 | (7,6) | L7178 | once only (flag 97FD bit mask 16); sets flag 97FD/=16 |
| 23 | (13,7) | L718D | once only (flag 97FC bit mask 2); "THE PLAZA IS NEARLY EMPTY."; sets flag 97FC/=2 |
| 24 | (10,1) | L72E6 | no event here (just the random-encounter check) |
| 25 | (10,6) | L72E6 | no event here (just the random-encounter check) |
| 26 | (15,13) (15,14) (15,15) | L72E6 | no event here (just the random-encounter check) |
| 27 | (14,13) (14,14) (14,15) | L71A7 | "TROOPS ARE POURING INTO THE PLAZA AND RACING TOWARDS YOU. DO YOU FLEE?" (+1 more texts); fight: 5x Mer. Warrior + Mer. Leader; yes/no prompt; yes/no prompt; pushes party back |
| 28 | (14,0) | L72E6 | no event here (just the random-encounter check) |
| 29 | (0,0) (1,0) (2,0) (0,1) (1,1) (2,1) (3,1) (0,2) ... | L71E8 | "BOLTS OF ANCIENT DAMASK SILK LIE ATOP A SLIGHTLY NEWER TABLETOP VIDEO GAME." (+2 more texts); random roll |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==38 | E | L6ED9 | (15,13) (15,14) (15,15) | "SECURITY IS DOING A PAT SEARCH ON EVERYONE TRYING TO PASS THESE GATES. LASER CANNON TRACK THE SQUARE. DO YOU L"... (+15 more texts); fight: 2x Mer. Leader + 10x Mer. Warrior + 8x Mer. H.S. Robot; menu: Be Searched / Attack; menu: Surrender / Attack; damages party; yes/no prompt; menu: Speak / Ignor... |
| [9E6F]==39 | E | L6EC9 | (14,13) (14,14) (14,15) | "AHEAD YOU CAN SEE A HEAVY SECURITY STATION. ANTI-PERSONNEL GUNS ARE MOUNTED ABOVE THE DOORS. IT MIGHT BE WISE"...; yes/no prompt; pushes party back |
| [9E6F]==40 | S | L6C10 | (14,0) | "YOU ARE ABOUT TO ENTER THE CORE. DO YOU CONTINUE?"; yes/no prompt; -> module 61; sets [9DBF]=255 |
| [9E6F]==1 | W | L6BF9 | (0,14) | "DO YOU WISH TO RETURN TO YOUR SHIP?"; yes/no prompt; -> module 22; sets [9E08]=2, [9DBF]=255 |
| [9E6F]==2 | W | L6BED | (0,11) | "THE ENTRANCE TO THE HANGAR HAS BEEN SEALED DUE TO A SECURITY ALERT. YOU CANNOT ENTER."; sets [9DBF]=255 |
| [9E6F]==4 | W | L6BE1 | (2,12) | "A MECHANICAL VOICE STATES, 'NO ENTRY WITHOUT VALID PASSCARD.'"; sets [9DBF]=255 |

## Module 61 / map 61 Mariposa Core

Search: `ONGOTO [9E6F]`, 19 targets, code 0 and codes >= 19 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (3,3) (10,3) (3,10) (10,10) (14,14) (15,14) (14,15) (15,15) | L6C1F | "THIS CORE RUNS THE LENGTH OF THE MARIPOSA. YOU CAN GO UPWARD OR DOWNWARD." (+11 more texts); fight: 15x Ram Assault Bot; menu: Go Up / Descend; menu: Go Up / Descend / Leave Core; -> module 62; staircase; explosion effect; explosion effect; damages party; explosion effect; damages party; staircase;... |
| 02 | (14,2) | L6F02 | "YOU ENTER THE AUDIENCE CHAMBER. THE SUN KING SITS BEFORE YOU ON A GOLDEN THRONE." (+15 more texts); menu: Oui / Non; pushes party back; menu: Oui / Non; menu: Oui / Non; menu: Grovel / Refuse; sets [98C7]=2, [98C7]=1, [98C7]=3 |
| 03 | (10,2) (9,3) (11,3) (10,4) | L70DA | "YOU FACE A STATUE OF AN EAGLE. EVERY FEATHER IS WOVEN FROM STRANDS OF GOLD."; sets [97F9]=1 |
| 04 | (3,9) (2,10) (4,10) (3,11) | L70C6 | "A STATUE OF AN OWL FACES THE TEAM. ITS WINGS ARE PLATED WITH SILVER."; sets [97F9]=1 |
| 05 | (3,2) (2,3) (4,3) (3,4) | L70EE | "LOOMING OVER YOU IS A STATUE OF A HAWK. ITS COPPER PLATING HAS RECENTLY BEEN POLISHED."; sets [97F9]=1 |
| 06 | (8,12) | L7116 | "A SERVANT IS TAKING A BREAK HERE. HE'S WEARING FRENCH REVOLUTION REGALIA. 'PSST,' HE WHISPERS, 'LOOKING FOR TH"... (+3 more texts); yes/no prompt; sets [98C6]=1 |
| 07 | (11,5) (12,5) | L7272 | "DEACTIVATED ROBOTS FILL THE ROOM. ON THE WALL IS A PLAQUE WITH A GOLD MEDALLION EMBOSSED WITH AN EAGLE. GET TH"... (+6 more texts); fight: Ram Combat Bot + Ram Assault Bot; yes/no prompt; skill check; pushes party back; sets [9E71]=2, [97FA]=1 |
| 08 | (5,13) | L723C | "DOODADS AND KNICKKNACKS FILL THIS STORAGE AREA. PAPER FLAGS AND OLD MEDALS ARE ONLY A SMALL SAMPLE. SEARCH THE"... (+5 more texts); yes/no prompt; yes/no prompt; yes/no prompt; sets [97FB]=1 |
| 09 | (0,5) | L7174 | "A ROBOT HERE HAS BEEN PROGRAMMED FOR MAINTENANCE. IT IS VACUUMING THE FLOOR. ITS VACUUM NEARS A COPPER MEDALLI"... (+7 more texts); fight: 3x Mer. H.S. Robot; menu: Grab Medallion / Fight Robot / Leave; skill check; sets [9E71]=2, [97FC]=1 |
| 0A | (2,14) | L72F5 | "THIS BEDROOM IS DRAPED WITH STRINGS OF PEARLS. THE WALLS ARE COVERED WITH SILK AND VELVET." |
| 0B | (14,12) | L7350 | "AS YOU STEP INTO THIS AVIARY, DOZENS OF BIRDS FLY OUT." |
| 0C | (4,15) | L7303 | "THIS IS THE SUN KING'S WARDROBE. MILITARY UNIFORMS FROM HISTORIC FRANCE, AMERICA, AND RUSSIA FILL THE ROOM." |
| 0D | (12,14) | L7311 | "YOU FIND A CHESSBOARD. THE PIECES ARE MODELS OF RAM OFFICERS AND FRENCH REVOLUTIONARY SOLDIERS." (+3 more texts); yes/no prompt; sets [97FE]=1 |
| 0E | (10,9) (9,10) (11,10) (10,11) | L7102 | "HERE IS A GIGANTIC STATUE OF A BIRD. IT'S HARD TO DISCERN ITS SPECIES, BECAUSE EVERY INCH IS CRUSTED WITH GEMS"...; sets [97F9]=1 |
| 0F | (3,5) (4,5) | L735E | no event here (cell is a plain marker) |
| 10 | (5,0) | L735F | "WEAPONS CONTROL" (+6 more texts); yes/no prompt; yes/no prompt; explosion effect; damages party; yes/no prompt; pushes party back |
| 11 | (8,1) | L73AC | "THIS ROOM IS FILLED WITH MACHINERY AND GIGANTIC GEARS. AN INFORMATION SCREEN ON THE WALL READS: 'OPERATION CON"... |
| 12 | (14,5) | L73B2 | "A PLAQUE READS --" (+3 more texts) |
| 13 | (14,3) | (falls through) | step event: facing N: "THE DOOR IS IMPENETRABLY LOCKED. GUARDS JEER AND TOSS GARBAGE THROUGH A TINY GRILLE." (+9 more texts); yes/no prompt; yes/no prompt; sets [9DBF]=255 |

Handlers no cell can reach: none

Codes beyond the table (fall through): 13

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==19 | N | L7026 | (14,3) | "THE DOOR IS IMPENETRABLY LOCKED. GUARDS JEER AND TOSS GARBAGE THROUGH A TINY GRILLE." (+9 more texts); yes/no prompt; yes/no prompt; sets [9DBF]=255 |

## Module 62 / map 62 Mercurian Finale, Weapons Control Level

Search: `ONGOTO [9E6F]`, 20 targets, code 0 and codes >= 20 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (9,0) (0,2) (0,9) | L6E17 | "YOU'RE AT THE BOTTOM OF AN EMERGENCY STAIRWELL. CLIMB THE STAIRS?" (+6 more texts); yes/no prompt; menu: Climb Stairs / Descend Stairs / Leave; yes/no prompt; moves party to (0,9) facing E |
| 02 | (15,6) (6,15) | L6F6C | "THIS ELEVATOR HAS FOUR BUTTONS ON A HIGH-TECH PANEL." (+7 more texts); menu: Security / Pods / Control Area / Leave; moves party to (15,6) facing E; sets [97F7]=1, [97F7]=2 |
| 03 | (11,11) | L7080 | "YOU SEE THE DOOMSDAY DEVICE THROUGH A SHIELDED DOME." (+5 more texts) |
| 04 | (0,6) (1,6) (0,7) (1,7) (0,8) (1,8) | L70C0 | "CABLES FILL THE ROOM. IT'S POSSIBLE, BUT DANGEROUS, TO CUT THE POWER TO THE DOOMSDAY DEVICE." (+4 more texts); fight: 5x Mer. Technician + Ram Mer Cgennie; yes/no prompt; skill check; explosion effect; damages party; sets [9E71]=1, [98C4]=1, [9805]=1 |
| 05 | (2,6) | L70BB | "POWER ROOM -- DANGER" |
| 06 | (5,14) | L7158 | "A HUGE SQUADRON OF GUARDS IS STATIONED AT THIS ELEVATOR. THEY DEMAND YOUR SURRENDER." (+12 more texts); fight: 10x Ram Mer Cgennie; menu: Surrender / Run / Attack; pushes party back; explosion effect; damages party; sets [97F9]=1, [98CA]=1 |
| 07 | (3,12) | L721C | "THIS IS THE TOP OF THE CORE. WILL YOU DESCEND?" (+1 more texts); yes/no prompt; -> module 61 |
| 08 | (3,11) (2,12) (4,12) (3,13) | L7237 | "A BABBLE OF INFORMATION ISSUES FROM AN INTERCOM." (+4 more texts); sets [98C8]=1 |
| 09 | (11,3) | L7268 | "POD 1" |
| 0A | (13,3) | L726D | "POD 2" |
| 0B | (10,3) | L7272 | "POD 3" |
| 0C | (12,3) | L7277 | "POD 4" |
| 0D | (13,5) | L7398 | "THE CONTROLS ARE LIT, BUT UNRESPONSIVE. AFTER A FEW MOMENTS, SCOT.DOS APPEARS ON ONE OF THE MONITORS." (+4 more texts); sets [97FC]=1 |
| 0E | (9,14) (11,14) (13,14) | L73D3 | "CONTROL TERMINALS LINE THE WALLS, MANNED BY RAM TECHNICIANS. GUARDS LUNGE TOWARDS YOU." (+2 more texts); fight: 3x Ram Assault Bot + 2x Ram Mer Cgennie + 4x Mer. Technician; sets [98C3]=1, [98C9]=1 |
| 0F | (9,5) (11,5) | L74CE | "YOU TAKE THE CONTROLS AND MANEUVER AS FAR AWAY FROM THE MARIPOSA AS POSSIBLE."; program 1; -> module 11; sets [9E08]=1 |
| 10 | (1,13) | L74F0 | "THIS IS THE RAM SECURITY OFFICE FOR THE TOP PORTION OF THE MARIPOSA." (+4 more texts); fight: 8x Ram Mer Cgennie; sets [97FD]=1 |
| 11 | (1,12) (0,13) (2,13) | L754C | "SECURITY OFFICE" |
| 12 | (6,8) | L7582 | "YOU USE YOUR RETINAL LOCKPICK TO PASS THE RETINAL IDENTIFICATION REQUIRED BY THIS DOOR."; sets [97FE]=1 |
| 13 | (13,4) | L7596 | "POD CONTROL CENTER" |
| 14 | (10,0) (1,2) (1,9) | (falls through) | step event: facing W: "EMERGENCY STAIRS." (+3 more texts); yes/no prompt; sets [97F9]=1, [9DBF]=255 |
| 15 | (12,2) (14,2) | (falls through) | step event: facing N: "A WARNING LIGHT BLINKS WHEN YOU TRY TO OPEN THE DOOR. A SIGN FLASHES --" (+8 more texts); yes/no prompt; item search; destroys item; skill check; explosion effect; program 1; -> module 11; sets [9DBF]=255, [9E71]=3, [9801]=1, [9802]=1, [9803]=1, [9804]=1, [9E08]=1 |
| 16 | (9,4) (11,4) | (falls through) | step event: facing S: "A SENSOR BEAM SWEEPS OVER YOUR TEAM AND THE DOORS SLIDE OPEN. 'AUTHORIZATION ACCEPTED.'" (+7 more texts); yes/no prompt; item search; destroys item; skill check; explosion effect; program 1; -> module 11; sets [9DBF]=255, [9E71]=3, [9801]=1, [9802]=1, [9803]=1, [9804]=1, [9E08... |
| 17 | (9,13) (11,13) (13,13) | (falls through) | step event: "WEAPONS CONTROL" (+2 more texts); sets [9DBF]=255 |

Handlers no cell can reach: none

Codes beyond the table (fall through): 14 15 16 17

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==20 | W | L6C25 | (10,0) (1,2) (1,9) | "EMERGENCY STAIRS." (+3 more texts); yes/no prompt; sets [97F9]=1, [9DBF]=255 |
| [9E6F]==21 | N | L727C | (12,2) (14,2) | "A WARNING LIGHT BLINKS WHEN YOU TRY TO OPEN THE DOOR. A SIGN FLASHES --" (+8 more texts); yes/no prompt; item search; destroys item; skill check; explosion effect; program 1; -> module 11; sets [9DBF]=255, [9E71]=3, [9801]=1, [9802]=1, [9803]=1, [9804]=1, [9E08]=1 |
| [9E6F]==22 | S | L72B7 | (9,4) (11,4) | "A SENSOR BEAM SWEEPS OVER YOUR TEAM AND THE DOORS SLIDE OPEN. 'AUTHORIZATION ACCEPTED.'" (+7 more texts); yes/no prompt; item search; destroys item; skill check; explosion effect; program 1; -> module 11; sets [9DBF]=255, [9E71]=3, [9801]=1, [9802]=1, [9803]=1, [9804]=1, [9E08]=1 |
| [9E6F]==23 |  | L7551 | (9,13) (11,13) (13,13) | "WEAPONS CONTROL" (+2 more texts); sets [9DBF]=255 |

## Module 63 / map 63 Enemy Ships

Search: `ONGOTO [9E6F]`, 12 targets, code 0 and codes >= 12 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (13,7) (8,10) (13,12) (2,14) | L6CC9 | "RAM GARRISON TROOPS ARE WAITING FOR YOUR RETURN. THEY ATTACK!" (+24 more texts); fight: 10x Ram Warrior; menu: Launch / Repair / Ammo / Exit; -> module 22; menu: Estimate / Repair / Done; menu: Hull / Cont / Engn / Weap / Done; menu: K-Cannon / Missile / Laser / Done; menu: K-Cannon / Missile / Don... |
| 02 | (13,6) (8,9) (13,13) (3,14) | L6CFD | "PORT FACILITIES.'" (+2 more texts) |
| 03 | (13,3) (10,7) (4,9) (11,13) | L6D43 | "GABE'S SALOON" (+15 more texts) |
| 04 | (14,3) (11,7) (5,9) (11,14) | L6D82 | "WHAT WOULD YOU LIKE TO DO?" (+32 more texts); fight: 5x Ram Warrior / or / 6x Ram Warrior / or / 6x Ram Warrior; menu: Mingle / Exit; random roll; random roll; random roll; yes/no prompt; random roll; menu: Fight / Leave; +1500 XP; random roll; pushes party back; random roll; random roll; treasure:... |
| 05 | (13,2) (8,5) (2,10) | L6D86 | "SUPPLIES.'" (+2 more texts) |
| 06 | (13,1) (8,4) (1,10) | L6DAF | "GOOD DAY."; store 3; store 5; store 9; store 10; store 11; store 12; store 13; store 14; store 15; pushes party back; sets [9E63]=64, [9EEC]=1, [9E63]=32 |
| 07 | (4,7) (7,7) (8,13) | L6DB3 | no event here (cell is a plain marker) |
| 08 | (2,8) (9,13) | L6DB4 | "TRAINING FACILITIES.'" (+2 more texts) |
| 09 | (1,8) (9,14) | L6DDA | "GOOD DAY."; program 0; pushes party back; sets [9D9E]=127, [9BCB]=96 |
| 0A | (3,7) | L6DDE | "LIBRARY.'" (+2 more texts) |
| 0B | (3,6) | L6DEF | "'YOU ARE CLEARED FOR LAUNCH.'" (+21 more texts); fight: 10x Ram Warrior / or / 10x Ram Warrior; -> module 22; yes/no prompt; skill check; NPC joins/appears; NPC joins/appears; +100 XP; pushes party back; sets [9E70]=1, [9E71]=2, [981C]=1 |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==1 |  | L79DC | (13,7) (8,10) (13,12) (2,14) | "GOOD DAY."; pushes party back |
