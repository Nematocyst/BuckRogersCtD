# Creation fix: Human / Tinker reset the desert-runner claws

**Bug (original ROM).** On the creation screen the desert runner choice sets the natural attack to 1d3 (+0x2A = 3) with a +1 damage bonus (+0x2C = 1). Choosing Human afterwards runs
`clr.b $0C(a2)` (ROM 0x6A8, a typo for `$2C`: it clears name byte 12), and Tinker clears only +0x2C, so the character kept the claws bonus and/or 1d3 fists.

**Fix.** The two handlers now call a 14-byte routine in free ROM that clears +0x2C and sets +0x2A = 2 (the fresh-record value, ROM 0x5A0). Warriors and desert runners still get 1d3 from the
creation finish handler (0x810), as in the original; the desert runner handler is untouched.

| ROM offset | original | patched |
|---|---|---|
| 0x6A8 (human) | `42 2A 00 0C 70 01` | `4E B9 00 0F 60 00` (jsr 0xF6000) |
| 0x6DC (tinker) | `42 2A 00 2C 70 03` | `4E B9 00 0F 60 0E` (jsr 0xF600E) |
| 0xF6000 (free, was FF) | | `42 2A 00 2C 15 7C 00 02 00 2A 70 01 4E 75` and the same with `70 03` |

Apply `creation_fix.ips` to the original USA/Europe ROM. It does not overlap the restoration v8/v9 patches or the shop-compare patch (offsets 0x6A8, 0x6DC, 0xF6000..0xF601B), so it can be applied on top of them.
The ROM header checksum is not updated (like the other patches in this repository).

`python3 build_patch.py ROM out.ips` rebuilds the patch (it checks the original bytes first); `python3 test_creation_fix.py ROM` runs seven race-switch sequences in the emulator on the original and the patched ROM
and checks that only +0x0C, +0x2A and +0x2C differ (not yet play-tested on hardware or in a full emulator).
