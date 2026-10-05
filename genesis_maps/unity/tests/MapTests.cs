using System;
using System.Linq;
using BuckRogersGenesis;

static class MapTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; Console.WriteLine("FAIL: " + what); } }

    static int Main(string[] args)
    {
        UnityEngine.Resources.Root = args.Length > 0 ? args[0] : "Resources";

        var maps = BuckRogersMaps.Maps;
        Check(maps.Length == 18, "18 maps");
        Check(string.Join(" ", maps.Select(m => m.id.ToString("X2"))) == "03 10 11 20 23 30 31 32 34 41 42 43 51 52 60 61 62 63", "map ids");
        foreach (var m in maps)
        {
            Check(m.width == 16 && m.height == 16, $"{m.id:X2} size");
            Check(new[] { m.north, m.east, m.south, m.west, m.exists, m.special, m.lock_north, m.lock_east, m.lock_south, m.lock_west }.All(a => a != null && a.Length == 256), $"{m.id:X2} arrays");
        }

        // shared edges agree (north of a cell == south of the cell above, east == west of the right neighbour)
        foreach (var m in maps)
        {
            int agree = 0, total = 0;
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                if (y > 0) { total++; if (m.Wall(x, y, Facing.North) == m.Wall(x, y - 1, Facing.South)) agree++; }
                if (x < 15) { total++; if (m.Wall(x, y, Facing.East) == m.Wall(x + 1, y, Facing.West)) agree++; }
            }
            double r = agree / (double)total;
            Check(r >= (m.id == 0x34 ? 0.95 : m.id == 0x20 ? 0.98 : 0.995), $"{m.id:X2} edge agreement {r:F3}");
        }

        // lock symmetry: 1,040 of 1,060 locked sides have the matching lock on the other side (ROM analysis)
        int locked = 0, sym = 0;
        foreach (var m in maps) for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) foreach (Facing f in Enum.GetValues(typeof(Facing)))
        {
            if (m.Lock(x, y, f) == 0) continue;
            locked++;
            int nx, ny; if (!m.Neighbor(x, y, f, out nx, out ny)) continue;
            Facing opp = (Facing)(((int)f + 2) & 3);
            if (m.Lock(nx, ny, opp) != 0) sym++;
        }
        Check(locked > 1000 && sym >= 1040, $"lock counts locked={locked} symmetric={sym}");

        // Chicagorg (module 0x10)
        var chi = BuckRogersMaps.MapForModule(0x10);
        Check(chi != null && chi.id == 0x10 && chi.name == "Chicagorg", "module 10 -> Chicagorg");
        Check(chi.EventCode(1, 10) == 0x0B, "start cell (1,10) code 0B");
        Check(chi.EventCode(7, 11) == 2 && chi.CellByte(7, 11) == 0x82 && chi.CellByte(7, 10) == 0x02, "security door cells 0x82 / 0x02");
        var men = BuckRogersMaps.SearchEventAt(0x10, 6, 13);
        Check(men != null && men.summary.Contains("MEN'S ROOM"), "search event (6,13) is the men's room sign");
        var heat = BuckRogersMaps.SearchEventAt(0x10, 4, 3);
        Check(heat != null && heat.summary.Contains("HEAT FROM THE EXPLOSIONS") && !chi.InsideArea(4, 3), "heat zone: event on a void cell");
        var sealed_ = BuckRogersMaps.StepEventsAt(0x10, 7, 11, Facing.North);
        Check(sealed_.Any(e => e.test == "[9AF9]EQ130" && e.summary.Contains("SECURITY DOORS ARE SEALED")), "step event north at (7,11)");
        Check(!BuckRogersMaps.StepEventsAt(0x10, 7, 11, Facing.South).Any(e => e.summary.Contains("SECURITY DOORS")), "no sealed-door step event south at (7,11)");
        Check(BuckRogersMaps.StepEventsAt(0x10, 7, 10, Facing.South).Any(e => e.test == "[9AF9]EQ2" && e.summary.Contains("SECURITY DOORS ARE SEALED")), "step event south at (7,10)");
        var fight = BuckRogersMaps.SearchEventAt(0x10, 14, 9);
        Check(fight != null && fight.summary.Contains("fight: 6x Terrine Warrior + Terrine Leader"), "squad at (14,9) described");
        var ambush = BuckRogersMaps.SearchEventAt(0x10, 11, 10);
        Check(ambush != null && ambush.summary.Contains("once only") && ambush.summary.Contains("fight: 4x Terrine Warrior + Terrine Leader"), "ambush at (11,10) described");
        var door = BuckRogersMaps.SearchEventAt(0x10, 15, 10);
        Check(door != null && door.summary.Contains("TECHNICIAN"), "technician room (15,10)");
        Check(BuckRogersMaps.SearchEventAt(0x10, 0, 0) == null, "no event on an empty void cell");

        // shared map
        Check(BuckRogersMaps.MapForModule(0x53).id == 0x51 && BuckRogersMaps.MapForModule(0x51).id == 0x51, "modules 51/53 share map 51");
        Check(BuckRogersMaps.MapForModule(0x03).id == 0x03 && BuckRogersMaps.EventsForModule(0x03).search.Length == 0, "map 03 has no search events");
        Check(BuckRogersMaps.EventsForModule(0x52).step.Any(s => s.summary.Contains("MAIN EXIT")), "module 52 exit step event");

        // movement: lock blocks even when the wall type is declared passable; open side is passable
        var spy = BuckRogersMaps.MapById(0x20);
        bool foundLocked = false, foundOpen = false;
        for (int y = 0; y < 16 && !(foundLocked && foundOpen); y++) for (int x = 0; x < 16; x++) foreach (Facing f in Enum.GetValues(typeof(Facing)))
        {
            int nx, ny;
            if (!foundLocked && spy.IsLocked(x, y, f) && spy.Neighbor(x, y, f, out nx, out ny))
            { foundLocked = true; Check(!spy.TryStep(x, y, f, out nx, out ny, w => true), $"locked side {x},{y},{f} blocks"); }
            if (!foundOpen && spy.InsideArea(x, y) && spy.Wall(x, y, f) == 0 && spy.Neighbor(x, y, f, out nx, out ny))
            { foundOpen = true; Check(spy.TryStep(x, y, f, out nx, out ny) && nx == x + GenesisMap.DX[(int)f] && ny == y + GenesisMap.DY[(int)f], "open side steps"); }
        }
        Check(foundLocked && foundOpen, "found locked and open sides on map 20");
        int ox, oy;
        Check(!chi.TryStep(0, 0, Facing.North, out ox, out oy) && !chi.TryStep(15, 15, Facing.East, out ox, out oy), "map edge blocks");

        Console.WriteLine($"{checks - fails}/{checks} checks passed");
        return fails == 0 ? 0 : 1;
    }
}
