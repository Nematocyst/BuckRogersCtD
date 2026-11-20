# Building the release from the original ROM

The release patches are built from the original ROM by a short chain of scripts in [`kit/`](../kit/). Every step starts from a fresh copy of the original ROM, applies changes in order, and recomputes both checksums once at the end.

## Requirements

- Python 3 (standard library only; no packages to install) and a POSIX `sh`.
- A copy of the original ROM, `Buck Rogers - Countdown to Doomsday (USA, Europe)`: 1,048,576 bytes, CRC32 `44E3BFFF`, MD5 `f768fbba021636fff3682b9ae116076a`, SHA-1 `89c39f00745f2a8798fe985ad8ce28411b977f9e`. It must be unheadered (not `.smd`) and not interleaved. `build_combined.py` refuses any input whose size or game checksum is wrong, so a patched or headered ROM is caught at layer 1.
- Keep the ROM outside the repository. `kit/build/` and files named `ORIGINAL*` are git-ignored, but it is safer not to put ROMs anywhere in the working tree.

## Rebuild the two main patches

```
cd kit
sh build_release.sh /path/to/ORIGINAL.md           # build/release.ips        (original level cap)
sh build_release.sh /path/to/ORIGINAL.md level9    # build/release_level9.ips (level cap 9)
```

Each run also leaves the patched ROM (`build/release.md`, `build/release_level9.md`) and intermediate files (`build/a.*`, `b.*`, `c.*`) in `kit/build/`.

### What the script does

`build_release.sh` applies these layers in order. Each is one script run, and the output of one is the input of the next:

| # | Layer | Done by |
| --- | --- | --- |
| 1 | Base restoration (`restoration_v9_clean.ips`, or `_level9`) | `build_combined.py --ips` |
| 2 | Rocket special fix: byte `0x10892` = `0x60` | `build_combined.py --byte` |
| 3 | Quick Combat weapon-choice fix: byte `0xE95D` = `0x00` | `build_combined.py --byte` |
| 4 | Shop comparison line (`shop_compare_only.ips`) | `build_combined.py --ips` |
| 5 | Character creation fix (`creation_fix_code_only.ips`) | `build_combined.py --ips` |
| 6 | Venusian and Lunarian rocket pistols | `add_racial_rockets.py` |
| 7 | Armor clean-up in the shop lists | `dedupe_armor.py` |
| 8 | Courier reward (script edit, module `0x63`) | `build_combined.py --ecl` |

Layers 1–5 are one `build_combined.py` run. Layers 6 and 7 read the previous step's IPS file, apply it to the original ROM, add their own change and write a new IPS. Layer 8 is another `build_combined.py` run on top.

## Rebuild the add-ons

The add-ons are built against both main patches, so run both commands above first. Then, still inside `kit/`:

```
python3 tools/build_addons.py /path/to/ORIGINAL.md
```

This writes six patches to `kit/build/addons/` (cut weapons, difficulty, and both together, each in a normal and a `_level9` version). Each file is verified when it is written: release plus add-on must equal the target ROM, the game checksum must be valid, and the racial rocket pistols must still be in the New Elysium and Tycho lists.

Apply an add-on only on top of the main patch of the same variant, and apply only one add-on. Each add-on carries its own checksum fix, so two of them applied one after the other leave a wrong checksum. The "both" add-on exists for that reason.

## Check the result

The released files should be reproduced byte for byte. Compare your build with the files in `BR_Restored_Improved_v1.0_patches.zip`:

```
cmp kit/build/release.ips        BR_Restored_Improved_v1.0.ips
cmp kit/build/release_level9.ips BR_Restored_Improved_v1.0_level9.ips
```

(Unzip the release first. The release README says both builds were verified byte-identical to the play-tested files when v1.0 was built, on 2026-10-12.)

If you cannot compare files, check the checksum bytes of the patched ROM instead. The game checksum is the 4 bytes at `0xFFFCC` and the header checksum is the 2 bytes at `0x18E`:

| Build | Game checksum (`0xFFFCC`) | Header (`0x18E`) |
| --- | --- | --- |
| Main patch | `91 C8 12 FA` | `A2 0F` |
| Main patch, level9 | `AB B8 13 05` | `D6 09` |
| Add-on cut weapons | `A5 98 F1 0A` | `85 CD` |
| Add-on cut weapons, level9 | `BF 88 F1 15` | `B9 C7` |
| Add-on difficulty | `3A 83 8F 75` | `EE 0F` |
| Add-on difficulty, level9 | `54 73 8F 80` | `22 09` |
| Add-on cut weapons + difficulty | `4E 54 6D 85` | `D1 CE` |
| Add-on cut weapons + difficulty, level9 | `68 44 6D 90` | `05 C8` |

These values were read from the released patch files. For the two difficulty add-ons the patch changes only one header byte, so the other is taken from the main patch.

## Making your own change

`tools/build_combined.py` is the general tool. It applies any mix of components to a fresh ROM, in command-line order, and writes the patched ROM and an IPS file:

```
python3 tools/build_combined.py ORIGINAL.md OUT.md OUT.ips [components...]
```

| Component | Meaning |
| --- | --- |
| `--ips FILE` | Apply an IPS patch. Its writes to the checksum bytes are ignored. |
| `--byte ADDR=VAL` | Write one byte (hex or decimal). |
| `--level9` | Raise the level cap to 9. |
| `--weapon "ITEM:FIELD=VALUE"` | Edit the weapon/armor stat table, e.g. `"laser pistol:sides=10"`. |
| `--monster "EDIT"` | Edit a monster: `"NAME=32"` (HP), `"NAME*1.5"` (scale HP), `"NAME:gear1=heavy body armor"`, `"NAME@37=60"` (raw record byte). |
| `--ecl "EDIT"` | Edit a game script: `"10:6D4E+=SAVE 1,[9DC1]"` inserts instructions, `"10:6D3F=00 05"` overwrites bytes. |
| `--allow-override` | Let a later component overwrite a byte an earlier one changed. |

The builder remembers which component changed each byte. If a later component changes the same byte to a different value, the build stops and names both components, unless you pass `--allow-override`. The checksum bytes belong to nobody and are recomputed at the end.

Other tools in `kit/tools/` (each documents its usage in its header):

| Tool | Purpose |
| --- | --- |
| `build_weapon_patch.py` | Standalone weapon/armor stat edits |
| `build_monster_patch.py` | Standalone monster edits; also holds the checksum and IPS helpers the other tools import |
| `ecl_disasm.py` | Disassemble the game's 27 script modules into text listings |
| `ecl_patch.py` | Library behind `--ecl` |
| `decoder.py`, `decoder2.py`, `encoder.py` | Decompress and recompress the game's data (see [ROM-FORMATS.md](ROM-FORMATS.md)) |
| `ips.py` | Minimal IPS apply and diff helpers |

For what the addresses and structures mean, see [ROM-FORMATS.md](ROM-FORMATS.md) and [SHOPS.md](SHOPS.md).
