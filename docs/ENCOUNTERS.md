# Scripted fights

Every fight started by a script (`COMBAT` after `LOADMONSTER` blocks), taken from a disassembly of the ECL script modules. The game's own text for each fight is left out; the area names are the module numbers and the places they belong to.

- **Count** `[9Exx]` means the number is set at run time (random, or by an earlier choice). `x0` is a template that the random-encounter code fills in.
- **Surprise** means the same script block saves to `[9DC1]` before the fight (1 party surprised, 2 enemies surprised; see [SCRIPT-VARIABLES.md](SCRIPT-VARIABLES.md)).
- To change a fight, edit the `LOADMONSTER` id or count, or insert `SAVE 1,[9DC1]` before `COMBAT` for an ambush, with the ECL patch tool in `kit/tools/ecl_patch.py`. See [BUILDING.md](BUILDING.md).

135 fights.

| Area | Script addr | Monsters | Surprise |
|---|---|---|---|
| 01 intro | 6C38 | ? x[9E71], ? x[9E74], ? x[9E77] |  |
| 03 opening lore | 6BB3 | Terrine Warrior x4, Terrine Leader x1 |  |
| 10 Chicagorg (opening) | 6D4E | Terrine Warrior x[9E70], Terrine Leader x1, Neo Warrior x[9E6F] |  |
| 10 Chicagorg (opening) | 6E70 | Terrine Warrior x1 | party surprised |
| 10 Chicagorg (opening) | 6F02 | Terrine Warrior x3 |  |
| 10 Chicagorg (opening) | 6F8D | Terrine Warrior x4, Terrine Leader x1 |  |
| 10 Chicagorg (opening) | 6FB6 | Terrine Warrior x6, Terrine Leader x1 |  |
| 20 RAM spy ship | 6E01 | Sm. E.C. Gennie x0 |  |
| 20 RAM spy ship | 6FB2 | Sm. E.C. Gennie x6 |  |
| 20 RAM spy ship | 6FD4 | Ram L.D. Robot x3 |  |
| 20 RAM spy ship | 7000 | Ram H.D. Robot x2 |  |
| 20 RAM spy ship | 73CC | Sm. E.C. Gennie x3, Lg. E.C. Gennie x1 |  |
| 20 RAM spy ship | 76C1 | Lg. E.C. Gennie x1 |  |
| 20 RAM spy ship | 7856 | Lg. E.C. Gennie x8, Sm. E.C. Gennie x4 |  |
| 21 spy ship (2) | 6D68 | Sm. E.C. Gennie x0 |  |
| 21 spy ship (2) | 6FC2 | Lg. E.C. Gennie x1, Sm. E.C. Gennie x4 |  |
| 21 spy ship (2) | 7088 | Lg. E.C. Gennie x1 |  |
| 21 spy ship (2) | 72CF | Ram H.D. Robot x2 |  |
| 21 spy ship (2) | 74A5 | Ram H.D. Robot x6 |  |
| 21 spy ship (2) | 7506 | Ram H.D. Robot x6 |  |
| 21 spy ship (2) | 760E | Stage 3  Ecg x2 |  |
| 23 asteroid base | 6BF2 | Sm. E.C. Gennie x3, Lg. E.C. Gennie x2 |  |
| 23 asteroid base | 6C3A | Lg. E.C. Gennie x2, Stage 3  Ecg x2 |  |
| 23 asteroid base | 6C98 | Sm. E.C. Gennie x4, Lg. E.C. Gennie x1 |  |
| 23 asteroid base | 6D3C | Sm. E.C. Gennie x3, Lg. E.C. Gennie x1, Stage 3  Ecg x1 |  |
| 23 asteroid base | 6D7F | Sm. E.C. Gennie x3, Lg. E.C. Gennie x1, Stage 3  Ecg x1 |  |
| 30 evacuation, Milo | 6E40 | Space Rat x[9E6F] | party surprised |
| 30 evacuation, Milo | 6F1B | Ram L.S. Robot x2, Ram Warrior x2 |  |
| 30 evacuation, Milo | 72E6 | Ram Warrior x4 |  |
| 30 evacuation, Milo | 7322 | Ram Warrior x4 |  |
| 31 killer gennies | 6CC0 | Hypercrab x[9E73], Hyperscorp x[9E74], Hypersnake x[9E75] |  |
| 31 killer gennies | 6F9E | Hyperscorp x4, Hypersnake x2 | party surprised |
| 31 killer gennies | 70C8 | Hypercrab x6 |  |
| 32 space / pirate ambush | 6BCB | Pirate Warrior x7, Pirate Leader x5 |  |
| 32 space / pirate ambush | 6F4E | Talon x1, Pirate Warrior x5, Pirate Leader x25, P. Combat Robot x25 |  |
| 32 space / pirate ambush | 6FD5 | Talon x1, Pirate Warrior x2, Pirate Leader x1, P. Combat Robot x2 |  |
| 32 space / pirate ambush | 708F | Pirate Warrior x10, Pirate Leader x8, P. Combat Robot x8 |  |
| 32 space / pirate ambush | 70D2 | Pirate Warrior x2, Pirate Leader x2, P. Combat Robot x2 | enemies surprised |
| 32 space / pirate ambush | 74FB | Pirate Warrior x5, Pirate Leader x5 |  |
| 32 space / pirate ambush | 755E | Pirate Warrior x4, Pirate Leader x3 |  |
| 32 space / pirate ambush | 7594 | Pirate Warrior x5, Pirate Leader x5 |  |
| 32 space / pirate ambush | 76ED | Pirate Warrior x4, Pirate Leader x2, P. Combat Robot x1 |  |
| 32 space / pirate ambush | 7969 | Pirate Warrior x5, Pirate Leader x1, P. Combat Robot x1 |  |
| 34 pirate ship | 7307 | Ram Assassin x[9E71] | party surprised |
| 40 Mars arrival | 6E80 | Desert Ape x[9E70] |  |
| 40 Mars arrival | 6EAE | Hexadillo x[9E70] |  |
| 40 Mars arrival | 6EDD | Sand Squid x[9E70] |  |
| 40 Mars arrival | 6FA1 | Ram Warrior x3, Ram G.D. Gennie x2, Ram Mar Cgennie x3 | party surprised |
| 41 Desert Runner village | 6C10 | Ram Warrior x[9E6F], Ram Mar Cgennie x[9E70], D.R. Warrior x6 |  |
| 41 Desert Runner village | 6C98 | Ram Mar Cgennie x[9E6F], Ram Assault Bot x[9E70], D.R. Warrior x6 |  |
| 41 Desert Runner village | 6DC1 | D.R. Warrior x12 |  |
| 41 Desert Runner village | 6DF1 | Ram Warrior x5, Ram Mar Cgennie x5, D.R. Warrior x6 |  |
| 41 Desert Runner village | 6E36 | Ram Assault Bot x1 | party surprised |
| 41 Desert Runner village | 6E9B | Ram Warrior x8, Ram Mar Cgennie x3, D.R. Warrior x4 | enemies surprised |
| 41 Desert Runner village | 711F | Ram Warrior x6, Ram G.D. Gennie x4 |  |
| 41 Desert Runner village | 715E | Ram Assault Bot x1 |  |
| 42 Gradivus Mons | 6B78 | Ram G.D. Gennie x3, Ram Warrior x3 |  |
| 42 Gradivus Mons | 6D1A | Desert Ape x3 |  |
| 42 Gradivus Mons | 6FE4 | Desert Ape x[9E6F] |  |
| 42 Gradivus Mons | 706E | Desert Ape x5 |  |
| 42 Gradivus Mons | 70CB | Ram Mar Cgennie x7 |  |
| 42 Gradivus Mons | 7105 | Ram Warrior x6, Ram Combat Bot x2 |  |
| 42 Gradivus Mons | 7197 | Ram Combat Bot x2, Ram Mar Cgennie x3 |  |
| 42 Gradivus Mons | 72A2 | Ram G.D. Gennie x7 |  |
| 42 Gradivus Mons | 7397 | Ram Warrior x5, Ram Mar Cgennie x5 |  |
| 42 Gradivus Mons | 73D8 | D.R. Warrior x10, Ram G.D. Gennie x6, Ram Combat Bot x1 |  |
| 43 Mars RAM base | 6D7A | Ram Warrior x4, Ram G.D. Gennie x2 |  |
| 43 Mars RAM base | 6DBF | Ram Combat Bot x1, Ram Mar Cgennie x5 |  |
| 43 Mars RAM base | 6DF7 | Ram Combat Bot x1, Ram Mar Cgennie x5 |  |
| 43 Mars RAM base | 6E41 | Desert Ape x9, Ram Combat Bot x1 |  |
| 43 Mars RAM base | 6F2D | Ram Warrior x5, Ram G.D. Gennie x3 |  |
| 43 Mars RAM base | 6F8A | Ram G.D. Gennie x7 |  |
| 43 Mars RAM base | 7004 | Ram Warrior x5, Ram Technician x3, Ram G.D. Gennie x2 |  |
| 43 Mars RAM base | 714D | Ram Mar Cgennie x3, Ram Warrior x4 |  |
| 43 Mars RAM base | 71A5 | Ram Combat Bot x1 |  |
| 43 Mars RAM base | 721E | Ram Assault Bot x2 |  |
| 43 Mars RAM base | 73EA | Ram Combat Bot x1, Desert Ape x8 |  |
| 43 Mars RAM base | 74A2 | Desert Ape x8 |  |
| 43 Mars RAM base | 74F1 | Ram Warrior x8, Ram Mar Cgennie x1 |  |
| 43 Mars RAM base | 7580 | Ram Technician x4 |  |
| 43 Mars RAM base | 75C5 | Sand Squid x6, Ram Warrior x3 |  |
| 43 Mars RAM base | 766A | Ram Mar Cgennie x5, Ram Assault Bot x2 |  |
| 43 Mars RAM base | 76A2 | Ram Assault Bot x1, Ram Combat Bot x2 |  |
| 43 Mars RAM base | 76E8 | Ram Mar Cgennie x8 |  |
| 43 Mars RAM base | 773F | Ram Combat Bot x1 |  |
| 50 Venus arrival | 6CCC | Ram Warrior x[9E70], Acidicium x2 |  |
| 50 Venus arrival | 6CDD | Acid Frog x2, Swamp Hornet x13 |  |
| 50 Venus arrival | 6D5F | Ursadder x8 |  |
| 50 Venus arrival | 6E10 | Ven. H.S. Robot x4 |  |
| 50 Venus arrival | 704D | Ram H.S. Robot x6 | party surprised |
| 50 Venus arrival | 7125 | Acid Frog x7 |  |
| 50 Venus arrival | 71C2 | Ram Warrior x9, Ram Ven Cgennie x1, Ll. Warrior x6 |  |
| 50 Venus arrival | 7299 | Ram Ven Cgennie x6 |  |
| 51 Lowlander village | 6D86 | Ram Assassin x[9E70], Ram Ven Cgennie x2, Acid Frog x2 |  |
| 51 Lowlander village | 6E59 | Acid Frog x6 |  |
| 51 Lowlander village | 70D2 | Swamp Hornet x11 |  |
| 51 Lowlander village | 7310 | Ram H.S. Robot x3, Ram Warrior x1, Ven. H.S. Robot x2, Acid Frog x2 |  |
| 52 Venus RAM base | 6BF2 | Ram Warrior x4, Ram H.S. Robot x2, Ram Ven Cgennie x1 | party surprised |
| 52 Venus RAM base | 6F03 | ? x[9E70] |  |
| 52 Venus RAM base | 70A9 | Acidicium x5 |  |
| 52 Venus RAM base | 70D7 | Ram H.S. Robot x7 |  |
| 52 Venus RAM base | 745C | Ram Warrior x9 |  |
| 52 Venus RAM base | 750C | Ram Assassin x[9E71] |  |
| 52 Venus RAM base | 7558 | Ram Assassin x[9E70] |  |
| 52 Venus RAM base | 75BE | Ram Warrior x3 |  |
| 52 Venus RAM base | 76D2 | Ursadder x9 |  |
| 53 cave of twisted girders | 6E48 | Ram H.S. Robot x5, Ram Warrior x3 |  |
| 53 cave of twisted girders | 6E85 | Swamp Hornet x[9E70], Ursadder x[9E71] |  |
| 53 cave of twisted girders | 6EB1 | Ursadder x7 |  |
| 53 cave of twisted girders | 6F4D | Ram H.S. Robot x5, Ram Warrior x3 |  |
| 5F Mercury approach | 6E5C | Ram Warrior x10 |  |
| 5F Mercury approach | 6EBE | Ram Warrior x10 |  |
| 60 Mariposa embarkation | 6F49 | Mer. Leader x2, Mer. Warrior x10, Mer. H.S. Robot x8 |  |
| 60 Mariposa embarkation | 6FC4 | Mer. Warrior x5, Mer. Leader x1 |  |
| 60 Mariposa embarkation | 7013 | Mer. Warrior x7, Mer. Leader x1, Mer. H.S. Robot x6 | party surprised |
| 60 Mariposa embarkation | 71C3 | Mer. Warrior x5, Mer. Leader x1 |  |
| 60 Mariposa embarkation | 733A | Mer. H.S. Robot x20 |  |
| 61 Mariposa core | 6E70 | Ram Assault Bot x15 |  |
| 61 Mariposa core | 6EEC | Ram Assault Bot x1, Mer. H.S. Robot x1 |  |
| 61 Mariposa core | 71F6 | Mer. H.S. Robot x3 |  |
| 61 Mariposa core | 72BC | Ram Combat Bot x1, Ram Assault Bot x1 |  |
| 62 Mariposa interior/finale | 6DC3 | Ram Assault Bot x[9E70], Ram Combat Bot x1 |  |
| 62 Mariposa interior/finale | 6E00 | Mer. Leader x1, Mer. Warrior x[9E70] |  |
| 62 Mariposa interior/finale | 7142 | Mer. Technician x5, Ram Mer Cgennie x1 |  |
| 62 Mariposa interior/finale | 720D | Ram Mer Cgennie x10 |  |
| 62 Mariposa interior/finale | 73EA | Ram Assault Bot x3, Ram Mer Cgennie x2, Mer. Technician x4 |  |
| 62 Mariposa interior/finale | 7430 | Ram Assault Bot x1, Mer. Technician x7 |  |
| 62 Mariposa interior/finale | 7516 | Ram Mer Cgennie x8 |  |
| 63 restricted RAM areas | 6CF1 | Ram Warrior x10 |  |
| 63 restricted RAM areas | 741C | Ram Warrior x5 |  |
| 63 restricted RAM areas | 748F | Ram Warrior x6 |  |
| 63 restricted RAM areas | 755A | Ram Warrior x6 |  |
| 63 restricted RAM areas | 75D6 | Ram Assault Bot x1 |  |
| 63 restricted RAM areas | 78B6 | Ram Warrior x10 |  |
| 63 restricted RAM areas | 78DD | Ram Warrior x10 |  |
