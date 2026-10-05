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
| 01 | (4,3)o (5,3)o (6,3)o (8,3)o (3,4)o (9,4)o (3,5)o (11,5)o ... | L6E0D | "THE HEAT FROM THE EXPLOSIONS DRIVES YOU BACK. YOU ARE VERY EXPOSED ..." |
| 02 | (7,10)o (8,10)o (7,11) (8,11) | L6E22 | ENCEXIT |
| 03 | (6,13) | L6E23 | "MEN'S ROOM" |
| 04 | (5,13) | L6E28 | "WOMEN'S ROOM" |
| 05 | (6,14) | L6E2D |  |
| 06 | (5,14) | L6E47 |  |
| 07 | (2,14) | L6E79 |  |
| 08 | (1,14) | L6EA8 |  |
| 09 | (1,13) | L6EDF |  |
| 0A | (2,10) | L6F0B | "LECTURE HALL" |
| 0B | (1,10) | L6F10 |  |
| 0C | (4,10)o | L6F3A |  |
| 0D | (11,10)o | L6F62 |  |
| 0E | (13,9) | L6FA2 | "AUTHORIZED PERSONNEL ONLY" |
| 0F | (14,9) | L6FA7 | "YOU BURST IN AMONGST A SQUAD OF TERRINES. IN THE BACKGROUND YOU CAN..." | Terrine Warrior | Terrine Leader | COMBAT |
| 10 | (15,10) | L6FD0 | "WITH ONE HAND, THE TECHNICIAN BEGINS FIRING AT YOU. HE IS STILL MOD..." | "IF YOU SHOOT AT HIM YOU MAY DAMAGE THE CONTROLS WHICH OPERATE THE B..." |
| 11 | (13,13) | L71BF | "MEDICAL OFFICER" |
| 12 | (13,14) | L71C4 | "THE ROOM IS FULL OF WOUNDED TROOPS. AN ORDERLY SHOUTS, 'WE'RE FULL ..." | "YOU HEAR AN EXPLOSION." | ENCEXIT |
| 13 | (10,13) | L7249 | "WAITING ROOM" |
| 14 | (10,14) | L724E |  |
| 15 | (7,15) (8,15) | L6E22 | ENCEXIT |
| 16 | (6,7) | L7284 |  |
| 17 | (10,5) | L729E |  |
| 18 | (10,7) (11,7) | L72B8 |  |
| 19 | (2,13) | L72D2 | "TO THE SOUTH IS THE OFFICER'S LOUNGE. " | "TO THE WEST IS THE CANTEEN." |
| 1A | (5,5)o (4,6)o (6,6)o (5,7)o | L72DC |  |
| 1B | (12,10) | L6E22 | ENCEXIT |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9AF9]==130 | N | L6BDE | (none) | "THE SECURITY DOORS ARE SEALED." |
| [9AF9]==2 | S | L6BDE | (7,10)o (8,10)o (7,11) (8,11) | "THE SECURITY DOORS ARE SEALED." |
| [9AF9]!=149 | S | L6BEA | (none) |  |
| [9AF9]==13 | E | L6C07 | (11,10)o | "THE DOOR HAS BEEN FUSED SHUT BY THE BATTLE." | ENCEXIT |
| [9E6F]==2 |  | L6D26 | (7,10)o (8,10)o (7,11) (8,11) | "TERRINES MOVE IN FOR THE KILL." | " NEO FORCES COME TO YOUR AID." | Terrine Warrior | Terrine Leader | Neo Warrior | COMBAT |
| [9E6F]==1 |  | L6D26 | (4,3)o (5,3)o (6,3)o (8,3)o (3,4)o (9,4)o (3,5)o (11,5)o ... | "TERRINES MOVE IN FOR THE KILL." | " NEO FORCES COME TO YOUR AID." | Terrine Warrior | Terrine Leader | Neo Warrior | COMBAT |
| [9E6F]==0 |  | L7162 | (0,0)o (1,0)o (2,0)o (3,0)o (4,0)o (5,0)o (6,0)o (7,0)o ... | "YOU ARE GIVEN A SMALL REWARD." | TREASURE #1000, 0 | COMBAT | -> module 17 |
| [9E6F]==2 |  | L7162 | (7,10)o (8,10)o (7,11) (8,11) | "YOU ARE GIVEN A SMALL REWARD." | TREASURE #1000, 0 | COMBAT | -> module 17 |

## Module 11 / map 11 

Search: `ONGOTO [9E70]`, 11 targets, code 0 and codes >= 11 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (2,2) | L6E27 |  |
| 02 | (1,2) | L6E34 | "'TAKE A TUG OUT AND EXPLORE THE DEBRIS IN THE EARTH'S NEAR ORBIT. I..." |
| 03 | (3,1) | L6F02 |  |
| 04 | (3,0) | L6F0F | "'COME AGAIN!'" | ENCEXIT |
| 05 | (6,1) | L6F25 |  |
| 06 | (6,0) | L6F32 | "SALVATION'S MEDICS RESTORE THE ENTIRE TEAM TO PERFECT HEALTH." |
| 07 | (9,1) | L6F70 |  |
| 08 | (9,0) | L6F7D | "YOU ENTER THE SALVATION LOUNGE." | "YOU ARE IN THE SALVATION LOUNGE." |
| 09 | (11,2) | L7110 | "THIS HATCHWAY LEADS TO THE ROCKETBAY." |
| 0A | (12,2) | L7120 | "YOU ARE IN THE SALVATION PORT AREA." | "LAUNCH ACCESS DENIED UNTIL YOU HAVE BEEN BRIEFED. REPORT TO HQ." |
| 0B | (6,3) | (falls through) |  |

