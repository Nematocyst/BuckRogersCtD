# Buck Rogers: Countdown to Doomsday (Genesis) — Restored Weapons Patch

**Patch file:** `racial_armories_plus_repairs_v6.ips`
Apply to a fresh, unmodified copy of the USA/Europe Genesis ROM.

## What this patch does

The original game's data files contain a full set of racial weapon variants
(Martian, Venusian, Mercurian, and Lunarian versions of most weapons) that
were never actually placed in any shop. This patch adds those unused-but-complete
items to four shops, one per race, and fixes two items whose names were
stored incorrectly in the original game and would have displayed wrong.

Nothing is removed. Every item each shop already sold is still there.

## New items by shop

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

### Unnamed Martian outpost shop (shop #8) — Martian Armory
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

*Already sold here: Martian Mono Sword, Martian Laser Pistol.*

This shop's in-game town name wasn't identified during this project — if you
recognize it, it's worth noting for next time.

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

## Two items repaired

Two existing item records had the right stats and price but the wrong name
stored — almost certainly a data-entry mistake from the original 1991
development, not intentional. Both now display and function correctly:

- **Martian Heat Gun** (previously showed as "Martian sword gun")
- **Mercurian Mono Sword** (previously showed as "Mercurian mono bolt")

## What's still missing, and why

Not every race has every weapon or armor piece — this patch restores
everything that exists *and works* in the game's data, but a few
combinations were never created by the original developers at all (no
broken record, nothing to fix, just genuinely absent from the ROM):

- No Mercurian Mono Sword existed as a *separate* combination beyond the one repaired above
- No Lunarian Needle Gun or Lunarian Rocket Pistol, in any form
- No Venusian, Mercurian, or Lunarian Cutlass beyond what's listed above
- Armor: no Lunarian Battle Armor, and no Venusian, Mercurian, or Lunarian
  Battle Armor w/Fields (Martian is the only race with the complete 5-piece
  armor line — restoring the rest across shops is a possible next step, not
  included in this patch)

## Installation

1. Get a copy of the original, unmodified Genesis ROM
   (`Buck Rogers - Countdown to Doomsday (USA, Europe)`).
2. Apply `racial_armories_plus_repairs_v6.ips` using any IPS patcher
   (e.g. the browser-based RomPatcher.js, or Lunar IPS).
3. To confirm the patch applied correctly, the bytes at ROM offset
   `0xFFFCC` should read `8E 98 17 F6`, and the header checksum at `0x18E`
   should read `A1 5F`.
