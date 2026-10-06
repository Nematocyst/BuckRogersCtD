# Quirks of the original reproduced on purpose

Found by the tests (a difference that survived a careful reading is usually a real ROM quirk). Keeping them makes recorded fights identical; they are listed so a Unity version can decide to fix them.

* **Rocket "full damage"** (0xFF) never hurts anyone; and a normal hit of 128+ is dropped (reachable only with 128+ HP).
* **Whole-turn flag**: "time was 1" lives in register d6, which the target enumeration overwrites, so almost every turn takes the "has waited" ending.
* **Bogus first path step**: the search marks the start cell, so a creature at the map edge can try to step off the map.
* **Weapon choice** tests the actor's tile at x*21+y (everything else y*21+x).
* **Damage list overlap**: 9+ hits overwrite the damage multiplier and side modifier globals.
* **Explosives scatter** without running the to-hit preparation first.
* **Inventory**: the member-can-take-it check uses a leftover item pointer (backpack slot 8 when a slot 9..12 item is picked); handing a single item to a member who has a stack overwrites the stack with count 1; a hand-over with no room writes into ROM (ignored) and loses the item; moving a slow weapon into the hand costs the turn; money from a sale is truncated to 16 bits.
* **Battlefield generator**: the random radius is a divide by the signed byte 0x81, and the square root can run on a negative number.
* **Manual turn**: two frame variables (explosive-in-hand flag, target index) are uninitialised stack in the ROM; the port starts them at 0.
* **AI turns read the pad**: pressing cancel during a computer turn hands the party to the player.