Handlers no cell can reach: none

Codes beyond the table (fall through): 0B

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==0 |  | L6F68 | (0,0)o (1,0)o (2,0)o (4,0) (5,0)o (7,0) (8,0)o (10,0) ... | "SALVATION'S MEDICS RESTORE THE ENTIRE TEAM TO PERFECT HEALTH." | ENCEXIT |
| [9E6F]==2 |  | L6F68 | (1,2) | "SALVATION'S MEDICS RESTORE THE ENTIRE TEAM TO PERFECT HEALTH." | ENCEXIT |

## Module 20 / map 20 Spy Ship

Search: `ONGOTO [9E6F]`, 29 targets, code 0 and codes >= 29 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (14,5) (5,10) | L6B82 |  |
| 02 | (13,5) (4,10) | L7029 |  |
| 03 | (13,4) (3,8) | L704F |  |
| 04 | (12,4) (2,10) | L706D |  |
| 05 | (12,5) (0,10) | L708B |  |
| 06 | (12,6) (14,11) | L70A9 |  |
| 07 | (13,6) (13,10) | L70C7 |  |
| 08 | (12,7) (14,10) | L70EA |  |
| 09 | (13,7) (13,8) | L7108 |  |
| 0A | (15,3) (15,7) (14,8) | L700F |  |
| 0B | (7,0) (15,0) (5,8) (11,8) (15,8) (3,13) | L7131 |  |
| 0C | (0,1) (8,1) (0,9) (6,9) (12,9) | L7213 |  |
| 0D | (11,1) (3,14) | L74EE |  |
| 0E | (8,6) (2,13) | L764B |  |
| 0F | (9,7) (1,14) | L7690 |  |
| 10 | (11,7) (0,14) | L76CA |  |
| 11 | (7,3) (2,8) | L7724 | "THE ENGINES HAVE BEEN SHUT DOWN. TO RESTART REQUIRES ACCESS TO THE ..." |
| 12 | (7,6) (4,8) | L7489 | "THIS ROOM IS FILLED WITH INNUMERABLE PIECES OF EQUIPMENT AND SUPPLIES." | "NOTHING HERE SEEMS OF IMMEDIATE USE." | ENCEXIT |
| 13 | (6,0) (7,1) | L74C6 |  |
| 14 | (2,7) | L795D |  |
| 15 | (10,11) | L78C7 |  |
| 16 | (8,11) | L78F2 |  |
| 17 | (6,11) | L791D |  |
| 18 | (9,9) | L78AD |  |
| 19 | (7,8) | L795D |  |
| 1A | (6,12) | L7943 |  |
| 1B | (8,12) | L7903 |  |
| 1C | (10,12) | L78D8 |  |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]!=1 | E | L6C2D | (14,5) (5,10) |  |
| [9E6F]==0 |  | L6EF4 | (0,0) (1,0) (2,0) (3,0) (4,0) (5,0) (8,0) (9,0) ... |  |

## Module 23 / map 23 Asteroid Base

Search: `ONGOTO [9E6F]`, 11 targets, code 0 and codes >= 11 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (0,6) | L6C11 |  |
| 02 | (2,6) | L6C53 |  |
| 03 | (4,4) | L6C6D |  |
| 04 | (5,3) | L6CA8 |  |
| 05 | (2,8) | L6CC2 |  |
| 06 | (3,6) | L6CF2 |  |
| 07 | (6,6) | L6D16 |  |
| 08 | (7,8) | L6D53 |  |
| 09 | (9,8) | L6DA4 |  |
| 0A | (9,4) | L6DC2 | "YOU FIND A CACHE OF EXPLOSIVES AND SEVERAL EMPTY CYLINDERS MARKED, ..." | TREASURE 0, 8, 33, 33, 33, 33, 33, 33, 33, 33 | COMBAT |

Handlers no cell can reach: none

## Module 30 / map 30 Asteroid Base, Level 1

Search: `ONGOTO [9E70]`, 26 targets, code 0 and codes >= 26 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (5,0) | L6E8A | "YOU ARE IN THE AIRLOCK OF A RAM DOCKING BAY." | ENCEXIT |
| 02 | (5,2) | L6E90 |  |
| 03 | (2,3) | L6F4C |  |
| 04 | (2,2) | L6F8C |  |
| 05 | (0,8) | L6FBD |  |
| 06 | (0,11) | L6FD7 |  |
| 07 | (1,13) | L7021 |  |
| 08 | (2,15) | L706A | "YOU HAVE ENTERED THE ELEVATOR. " | "DO YOU WANT TO DESCEND?" | ENCEXIT |
| 09 | (4,14) (6,14) | L70B1 |  |
| 0A | (9,15) | L7178 |  |
| 0B | (14,11) (14,13) | L717C |  |
| 0C | (14,9) | L71AA |  |
| 0D | (8,11) | L71CC |  |
| 0E | (8,3) | L71E6 |  |
| 0F | (3,6) | L727D | "THIS IS A WORKING MEDICAL OFFICE." |
| 10 | (3,10) | L728D |  |
| 11 | (9,2) | L72B8 |  |
| 12 | (12,7) | L72F4 |  |
| 13 | (13,4) (14,4) (13,5) (14,5) | L7330 | "YOU HAVE ENTERED A POWER ROOM. VARIOUS DISPLAYS LINE THE WALLS." |
| 14 | (12,5) | L73A5 |  |
| 15 | (13,3) | L73BF |  |
| 16 | (13,0) | L73DF | "YOU HEAR THROUGH THE SWIFTLY CLOSING AIRLOCK, 'HOSTILES IN THE BAY!..." | ENCEXIT |
| 17 | (15,10) | L7416 |  |
| 18 | (8,8) | L7441 |  |
| 19 | (7,14) | L7460 |  |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9AF9]!=129 | N | L6BE4 | (none) |  |

