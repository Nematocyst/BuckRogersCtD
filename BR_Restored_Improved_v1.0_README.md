# Buck Rogers Restored and Improved v1.0

Released October 12, 2026

## What this is

Buck Rogers Restored and Improved v1.0 is an IPS patch for the Sega Genesis version of *Buck Rogers: Countdown to Doomsday*. It fixes combat and character-creation bugs, restores content the developers left unused, adds two new weapons and a quest reward, and puts an item comparison line in the shop. The main patch has been played through to the end.

### Files

| File | Apply to | What it does |
| --- | --- | --- |
| `BR_Restored_Improved_v1.0.ips` | original ROM | Main patch, original level cap |
| `BR_Restored_Improved_v1.0_level9.ips` | original ROM | Main patch with the level cap raised to 9 |
| `addon_cut_weapons.ips` | main patch | Optional: sells the weapons cut from the original game |
| `addon_difficulty.ips` | main patch | Optional, **untested**: harder fights |
| `addon_cut_weapons_difficulty.ips` | main patch | Optional: both add-ons in one file |

Each add-on also comes in a `_level9` version. Use it with the level9 main patch.

### Required ROM

| Property | Value |
| --- | --- |
| Name | Buck Rogers - Countdown to Doomsday (USA, Europe).md |
| Size | 1,048,576 bytes (1 MB) |
| CRC32 | 44E3BFFF |
| MD5 | f768fbba021636fff3682b9ae116076a |
| SHA-1 | 89c39f00745f2a8798fe985ad8ce28411b977f9e |

A .bin dump of the same ROM works too, as long as the checksums match. The patch won't work on headered (.smd) or interleaved dumps.

### How to apply

1. Make a copy of the original ROM.
2. Apply one main patch to the copy with an IPS patcher (Lunar IPS, Floating IPS or Rom Patcher JS).
3. Optional: apply **one** add-on to the patched ROM. Match the variant: level9 add-ons go with the level9 main patch.
4. Load the result in an emulator or on a flash cart.

Don't stack two add-ons. Each one carries a checksum fix that is only correct on its own, so use `addon_cut_weapons_difficulty` if you want both. Emulator soft-patching usually loads a single IPS file, so create a patched ROM file instead.

Start a new game. Saves from an unpatched ROM have not been tested with the patch.

## Bug fixes

Three bugs in the original game are fixed. All three were confirmed in play.

| Bug | Original behaviour | Fixed behaviour |
| --- | --- | --- |
| Rocket special | Against targets weak to rockets (most robots), a rocket weapon can roll a "full damage" special. The game stored that hit as 255, read it as a negative number and threw it away, so the special made the hit do nothing. | The special no longer wipes the hit; it deals its rolled damage. |
| Quick Combat weapon choice | Quick Combat cut a rocket weapon's score to a quarter, so a character specialized in the rocket pistol fired a laser pistol instead. | Rocket weapons are scored like any other weapon, so specialized characters use them. |
| Character creation | Picking Desert Runner and then switching to Human kept the Desert Runner's claws (1d3 unarmed damage, +1 claw bonus). Switching to Tinker kept the 1d3. | Switching race resets unarmed damage correctly. Warriors still get 1d3 fists, as intended. |

## Restored and added content

The original shops sold only a handful of racial weapons and four armors. The patch fills the racial shops with the weapons and armor the item system supports, adds two rocket pistols, and gives the courier mission a reward.

### Racial weapons and armor in shops

| Shop | Added |
| --- | --- |
| Hielo | Mercurian Needle Gun, Cutlass, Laser Pistol, Rocket Pistol and Mono Sword; Demo Charge |
| New Elysium | Venusian Mono Sword, Needle Gun, Heat Gun and Rocket Pistol (new); Demo Charge |
| Tycho | Lunarian Laser Pistol and Heat Gun; Lunarian Needle Gun and Rocket Pistol (both new); Demo Charge |
| Psyche | Martian Cutlass, Needle Gun, Rocket Pistol and Heat Gun |
| Juno | Demo Charge |
| Aurora | Martian Spacesuit, Smartsuit, Heavy Body Armor, Battle Armor and Battle Armor W/Fields; Venusian Heavy Body Armor |

The racial items already existed, complete, in the game's data; the original shops just never sold them. Of the 16 racial armors, 12 couldn't be obtained at all in the original game. Only three items are new records: the Lunarian Needle Gun and the two rocket pistols below. Nothing the original shops sold was removed, except the duplicate armors (see Balance).

A racial item beats the plain version by its race's tier: Martian +1, Venusian +2, Mercurian +3, Lunarian +4. On a weapon the tier is added to the damage of every hit; on armor it improves AC by the same amount.

### New rocket pistols

| Item | Shop | Base price (credits) |
| --- | --- | --- |
| Venusian Rocket Pistol | New Elysium | 2,000 |
| Lunarian Rocket Pistol | Tycho | 8,000 |

