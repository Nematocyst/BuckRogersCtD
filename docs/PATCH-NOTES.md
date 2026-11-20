# Patch notes: how each change works

One section per change in the v1.0 release and its add-ons, with the addresses and bytes involved. The release layers are listed in [COMPONENTS.md](COMPONENTS.md); how to build them is in [BUILDING.md](BUILDING.md). All addresses are ROM offsets in the original USA/Europe ROM. Rules these fixes touch are described in [COMBAT-RULES.md](COMBAT-RULES.md).

## Contents

1. [Rocket special fix](#rocket-special-fix)
2. [Quick Combat weapon choice fix](#quick-combat-weapon-choice-fix)
3. [Character creation fix](#character-creation-fix)
4. [Shop comparison line](#shop-comparison-line)
5. [Racial rocket pistols](#racial-rocket-pistols)
6. [Armor shop clean-up](#armor-shop-clean-up)
7. [Courier reward](#courier-reward)
8. [Cut weapons (add-on)](#cut-weapons-add-on)
9. [Difficulty (add-on)](#difficulty-add-on)
10. [Checksums](#checksums)

## Rocket special fix

**Symptom:** a rocket pistol did no damage to robots.

**Mechanism.** `ResolveAttack` (`0x10804`) clears the flag `[0xD4FC]`. For each hit it computes the damage into `[0xD497]` and runs the effect stages. Then, if the attacker holds a rocket-type weapon (`0x1062C`: weapon-table type 2, i.e. rocket pistol or rocket rifle) and the target's record byte +`0x2F` has bit 2 (75%) or bit 1 (50%), it sets `[0xD4FC]` (`0x10886`). The stage-5 handler of effect `0x14` ("weak to rockets", carried by most robots) sets the flag the same way, without a dice roll. At `0x1088E`, if the flag is set, the hit's damage is replaced by `0xFF` (`0x10894`). That is the "no hit" marker of the damage list, so the damage applier skips it. Once set, the flag stays set for the rest of that attack. Every rocket hit on a robot was therefore discarded.

**Fix (1 byte).** `0x10892`: `67 04` (`beq`) becomes `60 04` (`bra`). The `0xFF` is never written and the hit keeps its rolled damage. Rockets get no bonus against robots but are no longer wasted, and misses stay skipped.

**Withdrawn first attempt.** Changing `0x10E64` from `6F` to `67` made the damage applier take every list entry from 1 to 255. The list is pre-filled with `0xFF` for "no hit", so every miss dealt 255 damage and the party died in round one. Do not use it.

**Status:** played; rocket pistols do reasonable damage to robots.

## Quick Combat weapon choice fix

**Symptom:** Quick Combat (computer-controlled party members) avoided the rocket pistol.

**Mechanism.** `ChooseWeapon` (`0xE89C`) scores each weapon by expected damage, then divides by a "will not work" shift decoded at `0xE912`–`0xE95F`:

- lasers: ÷ 8 if the line of fire crosses an aerosol cloud or the target has effect `0x18`;
- rockets: ÷ 8 behind a chaff cloud; otherwise **÷ 4 if the target's record +`0x2F` has bits `0x06` or the target has effect `0x14`**.

In the original the ÷ 4 made sense, because rocket hits on those targets did nothing (see above). With the rocket fix it only steers the computer away from a weapon that now works. The flag +`0x2F` is set on many non-robot creatures too, armored humans included, so the penalty hit more than robots.

**Fix (1 byte).** `0xE95D`: the shift value `2` becomes `0` in `move.b #2,-12(a6)` (the instruction at `0xE95A`). The cover shift and the laser-immunity shift are unchanged. **Use only together with the rocket fix.** A first build changed `0xE95B` (the opcode) by mistake and crashed the emulator; the correct byte is `0xE95D`.

**Status:** boots; the in-fight effect was not observed in a controlled emulator test.

## Character creation fix

**Bug (original game, confirmed live).** On the creation screen, desert runner sets +`0x2A` = 3 (die sides, 1d3) and +`0x2C` = 1 (claw bonus). Switching to human keeps both, because the human handler (`0x6A8`) clears +`0x0C` (a name byte) instead of +`0x2C`. The tinker handler clears +`0x2C` but keeps +`0x2A` = 3. Measured on the original ROM: desert runner → human gave race 1, movement 8, +`0x2A` 3, +`0x2C` 1.

**Fix.** `0x6A8` (human) and `0x6DC` (tinker) become `jsr 0xF6000` and `jsr 0xF600E`. The stubs, 28 bytes at `0xF6000`, are: `clr.b $2C(a2); move.b #2,$2A(a2); moveq #1 / #3,d0; rts`. Warriors and desert runners still get 1d3 at the creation finish (`0x81C`). The range `0xF6000`–`0xF601B` must be free in the build (the shop line uses `0xF5000`–`0xF51F7`, avatars `0xF4000`, items `0xF2200`, the relocated monster file `0xF8000`). See [RECORD-FIELDS.md](RECORD-FIELDS.md#character-creation-values-and-the-fixed-bug) for the fields.

**Checksum trap.** A patch that changes ROM code must also fix the checksums (see [Checksums](#checksums)). A first version of this fix did not, and the ROM booted to a black screen. When combining with other patches, apply the code bytes and recompute the checksums once; do not apply a stand-alone patch with its own checksum words on top of another one.

**Status:** played. Desert runner → human medic shows 1–2 unarmed damage; → human warrior 1–3 (warrior fists); tinker → desert runner → tinker 1–2.

## Shop comparison line

With the cursor on an item in a shop, a third line appears under name and price:

- weapons: `DMG 6-13 X3  NOW 2-9 X3`, the damage range and attacks per round of this weapon, then of the selected character's current weapon, computed as the character sheet does (dice, table bonus, strength bonus for melee, specializations, the item's flat bonus);
- armor: `AC 2  NOW 4`, the armor class the character would have, then the current one (lower is better, as on the sheet);
- grenades and other items: no extra line. The loot screen after fights is unchanged.

The hook is at `0x16ABE` (`jmp 0xF5000`); the routine is 504 bytes at `0xF5000`–`0xF51F7`. Standalone file: `kit/shop_compare_only.ips`.

## Racial rocket pistols

The original game has three rocket pistols (plain, Martian, Mercurian). The patch adds the missing two, in the same way as the Lunarian needle gun:

| Id | Item | Record | Price | Sold at |
| --- | --- | --- | --- | --- |
| `0x5D` | Venusian Rocket Pistol (1d10 +2, 4 attacks) | `0a 0a 0b 14 02 00 07 d0 00 00` | 2000 (× 5) | New Elysium (list 5) |
| `0x5E` | Lunarian Rocket Pistol (1d10 +4, 4 attacks) | `0a 0a 0b 16 04 00 1f 40 00 00` | 8000 (× 20) | Tycho (list 9) |

Prices follow the existing rocket pistols (400, Martian 1000, Mercurian 4000) before port markup. The records go after the Lunarian needle gun in the relocated item table (`0xF25A2`, `0xF25AC`). In an emulator both are in stock and the comparison line reads `DMG 3-12 X4` and `DMG 5-14 X4`. A purchase was not tested for these two, but the mechanism is the one the Lunarian needle gun uses, which works in play.

## Armor shop clean-up

Armor value = base armor + racial tier (Martian +1, Venusian +2, Mercurian +3, Lunarian +4). The armors differ in nothing else. For every armor value only the cheapest armor stays in the shops, and the four armors the original shops sold (Spacesuit, Smartsuit, Heavy Body Armor, Battle Armor W/Fields) are always kept.

| Armor value | Kept (price) | Removed from shops |
| --- | --- | --- |
| +4 | Spacesuit (500) | |
| +5 | Martian Spacesuit (1000) | |
| +6 | Smartsuit (1200) | Venusian Spacesuit |
| +7 | Martian Smartsuit (2400) | Mercurian Spacesuit |
| +8 | Heavy Body Armor (1700) | Venusian Smartsuit, Lunarian Spacesuit |
| +9 | Martian Heavy Body Armor (3400) | Mercurian Smartsuit |
| +10 | Venusian Heavy Body Armor (6800) | Lunarian Smartsuit |
| +11 | Martian Battle Armor (5000) | Mercurian Heavy Body Armor |
| +12 | Battle Armor W/Fields (3000) | Venusian Battle Armor, Lunarian Heavy Body Armor |
| +13 | Martian Battle Armor W/Fields (6000) | Mercurian Battle Armor |

Aurora ends with 10 items. Juno keeps its ECM package, grenades and Demo Charge but sells no armor (all of its armors were dearer duplicates, at Juno's × 4 markup). Item records are untouched, so the removed armors can still drop as loot. Tool: `kit/tools/dedupe_armor.py`. See [SHOPS.md](SHOPS.md).

## Courier reward

In the original, the bar courier in the starport script (module `0x63`, flag set at `0x74A0`) only sets a temporary flag. On launch (`0x6E5B`) the script prints "AS YOU DEPART THE STATION, SCOT TRANSMITS THE COURIER'S INFORMATION." (subroutine `0x7C95`) and nothing else.

The patch is a script edit: after the transmission line it prints "NEO SENDS ITS THANKS." and runs `ADDEP 1, #1000`, which gives 1000 XP to every party member who is present and not down, with the game's own "the team gains … experience points" message. The new text is `T1209` in text module `0x63`. Module `0x63` grew and was moved to free space; the module table entry is updated by the tool. The patched module disassembles to exactly: `0x7C95` → goto `0x7C9C` → new code → `RETURN`. The courier is a random bar event, so this was not seen in play.

## Cut weapons (add-on)

The original game has item kinds that were cut before release (knife, mono knife, sword, polearm, behind cover gun, laser rifle, rocket rifle, sonic stunner, and the racial versions). The add-on puts them back into shop lists. They have no art, so their icon box is empty (the DOS version has no item images either, and no stand-in icons are used).

**Why names needed a code change.** The item-icon lookup (`0xA7BC`, table `0xA802`) returns `0xFF` for kinds without an icon. The two list builders replaced `0xFF` with `0x4E`, the "empty slot" marker, so the screens skipped the whole entry, name included. The add-on keeps `0xFF` (`bne` → `bra`): the icon drawer draws nothing for `0xFF` and the name is printed.

| Address | Change | Screen |
| --- | --- | --- |
| `0x7C1E` | `66` → `60` | inventory list (builder `0x7C1A`) |
| `0x16910` | `66` → `60` | shop and loot list (builder `0x1690A`) |

Kinds restored through the category table at `0xA829` (indexed by base item id: `FF` normal, `00` hidden, `n` = replaced by base item `n`) have `FF` in the icon table. The bolt gun uses the needle gun's icon, as in the original data. Items with an empty icon box: knife, mono knife, sword, polearm, behind cover gun, laser rifle, rocket rifle, sonic stunner, all racial versions.

In an emulator at Tycho the blank cells show "LUNARIAN KNIFE" and "LUNARIAN LASER RIFLE". The change is 49 bytes against the v9 base; 4 of them overlap the release's shop lists (see [COMPONENTS.md](COMPONENTS.md#how-the-add-ons-are-derived)).

## Difficulty (add-on)

The late game is easy for the experience it pays. The first balance pass has three monster-only levers; early enemies, loot and Buck Rogers are unchanged:

1. **HP floor = XP ÷ 8** (maximum 255) for under-built enemies.
2. **+1 armor rung** (base armor +2, so the party's chance to hit drops by 10 points) for armored enemies worth 200–450 XP. It is done on the monster record rather than by giving better armor, so loot does not improve.
3. **+2 attack** (their chance to hit rises by 10 points) for enemies worth 400+ XP.

| Enemy | HP | Armor | Attack |
| --- | --- | --- | --- |
| Space Rat | 10 → 16 | | |
| Hyperscorp, Hypersnake | 24 → 25 | | |
| RAM H.D. Robot | 30 → 31 | | |
| Pirate Leader, RAM Assassin, RAM Martian / Venusian CGennie | | +1 rung | |
| Stage 3 ECG | 35 → 38 | +1 rung | |
| Mercurian Technician | 24 → 38 | +1 rung | |
| Mercurian Warrior (both records) | 49 / 38 → 50 | +1 rung | +2 |
| RAM L.S. Robot | 44 → 50 | | +2 |
| Mercurian Leader | 49 → 56 | | +2 |
| P. Combat Robot | 60 → 75 | | +2 |
| RAM H.S. Robot, Venusian H.S. Robot | 55 → 75 | | +2 |
| Desert Ape | 48 → 78 | | +2 |
| Sand Squid, Acid Frog | 36 → 78 | | +2 |
| Talon | 90 → 144 | | +2 |
| Mercurian H.S. Robot | 77 → 156 | | +2 |
| RAM Assault Bot | 72 → 188 | | +2 |
| RAM Combat Bot | 88 → 255 | | +2 |

Only the HP, armor and attack bytes of these 25 monster records change. The patch is **not play-tested**; feelings about how it plays decide the next pass. Acid Frogs can be allies in a Venus scene and get the same boost.

## Checksums

Any patch that changes ROM bytes must fix the two checksums or the ROM can boot to a black screen:

- game checksum at `0xFFFCC`: sum of 32-bit words from `0` to `0xFFFB0`, skipping the word at `0x18C`;
- header checksum at `0x18E`: sum of 16-bit words from `0x200`.

The original values are `0x10D1310C` and `0xD7B6`. `kit/tools/build_combined.py` recomputes both after every layer. Expected values of the finished builds are in [BUILDING.md](BUILDING.md).