## Module 31 / map 31 Asteroid Base, Level 2

Search: `ONGOTO [9E70]`, 31 targets, code 0 and codes >= 31 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (2,15) | L6D9C | "THE ELEVATOR IS SHATTERED AND THE SHAFT BLOCKED." | ENCEXIT |
| 02 | (5,14) | L6DAD |  |
| 03 | (5,9) | L6DDC |  |
| 04 | (6,14) | L6DE0 |  |
| 05 | (5,15) | L6E04 | "AN UNUSUAL LASER DOMINATES THE ROOM." |
| 06 | (7,9) (6,12) | L6E29 |  |
| 07 | (4,6) | L6E4D |  |
| 08 | (14,14) | L6E76 |  |
| 09 | (12,15) | L6EA0 |  |
| 0A | (9,15) | L6EBA | "THE ELEVATOR IS DAMAGED. YOU NEED A ROPE." |
| 0B | (12,12) | L6F38 |  |
| 0C | (14,11) | L6FCD |  |
| 0D | (3,1) (5,1) (8,1) | L6FF2 |  |
| 0E | (12,3) | L6FFF |  |
| 0F | (14,4) | L701C |  |
| 10 | (9,7) | L7036 |  |
| 11 | (12,6) | L705B |  |
| 12 | (9,4) | L7075 |  |
| 13 | (12,10) | L709A |  |
| 14 | (9,10) | L70DC |  |
| 15 | (9,11) | L70F6 |  |
| 16 | (4,10) | L7110 |  |
| 17 | (11,1) | L712A |  |
| 18 | (1,4) | L714F |  |
| 19 | (13,1) | L7169 |  |
| 1A | (1,1) | L7191 |  |
| 1B | (1,10) | L71AB |  |
| 1C | (5,12) | L71C5 |  |
| 1D | (3,0) (4,0) (5,0) (6,0) (7,0) (8,0) (9,0) | L7213 | "THE COMPUTER YIELDS NO NEW INFORMATION." | ENCEXIT |
| 1E | (8,9) | L71C5 |  |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]!=28 | E | L6B70 | (5,12) |  |
| [9E6F]==1 |  | L731F | (2,15) | "'GREETINGS MILO, YOU HAVE A MESSAGE.'" | "THE MESSAGE READS, 'NEXT PHASE IS OPERATIONAL AT GRADIVUS MONS.' TH..." |

## Module 32 / map 32 Pirate Ship, Levels 1-5

Search: `ONGOTO [9E6F]`, 17 targets, code 0 and codes >= 17 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,5) (2,6) (3,6) (4,6) | L7173 | "THIS IS A SMALL HOLDING CELL. " |
| 02 | (5,5) | L728E | "'I HID SOME EQUIPMENT HERE.'" | TREASURE 0, 5, 25, 25, 26, 35, 35 | COMBAT |
| 03 | (3,3) (10,3) (3,10) (10,10) | L72C5 | "YOU'RE ON A STAIRCASE. A SIGN READS: LEVEL " | "." |
| 04 | (3,1) | L743D |  |
| 05 | (1,1) | L7516 |  |
| 06 | (0,2) (6,2) (0,3) (6,3) (0,4) (6,4) | L7535 | "YOU'VE ENTERED A CABIN. " |
| 07 | (10,5) | L7577 |  |
| 08 | (12,4) | L75A4 |  |
| 09 | (10,1) | L7612 |  |
| 0A | (8,3) | L76C4 |  |
| 0B | (6,9) (6,10) | L7792 |  |
| 0C | (2,7) (3,7) (4,7) | L77CD |  |
| 0D | (1,8) (1,9) | L7803 |  |
| 0E | (9,7) (10,7) (11,7) (7,9) (13,9) (7,10) (13,10) (7,11) ... | L785B |  |
| 0F | (3,11) | L7172 |  |
| 10 | (10,11) | L7172 |  |
| 11 | (13,2) | (falls through) |  |
| 12 | (10,2) | (falls through) |  |
| 13 | (9,3) | (falls through) |  |

Handlers no cell can reach: none

Codes beyond the table (fall through): 11 12 13

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==17 | E | L6EA1 | (13,2) | "THIS AIRLOCK HAS BEEN SEALED FOR SPACE TRAVEL. IT CAN ONLY BE OPENE..." | ENCEXIT |
| [9E6F]==18 | N | L78CF | (10,2) |  |
| [9E6F]==19 | W | L79D7 | (9,3) |  |
| [9E6F]==3 |  | L6EEF | (3,3) (10,3) (3,10) (10,10) |  |
| [9E6F]==3 |  | L6FFC | (3,3) (10,3) (3,10) (10,10) |  |

## Module 34 / map 34 Pirate Ship, Levels 11-15

Search: `ONGOTO [9E6F]`, 10 targets, code 0 and codes >= 10 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,0) (4,2) (0,5) (6,5) (1,8) (8,8) (1,11) (6,14) ... | L6DF6 |  |
| 02 | (3,0) (7,1) (12,2) (3,4) (7,6) (3,8) (12,8) (7,11) ... | L6E44 | "YOU ARE AT A LADDER. WHERE DO YOU GO?" |
| 03 | (15,0) (15,4) (4,15) (8,15) | L6DF5 |  |
| 04 | (2,3) (1,15) | L6FAB | "THESE CONTROLS ARE DAMAGED." |
| 05 | (11,0) (12,0) (5,10) (12,11) (13,11) | L7011 |  |
| 06 | (11,14) | L70FD |  |
| 07 | (10,13) | L7145 |  |
| 08 | (6,6) | L718D |  |
| 09 | (5,6) | L71A0 |  |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]!=1 |  | L6CF2 | (1,0) (4,2) (0,5) (6,5) (1,8) (8,8) (1,11) (6,14) ... |  |

