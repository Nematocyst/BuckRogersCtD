# Method: porting by differential testing

## The idea
The ROM is the specification. Rather than reading assembly and trusting my reading, every ported routine is run **side by side with the real ROM code** on thousands of random inputs and the results must match byte for byte.

## The pieces
* **Emulator harness** (`tools/emu68k.py`): the 1 MB ROM is loaded into a Unicorn 68000 emulator with the work RAM mapped at 0xFFFF0000 (and its 0x00FF0000 mirror). `call(addr, d0=..., a3=...)` runs a routine like a subroutine until its `rts`.
  `stub_rts` replaces a routine by an empty one, `stub_ret` by one that returns a fixed value, `stub_fn` by one that returns a scripted value (menu answers, pad readings).
* **World builders** (`tools/monster_world.py`): random but believable fights - party and monsters, records of 214 bytes, the 21x21 map with consistent occupancy markers, items, effects. `sane_world` is the workhorse.
* **Vector generators** (`tools/gen_monster_vectors.py`, sections A..V): each section builds a world, writes it into emulator RAM, runs the ROM routine, and stores the *before* and *after* state (slots, records, tiles, scratch globals, target list, RNG table and index, patches, money...) in a gzip JSON file.
* **C# tests** (`tests/MonsterTests.cs`): rebuild the same world from the "before" state, run the port, and compare everything with the "after" state. The RNG table and index are compared too, so a port that rolls even one die too many or too few is caught.
* **Event traces**: the emulator logs when certain routines are entered (`select`, `enum`, `step`, `prep`, `exec`, `turn aN`...); the port logs the same names. When a whole turn or fight differs, the first differing token shows where the paths split. With `TRACE_LOF=1` the traces also record every random draw.

## How a routine was ported (the loop)
1. Disassemble it (own disassembler script) and find what it reads and writes: registers in, RAM bytes in/out, called helpers.
2. Write the C# with the same structure, naming each ROM address in a comment.
3. Add a generator section that calls the ROM routine on random worlds, with every graphics/sound/animation routine stubbed out.
4. Run; read the first failing check (offset, port value / ROM value); fix; repeat until zero differences.
5. **Mutation check**: deliberately break one line of the port (an off-by-one constant, a flipped condition) and confirm the tests fail. If they do not, the generator does not exercise that line, so the world builder is improved (e.g. members sharing items so stacks merge; monsters with the ally flag).

## Recurring problems and how they were solved
| Problem | Resolution |
|---|---|
| UI routines write into the same RAM as game state (scratch at 0xD48E..0xD5FF) | Stub the UI routines; for the few UI bytes that still differ (menu layout, text cursor) the comparison masks exactly those bytes, per test type. |
| The emulator aborted on the first translated block of one routine | A short priming call before every run. |
| Uninitialised registers / stack in the ROM influence results (e.g. d2 upper bits, stack slots) | Reproduce where it matters and is deterministic (a queue of "garbage" values recorded from the ROM run); where it is truly arbitrary, define the port's behaviour (start at 0) and make the test start the ROM the same way. |
| A test passed because both sides were stubbed alike | Un-stub one level at a time; when a UI routine turned out to carry state (scroll routine moving the cursor, pad reads inside AI turns), port that effect. |
| Long runs are expensive | Generators accept `N_<section>` counts and range filters so one failing case can be regenerated alone; failing cases are traced and diffed token by token. |
