# Buck Rogers: Countdown to Doomsday (Genesis) — Restored Weapons & Armor Patch

**Current patch file:** `full_restoration_v8.ips`
Apply to a fresh, unmodified copy of the USA/Europe Genesis ROM.

To confirm the patch applied correctly: the bytes at ROM offset `0xFFFCC` should read `F3 E1 65 46`, and the header checksum at `0x18E` should read `06 90`.

## What this patch does

The original game's data files contain a full set of racial weapon and armor variants (Martian, Venusian, Mercurian, and Lunarian versions of most equipment) that were never actually placed in any shop. This patch adds that unused-but-complete content to real, existing shops — one weapon armory per race, plus two shops carrying the racial armor lines — and fixes two items whose names were stored incorrectly in the original game.

Nothing original is removed. Every item a shop already sold is still there.

## Changelog since v6

If you tried an earlier version of this patch, here's exactly what changed:

- **v7 added:** the racial armor lines — Martian and Venusian armor to Aurora (shop 14), Mercurian and Lunarian armor to Juno (shop 12). These didn't exist in v6 at all.
- **v8 fixed a real bug:** v6/v7 added the 10 Martian weapons (and the repaired Martian Heat Gun) to "shop 8" — but shop 8 turned out **not to be a real, walk-up shop**. It's a monster loot table (what RAM Assassins and Stage 3 ECGs drop when defeated), reusing the same internal data format as a real shop, which is what fooled the earlier version of this patch. In v8, shop 8 is restored to its exact original contents, and the Martian weapon line moves to **Psyche** (shop 11) instead — which already legitimately sold "Martian Laser Pistol" before this patch touched anything.

If you're upgrading from v6 or v7, re-apply v8 to a **fresh, unmodified** ROM rather than patching on top of an old one.

## New items by shop

### Psyche — Martian Armory
| Item | Price |
|---|---|
| Martian Mono Knife | 400 |
| Martian Cutlass | 200 |
| Martian Sword | 300 |
| Martian Needle Gun | 420 |
| Martian Bolt Gun | 420 |
| Martian Rocket Pistol | 1,000 |
| Martian Behind-Cover Gun | 800 |
| Martian Laser Rifle | 1,270 |
| Martian Rocket Rifle | 1,200 |
| Martian Sonic Stunner | 760 |
| Martian Heat Gun *(repaired — see below)* | 1,000 |

*Already sold here: Laser Pistol, Martian Laser Pistol.*

### Hielo — Mercurian Armory
| Item | Price |
|---|---|
| Mercurian Needle Gun | 1,610 |
| Mercurian Cutlass | 800 |
| Mercurian Polearm | 960 |
| Mercurian Behind-Cover Gun | 2,850 |
| Mercurian Laser Pistol | 2,680 |
| Mercurian Laser Rifle | 5,080 |
| Mercurian Rocket Pistol | 4,000 |
| Mercurian Mono Sword *(repaired — see below)* | 8,000 |

*Already sold here: Heavy Body Armor, Protective Goggles, Mercurian Heat Gun.*

### New Elysium — Venusian Armory
| Item | Price |
|---|---|
| Venusian Mono Knife | 800 |
| Venusian Sword | 600 |
| Venusian Mono Sword | 4,000 |
| Venusian Needle Gun | 840 |
| Venusian Bolt Gun | 840 |
| Venusian Laser Rifle | 2,540 |
| Venusian Heat Gun | 2,000 |

*Already sold here: Venusian Laser Pistol, Smartsuit, Protective Goggles, Poison Antidote.*

### Tycho — Lunarian Armory
| Item | Price |
|---|---|
| Lunarian Knife | 160 |
| Lunarian Bolt Gun | 3,360 |
| Lunarian Laser Pistol | 5,360 |
| Lunarian Laser Rifle | 10,160 |
| Lunarian Heat Gun | 8,000 |
| Lunarian Sonic Stunner | 6,080 |

*Already sold here: Lunarian Mono Sword, Poison Antidote, Stun/Dazzle/Aerosol Mist Grenades.*

### Aurora — Martian & Venusian Armor
| Item | Price |
|---|---|
| Martian Spacesuit | 1,000 |
| Martian Smartsuit | 2,400 |
| Martian Heavy Body Armor | 3,400 |
| Martian Battle Armor | 5,000 |
| Martian Battle Armor w/Fields | 6,000 |
| Venusian Spacesuit | 2,000 |
| Venusian Smartsuit | 4,800 |
| Venusian Heavy Body Armor | 6,800 |
| Venusian Battle Armor | 10,000 |

*Already sold here: Spacesuit, Smartsuit, Battle Armor w/Fields, Protective Goggles.*

### Juno — Mercurian & Lunarian Armor
| Item | Price |
|---|---|
| Mercurian Spacesuit | 4,000 |
| Mercurian Smartsuit | 9,600 |
| Mercurian Heavy Body Armor | 13,600 |
| Mercurian Battle Armor | 20,000 |
| Lunarian Spacesuit | 8,000 |
| Lunarian Smartsuit | 19,200 |
| Lunarian Heavy Body Armor | 27,200 |

*Already sold here: ECM Package, Stun/Dazzle/Chaff/Aerosol Mist Grenades.*

## Two items repaired

Two existing item records had the right stats and price but the wrong name stored — almost certainly a data-entry mistake from the original 1991 development, not intentional. Both now display and function correctly:

- **Martian Heat Gun** (previously showed as "Martian sword gun")
- **Mercurian Mono Sword** (previously showed as "Mercurian mono bolt")

## Availability, not just existence

Every item added by this patch was checked against community-documented drop/sale data before being placed. Of the 16 restored armor pieces specifically:
- 4 also had a pre-existing drop or one-time-find source in the original game (Martian Smartsuit, Martian Heavy Body Armor, Venusian Heavy Body Armor, Lunarian Smartsuit) — the shop is a second, easier way to get these; their original source still works too.
- The other 12 aren't documented as obtainable through any means in the unmodified game — this patch is very likely the only way to get them at all.

## What's still missing, and why

Not every race has every weapon or armor piece — this patch restores everything that exists *and works* in the game's data, but a few combinations were never created by the original developers at all (no broken record, nothing to fix, just genuinely absent from the ROM):

- No Lunarian Needle Gun or Lunarian Rocket Pistol, in any form
- No Venusian, Mercurian, or Lunarian Cutlass beyond what's listed above
- No Lunarian Battle Armor, and no Venusian, Mercurian, or Lunarian Battle Armor w/Fields (Martian is the only race with the complete 5-piece armor line)

## Shop directory, for reference

| Shop | Identity |
|---|---|
| Salvation, Hielo, New Elysium, Tycho, Pavonis, Psyche, Juno, Hygeia, Aurora, Thule | Real, confirmed shops |
| Two other shops (unnamed) | Real shops, never touched by this patch |
| Three shop-numbered slots | Not real shops — monster loot tables reusing the same data format (left untouched) |

## Installation

1. Get a copy of the original, unmodified Genesis ROM (`Buck Rogers - Countdown to Doomsday (USA, Europe)`).
2. Apply `full_restoration_v8.ips` using any IPS patcher (e.g. the browser-based RomPatcher.js, or Lunar IPS).
3. Confirm the patch applied correctly (see the checksum values at the top of this document).