## Module 41 / map 41 Desert Runner Village

Search: `ONGOTO [9E6F]`, 12 targets, code 0 and codes >= 12 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,7)o (0,8)o | L6D5B |  |
| 02 | (8,7)o (8,8)o | L6E47 |  |
| 03 | (5,7)o (3,8)o | L6ECA |  |
| 04 | (0,7)o | L6B95 |  |
| 05 | (9,7) (9,8) | L6ED7 |  |
| 06 | (9,5)o (10,5)o (12,7)o (12,8)o (9,10)o (10,10)o | L7094 |  |
| 07 | (10,1) (12,3) (7,4) | L70F1 |  |
| 08 | (1,3) (2,5) (4,5) | L7167 |  |
| 09 | (15,3) (14,7) (14,10) (14,11) | L7217 |  |
| 0A | (6,13) (11,13) (9,14) (14,15) | L7246 |  |
| 0B | (1,10) (5,10) (1,14) | L726E |  |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==0 |  | L72A5 | (0,0) (1,0) (2,0)o (3,0)o (4,0) (5,0)o (6,0)o (7,0)o ... |  |
| [9E6F]==2 |  | L72A5 | (8,7)o (8,8)o |  |

## Module 42 / map 42 Mars Base Gradiuvs Mons

Search: `ONGOTO [9E6F]`, 23 targets, code 0 and codes >= 23 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (2,12) | L6E77 |  |
| 02 | (3,11) | L6E9F |  |
| 03 | (1,9) | L6FFD |  |
| 04 | (3,9) | L709D |  |
| 05 | (7,12) | L70DD |  |
| 06 | (7,1) (8,13) (2,14) | L7115 | "YOU FIND AN INTERCOM STATION. USE THE INTERCOM?" |
| 07 | (0,0) (7,14) | L71DC | "A METAL LADDER LEADS DOWNWARD. CLIMB DOWN?" |
| 08 | (5,3) (7,3) (5,5) (7,5) (10,13) (11,13) (12,13) (10,15) ... | L725C | "A SMALL OFFICE. " |
| 09 | (4,6) | L72AB |  |
| 0A | (6,7) (15,15) | L72EC | "A COMPLEX SECURITY SYSTEM PREVENTS YOU FROM USING THIS ELEVATOR." | ENCEXIT |
| 0B | (10,4) | L737A |  |
| 0C | (10,3) | L73AC |  |
| 0D | (12,11) | L7415 |  |
| 0E | (12,10) | L7434 | "THESE ARE THE CONTROLS FOR THE MINIATURE LASER. " | "THEY ARE NONFUNCTIONAL." | ENCEXIT |
| 0F | (8,6) | L7462 |  |
| 10 | (4,11) | L6E75 |  |
| 11 | (3,12) | L6E75 |  |
| 12 | (1,12) | L6E75 |  |
| 13 | (1,11) | L6E75 |  |
| 14 | (11,3) (11,4) (11,5) | L6E75 |  |
| 15 | (0,15) | L6E75 |  |
| 16 | (15,10) | L750D |  |
| 17 | (13,14) | (falls through) |  |

Handlers no cell can reach: none

Codes beyond the table (fall through): 17

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==16 | E | L6C23 | (4,11) | "CAGE 1." |
| [9E6F]==17 | E | L6C4F | (3,12) | "CAGE 2." |
| [9E6F]==18 | W | L6C7B | (1,12) | "CAGE 3." |
| [9E6F]==19 | W | L6CA7 | (1,11) | "CAGE 4." |
| [9E6F]==20 | E | L6D3A | (11,3) (11,4) (11,5) | "THE MASSIVE FRONT DOORS ARE SEALED." | ENCEXIT |
| [9E6F]==21 | S | L6C17 | (0,15) | "THIS DOOR WAS FUSED BY A LASER BOLT. YOU CAN'T LEAVE THIS WAY." | ENCEXIT |
| [9E6F]==23 | W | L6D8B | (13,14) |  |
| [9E6F]==0 |  | L74FF | (1,0) (2,0)o (3,0)o (4,0)o (5,0)o (6,0)o (7,0)o (8,0)o ... | ENCEXIT |
| [9E6F]==2 |  | L74FF | (3,11) | ENCEXIT |

## Module 43 / map 43 More Asteroid Bases

