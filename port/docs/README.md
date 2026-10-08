# Buck Rogers: Countdown to Doomsday - combat engine port: documentation

These pages explain how the Genesis combat engine was ported to C# (`port/csharp`) and how every piece was proven correct against the original ROM.
Start with the method page; the rest are by topic.

| Page | Topic |
|---|---|
| [01-method.md](01-method.md) | How a 68000 routine becomes verified C#: emulator, vectors, differential tests, mutation checks |
| [02-computer-turn.md](02-computer-turn.md) | The monster / AI turn: targets, weapons, movement, attacks, reactions, explosives, healer rescue, special effects |
| [03-player-turn.md](03-player-turn.md) | The player-controlled turn, the character sheet / inventory, the yes/no prompt |
| [04-fight-lifecycle.md](04-fight-lifecycle.md) | Start of a fight (battlefield, deployment, control) and the round loop |
| [05-quirks.md](05-quirks.md) | Bugs and oddities of the original that the port reproduces on purpose |
| [06-status-and-gaps.md](06-status-and-gaps.md) | What is verified, what is not ported, how to run the tests |

The older, per-routine reference tables are in `port/README.md`.
| [findings.md](findings.md) | Effect ids 0x0A, 0x0B, 0x10, 0x12, 0x13: on the stage lists, but their handlers are empty |