They complete the rocket pistol set, which already had Plain (400), Martian (1,000) and Mercurian (4,000).

### Courier reward

In the original game, delivering the courier parcel gives nothing but a message. Now NEO answers "NEO SENDS ITS THANKS." and every party member gets 1,000 XP.

### Name fixes

- "Martian Sword Gun" is now **Martian Heat Gun**.
- "Mercurian Mono Bolt" is now **Mercurian Mono Sword**.

### Bigger shop lists

The original ROM packs every shop's item list tightly against the next, so no shop has room for even one more item. The original shops sold at most 8 items each. The patch moves all shop lists to a new table that gives each shop room for up to 15 items. That extra space is what lets the racial shops hold their new stock: Tycho sells 10 items with the main patch and 14 with the cut weapons add-on, and all 14 show on screen. This was not a feature of the original game.

## Interface: shop comparison line

The buy screen now shows an extra line under each weapon or armor, comparing it with what the selected buyer has equipped.

| Item type | Example | Meaning |
| --- | --- | --- |
| Weapon | `DMG 6-13 X3  NOW 2-9 X3` | With this weapon (here a Lunarian Laser Pistol) the buyer would do 6–13 damage per hit with 3 attacks per round; with the current weapon, 2–9 and 3 attacks. |
| Armor | `AC 2  NOW 4` | Wearing this armor (here a Smartsuit) the buyer's AC would be 2; it is 4 now. |

- **NOW** follows the character selected as the buyer. Change the buyer to compare for someone else.
- With an empty party slot selected, the line reads `NOW 0 X0`.
- The numbers are worked out the same way the character sheet does it: weapon dice, strength bonus for melee weapons, weapon specializations and the racial bonus. Grenades and other items get no line, and the loot screen is unchanged.
- Lower AC is better, as on the character sheet. One armor step, such as Spacesuit to Smartsuit (AC 4 to 2), lowers enemies' chance to hit by 10 percentage points; each point of AC is worth 5. Armor changes nothing else: movement, attacks and initiative stay the same.

## Balance: armor clean-up

Shops now sell one armor per armor bonus, the cheapest one. The armor bonus (base armor plus racial tier) is the only difference between armors, so the dearer duplicates were pointless; each point of bonus is one point of AC. The four armors the original game sold are always kept. Ten armors left the shops. Item records are untouched, so any of them that drop as loot still drop.

| Armor bonus | Sold | Removed from shops (base price) |
| --- | --- | --- |
| 4 | Spacesuit (500) | — |
| 5 | Martian Spacesuit (1,000) | — |
| 6 | Smartsuit (1,200) | Venusian Spacesuit (2,000) |
| 7 | Martian Smartsuit (2,400) | Mercurian Spacesuit (4,000) |
| 8 | Heavy Body Armor (1,700) | Venusian Smartsuit (4,800), Lunarian Spacesuit (8,000) |
| 9 | Martian Heavy Body Armor (3,400) | Mercurian Smartsuit (9,600) |
| 10 | Venusian Heavy Body Armor (6,800) | Lunarian Smartsuit (19,200) |
| 11 | Martian Battle Armor (5,000) | Mercurian Heavy Body Armor (13,600) |
| 12 | Battle Armor W/Fields (3,000) | Venusian Battle Armor (10,000), Lunarian Heavy Body Armor (27,200) |
| 13 | Martian Battle Armor W/Fields (6,000) | Mercurian Battle Armor (20,000) |

As a result, Juno sells no armor, and Aurora's list dropped from 13 items to 10.

## Options

Pick one main patch, then at most one add-on. Apply the main patch first.

### Level cap: level9 variant

The original game caps every career at level 8. The `_level9` main patch raises the cap to 9 and is otherwise identical. The level-9 thresholds come from the 1990 TSR *Buck Rogers XXVc* tabletop rulebook.

| Career | Level 8 XP (original) | Level 9 XP (new) |
| --- | --- | --- |
| Rocketjock | 70,000 | 110,000 |
| Warrior | 125,000 | 250,000 |
| Rogue | 70,000 | 110,000 |
| Medic | 96,000 | 192,000 |

Training works as normal: visit a gym or HQ, and you get the usual skill and specialization points. Add-ons have matching `_level9` versions; never mix a level9 add-on with the normal main patch.

### Add-on: cut weapons

The original game has data for nine kinds of weapon it never sells: Knife, Mono Knife, Sword, Polearm, Bolt Gun, Behind Cover Gun, Laser Rifle, Rocket Rifle and Sonic Stunner. This add-on puts 18 racial versions of them in four shops.