Search: `ONGOTO [9E6F]`, 51 targets, code 0 and codes >= 51 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (4,15) | L6D9C |  |
| 02 | (6,13) | L6DD4 |  |
| 03 | (5,13) | L6E07 |  |
| 04 | (1,12) | L6E56 |  |
| 05 | (1,9) | L6E8B |  |
| 06 | (7,9) | L6EC0 |  |
| 07 | (6,8) | L6EE8 |  |
| 08 | (12,15) | L6F00 |  |
| 09 | (12,13) | L6F3D |  |
| 0A | (10,13) | L6F67 |  |
| 0B | (10,14) | L6F93 |  |
| 0C | (8,15) | L6FAD |  |
| 0D | (8,12) | L6FC7 |  |
| 0E | (9,9) | L6FE1 |  |
| 0F | (9,8) | L701B |  |
| 10 | (9,10) | L706E |  |
| 11 | (11,8) | L70C1 |  |
| 12 | (11,10) | L70E0 |  |
| 13 | (14,10) | L7105 |  |
| 14 | (14,13) | L712A |  |
| 15 | (13,14) | L7175 |  |
| 16 | (12,0) | L71AE |  |
| 17 | (12,1) | L71CB |  |
| 18 | (12,4) | L71F0 |  |
| 19 | (10,4) | L7227 |  |
| 1A | (9,2) | L7241 |  |
| 1B | (9,7) | L725B |  |
| 1C | (11,6) | L7288 |  |
| 1D | (12,7) | L72AD |  |
| 1E | (12,6) | L72C5 |  |
| 1F | (13,6) | L72DF |  |
| 20 | (15,4) | L733F | "AN AUTOMATIC DEFENSE SYSTEM LOCKS ONTO YOU AND FIRES." | "THE WEAPONS DRIVE YOU BACK." | ENCEXIT |
| 21 | (14,0) (14,1) (14,2) (14,3) (14,4) | L736A | "YOU FIND JASON DUPARE. HE IS EXTREMELY EMACIATED. 'RAM IS BUILDING ..." | "HE FAINTS AND YOU CARRY HIM." | ENCEXIT |
| 22 | (0,7) | L73A6 |  |
| 23 | (3,8) (4,8) | L73F2 |  |
| 24 | (4,11) | L7421 | "SHRIEKING DESERT APES THROW HANDFULS OF DIRT AND PEBBLES AT THE TEA..." |
| 25 | (2,11) | L7467 |  |
| 26 | (7,6) | L74AB |  |
| 27 | (7,3) | L74CE |  |
| 28 | (6,2) | L7501 |  |
| 29 | (5,2) | L7520 |  |
| 2A | (4,1) | L7543 |  |
| 2B | (6,0) | L755D |  |
| 2C | (2,2) | L75A2 |  |
| 2D | (0,6) | L75D5 |  |
| 2E | (3,14) | L761F |  |
| 2F | (5,12) | L7647 |  |
| 30 | (0,14) | L767F |  |
| 31 | (6,10) | L76B2 |  |
| 32 | (2,8) | L76F6 |  |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]!=1 | S | L6C44 | (4,15) |  |
| [9E6F]!=8 | S | L6C58 | (12,15) |  |
| [9E6F]!=22 | N | L6C6C | (12,0) |  |

## Module 51 / map 51 Lowlander Village, Venusian Space Elevator Ruins

Search: `ONGOTO [9E6F]`, 14 targets, code 0 and codes >= 14 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,1) (3,15) | L6E7D | "THIS IS A SUPPLY SHED. " |
| 02 | (13,4) (3,5) | L6F1C |  |
| 03 | (9,1) | L74B2 |  |
| 04 | (3,1) | L74B2 |  |
| 05 | (2,2) (6,9) | L6FE8 |  |
| 06 | (3,2) (10,11) | L713C | "YOU FIND THE BODIES OF A SLAIN FAMILY. A LOWLANDER BURSTS OUT AND A..." | "YOU IDENTIFY YOURSELVES AS NEO AGENTS. HE LOWERS HIS RIFLE. 'I HHAV..." |
| 07 | (0,0) (14,2)o | L71A9 |  |
| 08 | (1,6) (10,14) | L7263 |  |
| 09 | (3,0) (3,12) | L73BC |  |
| 0A | (9,0) (5,11)o | L73E6 |  |
| 0B | (2,5) (2,7) (3,7) (11,7) (14,7) (2,14) (3,14) | L740A |  |
| 0C | (2,6) (12,6)o | L7490 |  |
| 0D | (12,12)o (8,15)o | L6C5E | "YOU'RE AT THE MAIN GATE. " | "YOU HEAR A BABY CRYING. " | -> module 80 |
| 0E | (7,14)o | (falls through) |  |
| 0F | (5,1) (6,10)o | (falls through) |  |
| 10 | (9,3) (8,11)o | (falls through) |  |
| 11 | (2,12) | (falls through) |  |
| 13 | (5,2) | (falls through) |  |
| 14 | (8,3) | (falls through) |  |

Handlers no cell can reach: none

Codes beyond the table (fall through): 0E 0F 10 11 13 14

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==14 | W | L6DBB | (7,14)o | "THIS IS AN ACID FROG STOCKADE. THE FROGS HAVE BEEN MADDENED BY THE ..." | "WILL YOU FREE THE ACID FROGS?" | ENCEXIT |
| [9E6F]==15 | N | L6FB1 | (5,1) (6,10)o |  |
| [9E6F]==16 | E | L7129 | (9,3) (8,11)o |  |
| [9E6F]==17 | E | L735C | (2,12) | "THIS MEDICAL SUPPLY CLOSET IS LOCKED WITH LOWLANDER TECHNOLOGY YOU ..." |

## Module 52 / map 52 Venus RAM Base

