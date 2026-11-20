# Components of the build

The release is assembled from small patches and scripted edits. This page lists what each file in [`kit/`](../kit/) is, so you can reuse a piece or replace it. How to run the build is in [BUILDING.md](BUILDING.md).

## Patch files in `kit/`

All of them apply to the original ROM unless noted.

| File | What it is | Used by the release build |
| --- | --- | --- |
| `restoration_v9_clean.ips` | Base layer: racial weapons and armor in shops, Demo Charges, the new Lunarian Needle Gun, and the item table and shop lists moved to make room (`0xF2200`, `0xF2000`) | yes, layer 1 |
| `restoration_v9_clean_level9.ips` | The same, with the level cap raised to 9 | yes, layer 1 (level9 build) |
| `shop_compare_only.ips` | Shop comparison line: hook at `0x16ABE`, code at `0xF5000`–`0xF51F7` | yes, layer 4 |
| `creation_fix_code_only.ips` | Character creation fix: jumps at `0x6A8` and `0x6DC` | yes, layer 5 |
| `restoration_v9_difficulty.ips`, `_level9` | Difficulty changes as a standalone patch against the v9 base | only to derive the difficulty add-on |
| `restoration_v11.ips`, `_level9` | Cut weapons as a standalone patch against the v9 base | only to derive the cut weapons add-on |

The other layers (rocket fix, Quick Combat fix, racial rocket pistols, armor clean-up, courier reward) are not patch files. `build_release.sh` produces them with a byte write, a script or a script edit. What each layer changes and where is in the Technical notes of the [v1.0 README](../BR_Restored_Improved_v1.0_README.md).

## How the add-ons are derived

The add-ons were first made as standalone patches against the v9 base. Because the release changes some of the same bytes, `build_addons.py` rebuilds them to apply on top of the release instead:

- **Difficulty:** 2,829 bytes differ from the v9 base, and none overlap the release. The add-on is the difference between (release + difficulty changes) and the release.
- **Cut weapons:** 49 bytes differ from the v9 base, and 4 of them overlap the release, all in shop lists (`0xF2080`, `0xF2089`, `0xF20C0`, `0xF20CA`). The release's rocket and armor steps also rewrite those lists. The builder therefore re-adds the cut weapons to the release's lists rather than copying bytes, and checks that no list overflows.
- **Both together:** the two add-ons change different parts of the ROM, so they combine. They are shipped as one file because each add-on's checksum fix is only correct on its own.

## Standalone tools worth knowing

- `build_weapon_patch.py` edits one field of the stat table at a time, for example `"laser pistol:sides=10"`. See [ROM-FORMATS.md](ROM-FORMATS.md#weapon-and-armor-stat-table) for the fields.
- `build_monster_patch.py` edits monster HP and equipment. Its docstring lists the edit syntax.
- `ecl_disasm.py ROM.md OUTDIR` writes one listing per script module plus a `SUMMARY.txt`. These listings are generated from the ROM and are not stored in this repository.

## Release history

| Version | State |
| --- | --- |
| v6, v8 | superseded; see [`archive/`](../archive/) |
| v9 | the base layer (`restoration_v9_clean`) is still the foundation of v1.0 |
| v1.0, 2026-10-12 | current. Main patch played through; the difficulty add-on is untested |