| Shop | Added |
| --- | --- |
| Hielo | Mercurian Polearm, Behind Cover Gun, Laser Rifle |
| New Elysium | Venusian Mono Knife, Sword, Bolt Gun, Laser Rifle |
| Tycho | Lunarian Knife, Bolt Gun, Laser Rifle, Sonic Stunner |
| Psyche | Martian Mono Knife, Sword, Bolt Gun, Behind Cover Gun, Laser Rifle, Rocket Rifle, Sonic Stunner |

- **These weapons have no icon.** The game never had art for them, so shops and inventory show the name next to an empty icon box. No stand-in art was added.
- Their stats work: base dice plus the racial tier bonus.
- Tycho's list grows to 14 items, one more than any original shop. It was checked in play: every item shows.
- The Sonic Stunner's stun effect has not been checked.

### Add-on: difficulty (untested)

**This add-on has not been tested in play.** Nobody has fought a battle with it yet. Use it at your own risk and report problems.

Late-game enemies were cheap for the experience they give. The add-on toughens 25 enemy records with three changes; early enemies, loot and Buck Rogers himself are unchanged.

1. **HP floor:** an enemy's HP is at least its XP value ÷ 8 (up to 255).
2. **Armor:** armored enemies worth 200–450 XP get 2 points better AC, so the party's chance to hit them drops by 10 percentage points. This is done on the enemy record, so they don't drop better armor.
3. **Attack:** enemies worth 400 XP or more get +2 to hit, raising their chance to hit by 10 percentage points.

| Enemy | HP | AC better by | Attack |
| --- | --- | --- | --- |
| Space Rat | 10 → 16 |  |  |
| Hyperscorp, Hypersnake | 24 → 25 |  |  |
| RAM H.D. Robot | 30 → 31 |  |  |
| Pirate Leader, RAM Assassin, RAM Martian and Venusian CGennies |  | +2 |  |
| Stage 3 ECG | 35 → 38 | +2 |  |
| Mercurian Technician | 24 → 38 | +2 |  |
| Mercurian Warrior (both records) | 49 / 38 → 50 | +2 | +2 |
| RAM L.S. Robot | 44 → 50 |  | +2 |
| Mercurian Leader | 49 → 56 |  | +2 |
| P. Combat Robot | 60 → 75 |  | +2 |
| RAM H.S. Robot, Venusian H.S. Robot | 55 → 75 |  | +2 |
| Desert Ape | 48 → 78 |  | +2 |
| Sand Squid, Acid Frog | 36 → 78 |  | +2 |
| Talon | 90 → 144 |  | +2 |
| Mercurian H.S. Robot | 77 → 156 |  | +2 |
| RAM Assault Bot | 72 → 188 |  | +2 |
| RAM Combat Bot | 88 → 255 |  | +2 |

Acid Frogs fight on your side in one Venus scene, and they get the same boost there. In the emulator, the RAM base fight loads the Combat Bot with 255 HP and +2 attack. Feedback from play will decide the next balance pass.

### Using both add-ons

The two add-ons change different parts of the ROM, so they work together. Use `addon_cut_weapons_difficulty`, which holds both. Applying the two separate files one after the other leaves a wrong checksum.

## Shops reference

Every shop with the main patch, in game order. Prices are base prices in credits. Hielo and Juno charge 4× the base price; every other shop charges 2×. Items marked + are new or newly sold.

| Shop | Markup | Items (base price) |
| --- | --- | --- |
| Salvation | 2× | Cutlass 100, Needle Gun 210, Laser Pistol 335, Rocket Pistol 400, Heat Gun 500, Spacesuit 500, Smartsuit 1,200, Poison Antidote 400 |
| Hielo | 4× | Heavy Body Armor 1,700, Protective Goggles 10, Mercurian Heat Gun 4,000, +Mercurian Needle Gun 1,610, +Mercurian Cutlass 800, +Mercurian Laser Pistol 2,680, +Mercurian Rocket Pistol 4,000, +Mercurian Mono Sword 8,000, +Demo Charge 100 |
| New Elysium | 2× | Venusian Laser Pistol 1,340, Smartsuit 1,200, Protective Goggles 10, Poison Antidote 400, +Venusian Mono Sword 4,000, +Venusian Needle Gun 840, +Venusian Heat Gun 2,000, +Demo Charge 100, +Venusian Rocket Pistol 2,000 |
| Tycho | 2× | Lunarian Mono Sword 16,000, Poison Antidote 400, Stun Grenade 50, Dazzle Grenade 50, Aerosol Mist Grenade 50, +Lunarian Laser Pistol 5,360, +Lunarian Heat Gun 8,000, +Demo Charge 100, +Lunarian Needle Gun 3,360, +Lunarian Rocket Pistol 8,000 |
| Pavonis | 2× | Cutlass 100, Mono Sword 1,000, Martian Cutlass 200 |
| Psyche | 2× | Laser Pistol 335, Martian Laser Pistol 670, +Martian Cutlass 200, +Martian Needle Gun 420, +Martian Rocket Pistol 1,000, +Martian Heat Gun 1,000 |
| Juno | 4× | ECM Package 50, Stun Grenade 50, Dazzle Grenade 50, Chaff Grenade 50, Aerosol Mist Grenade 50, +Demo Charge 100 |
| Hygeia | 2× | Cutlass 100, Heat Gun 500, Grenade Launcher 500 |
| Aurora | 2× | Spacesuit 500, Smartsuit 1,200, Battle Armor W/Fields 3,000, Protective Goggles 10, +Martian Spacesuit 1,000, +Martian Smartsuit 2,400, +Martian Heavy Body Armor 3,400, +Martian Battle Armor 5,000, +Martian Battle Armor W/Fields 6,000, +Venusian Heavy Body Armor 6,800 |
| Thule | 2× | Rocket Pistol 400, Martian Rocket Pistol 1,000 |

