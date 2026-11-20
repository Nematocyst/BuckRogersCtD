# BuckRogersCtD

Romhacks, tools and documentation for the Sega Genesis port of *Buck Rogers: Countdown to Doomsday*.

## Current release: Buck Rogers Restored and Improved v1.0

IPS patches that fix combat and character-creation bugs, restore content the developers left unused, add two weapons and a quest reward, and add an item comparison line to the shops. The main patch has been played through to the end.

- **Players:** read [`BR_Restored_Improved_v1.0_README.md`](BR_Restored_Improved_v1.0_README.md), then download [`BR_Restored_Improved_v1.0_patches.zip`](BR_Restored_Improved_v1.0_patches.zip). Pick one main patch and, optionally, one add-on.
- **Required ROM:** `Buck Rogers - Countdown to Doomsday (USA, Europe)`, 1,048,576 bytes, CRC32 `44E3BFFF`. The README lists all checksums.

## What is in this repository

| Path | What it is |
| --- | --- |
| [`BR_Restored_Improved_v1.0_README.md`](BR_Restored_Improved_v1.0_README.md) | Full documentation of the v1.0 release, including a technical section for ROM hackers |
| `BR_Restored_Improved_v1.0_patches.zip` | The released patches: two main patches and six optional add-ons |
| [`kit/`](kit/) | Build kit: the scripts and component patches that rebuild the release from the original ROM |
| [`docs/`](docs/) | Reference for hackers: how to build, ROM data formats, record fields, combat rules, patch mechanisms, scripts and encounters, graphics, shops, open questions |
| [`archive/`](archive/) | Older patch versions (v6, v8, v9), kept for history and superseded by v1.0 |
| [`CREDITS.md`](CREDITS.md) | Credits and acknowledgements |

## For ROM hackers

Start with [`docs/BUILDING.md`](docs/BUILDING.md) to rebuild the release, then [`docs/ROM-FORMATS.md`](docs/ROM-FORMATS.md) for the data structures the tools understand (the game's compressed data, item and shop tables, monster records and the script language). [`docs/COMBAT-RULES.md`](docs/COMBAT-RULES.md) and [`docs/RECORD-FIELDS.md`](docs/RECORD-FIELDS.md) describe how fights work. [`docs/OPEN-QUESTIONS.md`](docs/OPEN-QUESTIONS.md) lists what is still unknown or untested.

## License & Disclaimer

This project is licensed under the **MIT License**—feel free to use, modify, and distribute these patches and tools in your own projects.

**Disclaimer:** This project is an unofficial fan modification. It is not affiliated with, authorized, or endorsed by Electronic Arts (EA), TSR/Wizards of the Coast, or Sega. *Buck Rogers: Countdown to Doomsday* is a trademark of its respective owners.

* **No ROMs are hosted here.** This repository contains only `.ips` patch files, scripts and documentation.
* To use the patches, you must supply your own legally acquired Sega Genesis ROM of the game.
* The build scripts need your own copy of the original ROM and write their output to `kit/build/`, which is excluded from version control. Do not commit built ROMs.