Search: `ONGOTO [9E6F]`, 21 targets, code 0 and codes >= 21 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (7,0) (15,0) (7,8) (15,8) | L6F15 | "THERE ARE FOUR BUTTONS IN THE ELEVATOR." | "A RECORDED VOICE SAYS, 'IF YOU DO NOT WISH TO USE THE ELEVATOR, PLE..." |
| 02 | (12,9) | L705E |  |
| 03 | (12,8) | L708F | "ACIDICIUM OOZE TOWARDS YOU IN A SLIMY MASS." | Acidicium | COMBAT | ENCEXIT |
| 04 | (12,10) | L70BD | "GUARD ROBOTS ZERO IN ON YOU. " | Ram H.S. Robot | COMBAT | ENCEXIT |
| 05 | (0,9) | L715B |  |
| 06 | (15,12) | L71C5 |  |
| 07 | (1,2) (10,4) | L73BE |  |
| 08 | (11,0) (12,0) (8,2) (12,2) (13,2) (14,2) (15,2) (8,3) ... | L74C9 |  |
| 09 | (2,10) (3,10) (4,10) (5,10) (0,11) (2,11) (3,11) (4,11) ... | L7516 |  |
| 0A | (2,14) | L7562 |  |
| 0B | (5,4) | L7687 |  |
| 0C | (2,0) | L7694 | "LAB 2 " |
| 0D | (2,7) | L7699 | "LAB 3 " |
| 0E | (1,0) (4,4) (1,7) | L769E | "BROKEN BEAKERS AND BITS OF EQUIPMENT LITTER THE FLOOR." | ENCEXIT |
| 0F | (3,14) | L76A4 | "COMPUTER ROOM 1 " |
| 10 | (4,14) | L76A9 | "COMPUTER ROOM 2 " |
| 11 | (9,13) | L76AE |  |
| 12 | (8,15) | L76E1 | "AN ACID-FILLED TUNNEL LEADS TO THE JUNGLE. THE ACID IS SO STRONG TH..." | ENCEXIT |
| 13 | (0,15) | L7644 |  |
| 14 | (7,1) (15,1) (7,9) (15,9) | L7029 | "'FLOOR 1 -- LABS.' " |
| 16 | (13,4) (7,7) | (falls through) |  |
| 17 | (0,4) | (falls through) |  |
| 18 | (15,7) | (falls through) |  |
| 19 | (11,8) | (falls through) |  |
| 1A | (6,4) | (falls through) |  |

Handlers no cell can reach: none

Codes beyond the table (fall through): 16 17 18 19 1A

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==22 |  | L70EB | (13,4) (7,7) |  |
| [9E6F]==23 | W | L713A | (0,4) | "THIS IS THE MAIN EXIT FROM THE BASE, LEADING UP TO THE VENUSIAN JUN..." | -> module 80 |
| [9E6F]==24 | E | L7147 | (15,7) | "BEYOND THIS DOOR LIES A DARK TUNNEL DRIPPING WITH ACID. ENTER THE T..." | -> module 83 |
| [9E6F]==25 | W | L73B2 | (11,8) | "EXAMINING THE WALL CLOSELY, YOU FIND A CONCEALED LATCH. THIS MUST B..." | ENCEXIT |
| [9E6F]==26 |  | L76E7 | (6,4) |  |

## Module 53 / map 51 Lowlander Village, Venusian Space Elevator Ruins

Search: `ONGOTO [9E6F]`, 11 targets, code 0 and codes >= 11 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (1,1) (3,15) | L6E97 | "MUTATED GENNIES SWARM UP A LADDER! WITH HISSES AND HOWLS OF GLEE, T..." | Ursadder | COMBAT | ENCEXIT |
| 02 | (13,4) (3,5) | L6ECF | "THIS COMPUTER ROOM IS OCCUPIED BY A LONE RAM TECHNICIAN. HE PROMISE..." | "VENUSIAN SLUGS LEAVE TRAILS OF ACID OVER THE ABANDONED COMPUTER TER..." |
| 03 | (9,1) | L6FDA |  |
| 04 | (3,1) | L6FF9 |  |
| 05 | (2,2) (6,9) | L70DD |  |
| 06 | (3,2) (10,11) | L714A |  |
| 07 | (0,0) (14,2)o | L6C7F | "FROM HERE YOU CAN CLIMB UP TO THE JUNGLE. LEAVE THE RUINS?" | ENCEXIT |
| 08 | (1,6) (10,14) | L6C89 | "A LADDER LEADS TO A DEVELOPED AREA UNDERGROUND. GO DOWN?" | ENCEXIT |
| 09 | (3,0) (3,12) | L6CC4 | "A LADDER LEADS UP INTO DARK RUINS. CLIMB UP?" | ENCEXIT |
| 0A | (9,0) (5,11)o | L6CFF |  |
| 0B | (2,5) (2,7) (3,7) (11,7) (14,7) (2,14) (3,14) | (falls through) |  |
| 0C | (2,6) (12,6)o | (falls through) |  |
| 0D | (12,12)o (8,15)o | (falls through) |  |
| 0E | (7,14)o | (falls through) |  |
| 0F | (5,1) (6,10)o | (falls through) |  |
| 10 | (9,3) (8,11)o | (falls through) |  |
| 11 | (2,12) | (falls through) |  |
| 13 | (5,2) | (falls through) |  |
| 14 | (8,3) | (falls through) |  |

Handlers no cell can reach: none

Codes beyond the table (fall through): 0B 0C 0D 0E 0F 10 11 13 14

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==11 | S | L6D11 | (2,5) (2,7) (3,7) (11,7) (14,7) (2,14) (3,14) | "THIS IS NO WAY TO GET PAST THIS SECURITY DOOR WITHOUT A SECURITY CARD." |
| [9E6F]==12 | N | L6D44 | (2,6) (12,6)o | "YOU FACE AN ENORMOUS SECURITY DOOR. IT'S IMPOSSIBLE TO OPEN IT FROM..." |
| [9E6F]==19 |  | L70AA | (5,2) |  |
| [9E6F]==20 |  | L70AA | (8,3) |  |
| [9E6F]==21 |  | L70C9 | (none) |  |

## Module 5F / map 63 Enemy Ships

No search dispatch (no `AND [9AF9],63` + `ONGOTO`): this map has no cell-event table. 

## Module 60 / map 60 Mercury Merchants Area

