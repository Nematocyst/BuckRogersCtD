# The player side: manual turn, sheet and inventory, prompts

Code: `GenesisPlayerTurn.cs`, `GenesisInventory.cs`. All input comes through callbacks that stand for the ROM's input routines, so a Unity UI can drive the same state machines: `Pad` (control pad reading), `MenuChoice`, `InventoryMenu`, `AskQuantity`, `SheetMenu`, `CharacterSheet`, `RetreatPrompt`, `ContinuePrompt`.

## The manual turn (`PlayerTurn`, ROM 0xF2AE)
A small state machine over the modes *menu*, *walk*, *attack*, *rescue*.
* **Approach**: the routine was read first for its *structure* (menu table, mode variable, saved position for undo, target index, cursor), then written as a state machine mirroring the ROM's jumps. The test drives the ROM with scripted menu answers and pad bytes and replays the same script through the port; the number of inputs consumed must match as well as the final state.
* **Walk**: a step needs 21 pad readings (directions are or-ed; a diagonal ends the reading at once). Cancel undoes the whole walk (position, movement points, markers) but not the reactions already drawn.
* **Attack**: the cursor moves one cell per press; every stop runs the attack preparation, which already changes state (stats recomputed, target turned). Confirm attacks or throws.
* **Rescue**: the healer chooses a fallen friend with the cursor.
* **Findings from the tests**: cycling to the next target leaves the cursor step vector behind; the AI turn also reads the pad (cancel hands the party over to the player); a scroll routine moves the cursor when a creature acts off screen (`ActorVisible` stands for the camera).

## Character sheet and inventory (ROM 0xFDC8, 0x748C, 0x78D6)
* The stat page and skills page only draw; the rules are in the inventory screen, a menu of 23 cells (13 item slots, drop/sell, leave, 8 party members).
* The ROM **greys out** cells through a list at 0xD564; the port keeps the same list (`CellDisabled`) so a host UI can show the same choices.
* Rules: pick up, then swap slots (slots 9..12 accept only one item class each), drop/sell with quantity, or hand an item to another member (stack merge up to 250, class slot, first free backpack slot). Moving a slow weapon into the hand clears the action time.
* **How the odd parts were decoded**: several tests inside the ROM compared against a register that was really a leftover pointer (weapon-table row of the last item looked at). Reading the call order showed what each leftover held; the quirks are reproduced and documented.
* **Verification**: 1,200 scripted screens; the script chooses only cells the ROM leaves enabled; the prompt maximum is compared too (this caught an untested off-by-one in the merge limit, found by mutation).

## The yes/no prompt (`ChoicePrompt`, ROM 0x136DA)
The "leave the battlefield?" box is one general choice box (messages 1, 6, 7, 8 yes/no, others single choice). Cursor starts on the stored choice, left/right move it, confirm picks, cancel answers -1, demonstration mode takes the preset answer. Verified on 400 off-map steps with random pad scripts.
