# Buck Rogers: Countdown to Doomsday (Genesis) — Restoration v9 (cleaned shops)

Apply to a fresh, unmodified USA/Europe ROM.
- `restoration_v9_clean.ips`: check bytes `B1 04 03 51` at 0xFFFCC, `BD CD` at 0x18E.
- `restoration_v9_clean_level9.ips` (adds the level-9 cap): `CA F4 03 5C` / `F1 C7`.

## Changes from v8
1. **Removed 18 shop entries that never actually appeared.** The Genesis shop code drops or swaps whole weapon kinds
   (knives, sonic stunners, swords, bolt guns, rocket rifles, behind-cover guns, laser rifles, polearms) for every race,
   so these v8 additions showed up as nothing or as a plain base item (see SHOP_ITEM_TEST.md):
   - Hielo: Mercurian Polearm, Behind-Cover Gun, Laser Rifle
   - New Elysium: Venusian Mono Knife, Sword, Bolt Gun, Laser Rifle
   - Tycho: Lunarian Knife, Bolt Gun, Laser Rifle, Sonic Stunner
   - Psyche: Martian Mono Knife, Sword, Bolt Gun, Behind-Cover Gun, Laser Rifle, Rocket Rifle, Sonic Stunner
2. **Demo Charges** (100 credits) at Hielo, New Elysium, Tycho and Juno.
3. **New item: Lunarian Needle Gun** (tier 4, 3,360 credits) at Tycho. The item table was moved to 0xF2200 to make room.
Nothing original was removed; all racial armor from v8 is unchanged.

## Verified
Each changed shop opened in the emulator stocks exactly its list (Hielo 9, New Elysium 8, Tycho 9, Psyche 6 items);
both ROMs boot to the team screen. Built with add_lunarian_needle.py + clean_shops.py.