Search: `ONGOTO [9E6F]`, 42 targets, code 0 and codes >= 42 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (0,14) | L72E6 | ENCEXIT |
| 02 | (0,11) | L72E6 | ENCEXIT |
| 03 | (1,12) | L6CD8 | "A SENSOR BEAM SCANS YOUR STOLEN BADGES. A MECHANICAL VOICE SAYS, 'P..." | ENCEXIT |
| 04 | (2,12) | L6CF2 |  |
| 05 | (1,9) (7,9) (4,15) | L6D23 |  |
| 06 | (0,4) (5,7) | L6D92 |  |
| 07 | (3,3) | L6DAF |  |
| 08 | (4,2) | L6E80 |  |
| 09 | (4,7) | L6EA9 | "THROUGH THE DUST ON THE DOOR YOU MAKE OUT, 'TO RUE DE S.'" | ENCEXIT |
| 0A | (8,4) | L6EAF |  |
| 0B | (8,3) | L72E6 | ENCEXIT |
| 0C | (6,8) (8,11) | L6F7E |  |
| 0D | (7,8) | L6F89 |  |
| 0E | (9,11) | L6FA1 |  |
| 0F | (10,13) | L6FD4 | "THE SIGN ABOVE THE DOOR READS " | "      'MERCURIAN STOCK EXCHANGE'" | ENCEXIT |
| 10 | (10,14) | L7030 | "THE COMPUTERS IN THE ROOM HAVE BEEN PROTECTED BY STEEL PLATES." |
| 11 | (6,6) | L7039 |  |
| 12 | (4,9) (3,11) | L7053 | "MAINTENANCE ROBOTS ARE LINED UP HERE." | ENCEXIT |
| 13 | (5,14) | L7059 |  |
| 14 | (3,15) | L705D |  |
| 15 | (3,14) | L7068 |  |
| 16 | (5,10) | L7082 |  |
| 17 | (5,11) | L708D |  |
| 18 | (8,10) | L70A7 |  |
| 19 | (7,10) | L70B2 |  |
| 1A | (0,7) | L70CC |  |
| 1B | (1,7) | L70D7 |  |
| 1C | (5,4) | L70F1 |  |
| 1D | (11,1) | L710B |  |
| 1E | (11,6) | L7125 |  |
| 1F | (10,10) | L713F |  |
| 20 | (7,7) | L7163 |  |
| 21 | (8,5) (8,8) (11,12) | L72E6 | ENCEXIT |
| 22 | (7,6) | L7178 |  |
| 23 | (13,7) | L718D |  |
| 24 | (10,1) | L72E6 | ENCEXIT |
| 25 | (10,6) | L72E6 | ENCEXIT |
| 26 | (15,13) (15,14) (15,15) | L72E6 | ENCEXIT |
| 27 | (14,13) (14,14) (14,15) | L71A7 |  |
| 28 | (14,0) | L72E6 | ENCEXIT |
| 29 | (0,0) (1,0) (2,0) (0,1) (1,1) (2,1) (3,1) (0,2) ... | L71E8 |  |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==38 | E | L6ED9 | (15,13) (15,14) (15,15) | "SECURITY IS DOING A PAT SEARCH ON EVERYONE TRYING TO PASS THESE GAT..." | "THE LASERS SWIVEL TOWARD YOU. 'SURRENDER OR DIE!' BLARES A SPEAKER. " |
| [9E6F]==39 | E | L6EC9 | (14,13) (14,14) (14,15) |  |
| [9E6F]==40 | S | L6C10 | (14,0) | "YOU ARE ABOUT TO ENTER THE CORE. DO YOU CONTINUE?" | ENCEXIT |
| [9E6F]==1 | W | L6BF9 | (0,14) | "DO YOU WISH TO RETURN TO YOUR SHIP?" | -> module 34 |
| [9E6F]==2 | W | L6BED | (0,11) | "THE ENTRANCE TO THE HANGAR HAS BEEN SEALED DUE TO A SECURITY ALERT...." | ENCEXIT |
| [9E6F]==4 | W | L6BE1 | (2,12) | "A MECHANICAL VOICE STATES, 'NO ENTRY WITHOUT VALID PASSCARD.'" | ENCEXIT |

## Module 61 / map 61 Mariposa Core

Search: `ONGOTO [9E6F]`, 19 targets, code 0 and codes >= 19 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (3,3) (10,3) (3,10) (10,10) (14,14) (15,14) (14,15) (15,15) | L6C1F | "THIS CORE RUNS THE LENGTH OF THE MARIPOSA. YOU CAN GO UPWARD OR DOW..." | "YOU MAY ALSO LEAVE THE CORE FROM HERE." |
| 02 | (14,2) | L6F02 | "YOU ENTER THE AUDIENCE CHAMBER. THE SUN KING SITS BEFORE YOU ON A G..." | "'PARLEZ-VOUS FRANCAIS?' THE SUN KING DEMANDS." |
| 03 | (10,2) (9,3) (11,3) (10,4) | L70DA |  |
| 04 | (3,9) (2,10) (4,10) (3,11) | L70C6 |  |
| 05 | (3,2) (2,3) (4,3) (3,4) | L70EE |  |
| 06 | (8,12) | L7116 |  |
| 07 | (11,5) (12,5) | L7272 |  |
| 08 | (5,13) | L723C | "DOODADS AND KNICKKNACKS FILL THIS STORAGE AREA. PAPER FLAGS AND OLD..." |
| 09 | (0,5) | L7174 |  |
| 0A | (2,14) | L72F5 |  |
| 0B | (14,12) | L7350 |  |
| 0C | (4,15) | L7303 |  |
| 0D | (12,14) | L7311 |  |
| 0E | (10,9) (9,10) (11,10) (10,11) | L7102 |  |
| 0F | (3,5) (4,5) | L735E |  |
| 10 | (5,0) | L735F | "WEAPONS CONTROL" | "AUTHORIZED PERSONNEL ONLY" |
| 11 | (8,1) | L73AC | "THIS ROOM IS FILLED WITH MACHINERY AND GIGANTIC GEARS. AN INFORMATI..." | ENCEXIT |
| 12 | (14,5) | L73B2 |  |
| 13 | (14,3) | (falls through) |  |

