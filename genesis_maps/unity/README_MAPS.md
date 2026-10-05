# Genesis maps for Unity

1. Copy `Resources/BuckRogers/maps.json` and `map_events.json` into `Assets/Resources/BuckRogers/` (next to the other
   BuckRogers JSON files) and `BuckRogersMaps.cs` anywhere under `Assets/`. It uses the same namespace as
   `BuckRogersData.cs` (`BuckRogersGenesis`) and the same `Resources` loading pattern.
2. Use it:
```csharp
using BuckRogersGenesis;
var map = BuckRogersMaps.MapForModule(0x10);                  // Chicagorg (module 0x53 shares map 0x51)
int wall   = map.Wall(5, 13, Facing.North);                   // wall type 0..14, 0 = open
bool lockd = map.IsLocked(5, 13, Facing.North);               // door lock (plane 3)
var ev     = BuckRogersMaps.SearchEventAt(0x10, 6, 13);       // event on that cell, ev.summary / ev.texts
var steps  = BuckRogersMaps.StepEventsAt(0x10, 7, 11, Facing.North);   // "doors sealed" style step events
if (map.TryStep(x, y, Facing.East, out int nx, out int ny, w => w == 2 /* your door types */)) { ... }
```

Conventions: 16x16, index `y*16+x`, y grows south, facing 0 N / 1 E / 2 S / 3 W (the scripts' `[9AFA]`).
`special & 0x3F` is the event code; `exists` is plane-2 bit 7 (inside the explorable area; void cells can carry events).
Not decoded yet: what each wall type 1..14 means (wall, door, secret door ...), so `TryStep` only lets wall type 0
through unless you pass a predicate.

## Tests (no Unity needed)
`mono-mcs` compiles the loader against a tiny UnityEngine stub:
```
cd genesis_maps/unity
mcs -out:tests/maptests.exe BuckRogersMaps.cs tests/UnityStub.cs tests/MapTests.cs && mono tests/maptests.exe Resources
```
76 checks: map list, array sizes, shared-edge agreement, lock symmetry, Chicagorg event lookups (men's room sign, heat zone on void
cells, sealed security doors by facing, ambush descriptions), shared map 0x51, lock-blocked and edge-blocked steps.

Regenerate the JSON: `python tools/genesis_maps/export_unity_maps.py genesis_maps/genesis_maps.json genesis_maps/map_events.json OUT`.
Note: `mcs` is C# 7.0, so the loader uses `default(T)` instead of the `default` literal (also fine in Unity).
