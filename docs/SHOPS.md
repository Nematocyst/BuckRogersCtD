# Shops

The game has 16 shop lists, numbered 0–15 (the data format is in [ROM-FORMATS.md](ROM-FORMATS.md#shop-lists)). Ten of them are real shops. The other six are never opened as a shop by any script.

## Shop numbers

| List | Shop | Price markup |
| --- | --- | --- |
| 1 | Salvation | 2× |
| 3 | Hielo | 4× |
| 5 | New Elysium | 2× |
| 9 | Tycho | 2× |
| 10 | Pavonis | 2× |
| 11 | Psyche | 2× |
| 12 | Juno | 4× |
| 13 | Hygeia | 2× |
| 14 | Aurora | 2× |
| 15 | Thule | 2× |

Prices in the item records are base prices. A shop charges the base price times its markup. Hielo and Juno charge 4×; every other shop charges 2×.

Lists 0, 2, 4, 6, 7 and 8 are not shops. List 8 is used as a monster loot table (RAM Assassins and Stage 3 ECGs). The others are never opened by any script and appear to be debug lists.

## What each shop sells in v1.0

The complete stock of every shop in v1.0, with prices, is the *Shops reference* table in the [v1.0 README](../BR_Restored_Improved_v1.0_README.md#shops-reference). The cut weapons add-on adds items to Hielo, New Elysium, Tycho and Psyche.

## What the original game sold

Lists as the original ROM defines them, decoded from the packed lists at `0x3D96` onward (offset table `0x3D76`). Each list is `[count][item ids]`. Variant item ids start at `0x27` (Martian Laser Pistol `0x2D`, Venusian `0x39`, Mercurian `0x41`, Lunarian `0x48`).

| List | Address | Stock | Shop |
| --- | --- | --- | --- |
| 0, 1 | `0x3D96` | Cutlass, Needle Gun, Laser Pistol, Rocket Pistol, Heat Gun, Spacesuit, Smartsuit, Poison Antidote | Salvation (0 shares the list) |
| 2 | `0x3D9F` | Mono Sword, Grenade Launcher | unused |
| 3 | `0x3DA2` | Heavy Body Armor, Protective Goggles, Mercurian Heat Gun | Hielo |
| 4 | `0x3DA6` | Cutlass, Needle Gun, Mono Sword, Laser Pistol | unused |
| 5 | `0x3DAB` | Venusian Laser Pistol, Smartsuit, Protective Goggles, Poison Antidote | New Elysium |
| 6 | `0x3DB0` | Mono Sword, Grenade Launcher | unused |
| 7 | `0x3DB3` | Spacesuit, ECM Package | unused |
| 8 | `0x3DB6` | Martian Mono Sword, Martian Laser Pistol | loot table |
| 9 | `0x3DB9` | Lunarian Mono Sword, Poison Antidote, Stun Grenade, Dazzle Grenade, Aerosol Mist Grenade | Tycho |
| 10 | `0x3DBF` | Cutlass, Mono Sword, Martian Cutlass | Pavonis |
| 11 | `0x3DC3` | Laser Pistol, Martian Laser Pistol | Psyche |
| 12 | `0x3DC6` | ECM Package, Stun, Dazzle, Chaff and Aerosol Mist Grenades | Juno |
| 13 | `0x3DCC` | Cutlass, Heat Gun, Grenade Launcher | Hygeia |
| 14 | `0x3DD0` | Spacesuit, Smartsuit, Battle Armor W/Fields, Protective Goggles | Aurora |
| 15 | `0x3DD5` | Rocket Pistol, Martian Rocket Pistol | Thule |

The shop is opened by a `STORE n` script command: list 1 by Salvation's script (module `0x11`), lists 3, 5 and 9–15 by the shared starport script (module `0x63`), which picks the list from the docking location and sets the markup. Names that matched shop locations were cross-checked against a public item database; the Hielo, Juno and Aurora lists were matched by their exact items and prices.

## Capacity

- The release gives each list a 16-byte slot: the count plus up to 15 item ids. The original loader accepts at most 14 of them. The original game's shops sold at most 8 items each, and its lists had no spare room.
- With the cut weapons add-on, Tycho sells 14 items, six more than the largest original shop. All 14 were checked on screen in play.
- The racial rocket pistols are item ids `0x5D` (Venusian, New Elysium, list 5) and `0x5E` (Lunarian, Tycho, list 9). The add-on builder checks that they stay in those lists.

## Armor in shops

Armor value is the only difference between armors, so v1.0 keeps one armor per armor value, the cheapest. `dedupe_armor.py` does this. It never removes the four armors the original game's shops sold: spacesuit (`0x14`), smartsuit (`0x15`), heavy body armor (`0x16`) and battle armor w/fields (`0x18`). Only the shop lists change; the item records stay, so removed armors can still drop as loot. The full table is in the v1.0 README under *Balance: armor clean-up*.

## Which weapon kinds survive into a shop

The shop code applies a fixed rule by base item, the same for every race. It was tested by putting 13 item ids at a time into Tycho's list, opening the shop with an injected `STORE 9`, and matching the stock records in RAM back to item ids. The result was the same after 0, 7, 23 or 61 extra frames, so it is not random availability.

| Base kind | Result in the original game |
| --- | --- |
| Knife, mono knife (all races) | dropped |
| Sonic stunner (all races) | dropped |
| Sword | becomes the plain Mono Sword |
| Bolt gun | becomes the plain Needle Gun |
| Rocket rifle, cover gun | become the plain Rocket Pistol |
| Laser rifle | becomes the plain Laser Pistol |
| Polearm | becomes the plain Cutlass |
| Mini explosive, treating-wounds grenade | become Explosive / Stun Grenade |
| "lost.", rope, belt | dropped |
| everything else, and its racial versions | unchanged |

Substitutes are always the plain base item, even for racial originals. The rule is the category table at `0xA829` ([ROM-FORMATS.md](ROM-FORMATS.md#availability-and-icon-tables)); no field of the item records or the stat table explains the split. It looks like a deliberate cut in the Genesis port (a reduced weapon set), though that is not proven. All racial armor works.

## Things that look like they should work and do not

Found while building the earlier patches; the v1.0 README has the user-facing summary.

- **Some weapon kinds never show up in a shop.** In v8, 18 entries (knives, sonic stunners, swords, bolt guns, rocket rifles, behind-cover guns, laser rifles, polearms) appeared as nothing or as a plain base item. The shop code drops or swaps whole weapon kinds for every race. The cut weapons add-on of v1.0 does sell 18 racial versions of them. It adds entries to the category table and changes the list builders (`0xA82A`–`0xA83C`, `0x7C1E`, `0x16910`) so they show. They have no icon, because the game never had art for them.
- **A list that is not a shop looks like one.** The v6 patch put the Martian weapons into list 8, believing it was a Martian outpost shop. It is a loot table in the same format. v8 moved them to Psyche (list 11).