The cut weapons add-on adds its items to Hielo, New Elysium, Tycho and Psyche (see Options).

## Known limits

These are how the game works, not bugs in the patch.

- **Locked doors.** Some station doors never open by design ("the rest of the base is restricted", "the rest of Salvation is off limits").
- **Unused effects.** Item effects 0x0A, 0x0B, 0x10, 0x12 and 0x13 have empty handlers in the original code and do nothing.
- **Lists that aren't shops.** Shop lists 0, 2, 4, 6, 7 and 8 are never opened as a shop by any script. List 8 is used as a monster loot table (RAM Assassins, Stage 3 ECGs).
- **Level 9 is the top.** The level9 variant has no level-10 entry. A level-9 character who somehow earns enough XP for a tenth level would hit undefined training behaviour. It takes a very large amount of XP, so it's a late-game edge case.
- **Cut weapon icons.** With the cut weapons add-on, those weapons have no icon because none exists in the game.

## Technical notes

For ROM hackers. All addresses are ROM file offsets.

### Changes

| Change | Address | Edit |
| --- | --- | --- |
| Item table moved | 0xBF0, 0x16254, 0x1628C | pointers 0xF17CC → 0xF2200 |
| Shop lists moved | 0x3D46–0x3D53 | lookup reads a pointer table at 0xF2000 instead of the packed offset table at 0x3D76; each list gets a 16-byte slot (count + up to 15 items) |
| Rocket special fix | 0x10892 | `67 04` → `60 04` (never write 0xFF over the hit) |
| Quick Combat fix | 0xE95D | `02` → `00` (no /4 rocket penalty) |
| Shop comparison line | hook 0x16ABE | new code at 0xF5000–0xF51F7 |
| Character creation fix | 0x6A8, 0x6DC | jumps to stubs at 0xF6000 |
| Racial rocket pistols | 0xF25A2, 0xF25AC | item records 0x5D, 0x5E |
| Courier reward | script module 0x63, offset 0x7C95 | module relocated to 0xFB000 (text to 0xFBB28) |
| Difficulty add-on | 0x48F3 | pointer to a new table at 0xF8000 |
| Cut weapons add-on | 0xA82A–0xA83C, 0x7C1E, 0x16910 | category table entries; list builders keep icon 0xFF |
| Checksums | 0xFFFCC (game), 0x18E (header) | recomputed for every build |

### Free-space map

| Range | Used for |
| --- | --- |
| 0xF2000–0xF21FF | shop list pointers and lists |
| 0xF2200– | item records, 10 bytes each (0x00–0x5E) |
| 0xF5000–0xF51F7 | shop comparison code |
| 0xF6000– | character creation fix |
| 0xF8000– | difficulty table (add-on) |
| 0xFB000– | relocated script module 0x63 |

### Checksums

| Build | Game checksum (0xFFFCC) | Header (0x18E) |
| --- | --- | --- |
| Main patch | 91 C8 12 FA | A2 0F |
| Main patch, level9 | AB B8 13 05 | D6 09 |

### Rebuilding

The build kit is in the [`kit/`](kit/) folder of this repository. It rebuilds both main patches from the original ROM, byte-identical to the released files:

```
cd kit
sh build_release.sh ORIGINAL.md          # build/release.ips
sh build_release.sh ORIGINAL.md level9   # build/release_level9.ips
```

The build applies, in order: restoration base, rocket fix, Quick Combat fix, shop comparison line, creation fix, racial rocket pistols, armor clean-up, courier reward. After both builds, `python3 tools/build_addons.py ORIGINAL.md` builds the add-ons against each main patch.

Full instructions, checks and the data formats behind these edits are in [docs/BUILDING.md](docs/BUILDING.md) and [docs/ROM-FORMATS.md](docs/ROM-FORMATS.md).

### Credits

- **Nematocyst:** playtesting and debugging
- **Claude AI:** coding and reverse engineering