Handlers no cell can reach: none

Codes beyond the table (fall through): 13

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==19 | N | L7026 | (14,3) | "THE DOOR IS IMPENETRABLY LOCKED. GUARDS JEER AND TOSS GARBAGE THROU..." | ENCEXIT |

## Module 62 / map 62 Mercurian Finale, Weapons Control Level

Search: `ONGOTO [9E6F]`, 20 targets, code 0 and codes >= 20 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (9,0) (0,2) (0,9) | L6E17 | "YOU'RE AT THE BOTTOM OF AN EMERGENCY STAIRWELL. CLIMB THE STAIRS?" |
| 02 | (15,6) (6,15) | L6F6C | "THIS ELEVATOR HAS FOUR BUTTONS ON A HIGH-TECH PANEL." | "PRESS WHICH BUTTON?" | ENCEXIT |
| 03 | (11,11) | L7080 | "YOU SEE THE DOOMSDAY DEVICE THROUGH A SHIELDED DOME." | "'TARGETING -- EARTH'" | ENCEXIT |
| 04 | (0,6) (1,6) (0,7) (1,7) (0,8) (1,8) | L70C0 | "CABLES FILL THE ROOM. IT'S POSSIBLE, BUT DANGEROUS, TO CUT THE POWE..." | "THE POWER COUPLER IS HIGH ABOVE YOU. TRY TO TURN OFF THE POWER?" |
| 05 | (2,6) | L70BB | "POWER ROOM -- DANGER" |
| 06 | (5,14) | L7158 |  |
| 07 | (3,12) | L721C | "THIS IS THE TOP OF THE CORE. WILL YOU DESCEND?" |
| 08 | (3,11) (2,12) (4,12) (3,13) | L7237 |  |
| 09 | (11,3) | L7268 | "POD 1" |
| 0A | (13,3) | L726D | "POD 2" |
| 0B | (10,3) | L7272 | "POD 3" |
| 0C | (12,3) | L7277 | "POD 4" |
| 0D | (13,5) | L7398 |  |
| 0E | (9,14) (11,14) (13,14) | L73D3 |  |
| 0F | (9,5) (11,5) | L74CE | "YOU TAKE THE CONTROLS AND MANEUVER AS FAR AWAY FROM THE MARIPOSA AS..." | -> module 17 |
| 10 | (1,13) | L74F0 |  |
| 11 | (1,12) (0,13) (2,13) | L754C | "SECURITY OFFICE" |
| 12 | (6,8) | L7582 |  |
| 13 | (13,4) | L7596 | "POD CONTROL CENTER" |
| 14 | (10,0) (1,2) (1,9) | (falls through) |  |
| 15 | (12,2) (14,2) | (falls through) |  |
| 16 | (9,4) (11,4) | (falls through) |  |
| 17 | (9,13) (11,13) (13,13) | (falls through) |  |

Handlers no cell can reach: none

Codes beyond the table (fall through): 14 15 16 17

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==20 | W | L6C25 | (10,0) (1,2) (1,9) | "EMERGENCY STAIRS. " | "ALARM WILL SOUND." |
| [9E6F]==21 | N | L727C | (12,2) (14,2) | "A WARNING LIGHT BLINKS WHEN YOU TRY TO OPEN THE DOOR. A SIGN FLASHE..." | "POD HANGAR EMPTY " |
| [9E6F]==22 | S | L72B7 | (9,4) (11,4) | "A SENSOR BEAM SWEEPS OVER YOUR TEAM AND THE DOORS SLIDE OPEN. 'AUTH..." | ENCEXIT |
| [9E6F]==23 |  | L7551 | (9,13) (11,13) (13,13) |  |

## Module 63 / map 63 Enemy Ships

Search: `ONGOTO [9E6F]`, 12 targets, code 0 and codes >= 12 fall through.

| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |
|---|---|---|---|
| 01 | (13,7) (8,10) (13,12) (2,14) | L6CC9 | "RAM GARRISON TROOPS ARE WAITING FOR YOUR RETURN. THEY ATTACK!" | Ram Warrior | COMBAT |
| 02 | (13,6) (8,9) (13,13) (3,14) | L6CFD |  |
| 03 | (13,3) (10,7) (4,9) (11,13) | L6D43 |  |
| 04 | (14,3) (11,7) (5,9) (11,14) | L6D82 |  |
| 05 | (13,2) (8,5) (2,10) | L6D86 |  |
| 06 | (13,1) (8,4) (1,10) | L6DAF |  |
| 07 | (4,7) (7,7) (8,13) | L6DB3 |  |
| 08 | (2,8) (9,13) | L6DB4 |  |
| 09 | (1,8) (9,14) | L6DDA |  |
| 0A | (3,7) | L6DDE |  |
| 0B | (3,6) | L6DEF |  |

Handlers no cell can reach: none

Step events (run entry and elsewhere):

| tests | facing | handler | cells on the map | summary |
|---|---|---|---|---|
| [9E6F]==1 |  | L79DC | (13,7) (8,10) (13,12) (2,14) | "GOOD DAY." |
