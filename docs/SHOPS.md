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

## Capacity

- The release gives each list a 16-byte slot: the count plus up to 15 item ids. The original game's shops sold at most 8 items each, and its lists had no spare room.
- With the cut weapons add-on, Tycho sells 14 items, one more than any original shop. All 14 were checked on screen in play.
- The racial rocket pistols are item ids `0x5D` (Venusian, New Elysium, list 5) and `0x5E` (Lunarian, Tycho, list 9). The add-on builder checks that they stay in those lists.

## Armor in shops

Armor value is the only difference between armors, so v1.0 keeps one armor per armor value, the cheapest. `dedupe_armor.py` does this. It never removes the four armors the original game's shops sold: spacesuit (`0x14`), smartsuit (`0x15`), heavy body armor (`0x16`) and battle armor w/fields (`0x18`). Only the shop lists change; the item records stay, so removed armors can still drop as loot. The full table is in the v1.0 README under *Balance: armor clean-up*.

## Things that look like they should work and do not

Found while building the earlier patches; the v1.0 README has the user-facing summary.

- **Some weapon kinds never show up in a shop.** In v8, 18 entries (knives, sonic stunners, swords, bolt guns, rocket rifles, behind-cover guns, laser rifles, polearms) appeared as nothing or as a plain base item. The shop code drops or swaps whole weapon kinds for every race. The cut weapons add-on of v1.0 does sell 18 racial versions of them. It adds entries to the category table and changes the list builders (`0xA82A`–`0xA83C`, `0x7C1E`, `0x16910`) so they show. They have no icon, because the game never had art for them.
- **A list that is not a shop looks like one.** The v6 patch put the Martian weapons into list 8, believing it was a Martian outpost shop. It is a loot table in the same format. v8 moved them to Psyche (list 11).
