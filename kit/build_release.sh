#!/bin/sh
# Rebuild the full release from the ORIGINAL ROM.  sh build_release.sh ORIGINAL.md [level9]
# Chain: v9 base -> rocket fix (0x10892) -> AI fix (0xE95D) -> shop line -> creation fix -> racial rocket pistols
#        -> armor clean-up -> courier reward (script edit). Each step recomputes both checksums.
set -e
R="$1"; L=""; [ "$2" = "level9" ] && L="_level9"
T=tools; mkdir -p build
python3 $T/build_combined.py "$R" build/a.md build/a.ips --ips restoration_v9_clean$L.ips \
  --byte 0x10892=0x60 --byte 0xE95D=0x00 --ips shop_compare_only.ips --ips creation_fix_code_only.ips
python3 $T/add_racial_rockets.py "$R" build/b.md build/b.ips --ips build/a.ips
python3 $T/dedupe_armor.py "$R" build/c.md build/c.ips --ips build/b.ips
python3 $T/build_combined.py "$R" build/release$L.md build/release$L.ips --ips build/c.ips \
  --ecl '63:7C95+=PRINTCLEAR T11C4; CONTINUE; PRINTCLEAR T"NEO SENDS ITS THANKS."; CONTINUE; ADDEP 1, #1000; RETURN'
