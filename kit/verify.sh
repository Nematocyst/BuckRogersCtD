#!/bin/sh
# One command: rebuild everything from the original ROM and compare it with the released patches.
#   cd kit && sh verify.sh /path/to/ORIGINAL.md [/path/to/BR_Restored_Improved_v1.0_patches.zip]
# Prints PASS or FAIL for each of the 8 released files and exits non-zero if any differ.
# The zip defaults to the one in the repository root.
set -e
R="$1"; Z="${2:-../BR_Restored_Improved_v1.0_patches.zip}"
[ -f "$R" ] || { echo "usage: sh verify.sh ORIGINAL.md [patches.zip]   (original ROM not found: '$R')"; exit 2; }
[ -f "$Z" ] || { echo "release zip not found: $Z"; exit 2; }
sh build_release.sh "$R" > /dev/null
sh build_release.sh "$R" level9 > /dev/null
python3 tools/build_addons.py "$R" > /dev/null
REL=build/released; rm -rf "$REL"; mkdir -p "$REL"
python3 -I -c "import sys, zipfile; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" "$Z" "$REL"
fail=0
check() {  # built-file released-file
    if cmp -s "$1" "$REL/$2"; then echo "PASS  $2"; else echo "FAIL  $2"; fail=1; fi
}
check build/release.ips        BR_Restored_Improved_v1.0.ips
check build/release_level9.ips BR_Restored_Improved_v1.0_level9.ips
for a in addon_cut_weapons addon_difficulty addon_cut_weapons_difficulty; do
    check "build/addons/$a.ips"        "$a.ips"
    check "build/addons/${a}_level9.ips" "${a}_level9.ips"
done
[ "$fail" = 0 ] && echo "All 8 released files reproduced byte for byte." || echo "Some files differ from the release."
exit $fail
