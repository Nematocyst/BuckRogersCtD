| shop_compare.s -- hover comparison line for the shop's buy screen (and nothing else).
| Hook: ROM 0x16ABE (bsr 0x11C4C ; bra.b 0x16AC8, six bytes) becomes  jmp 0xF5000.
| On entry a3 = the pool item under the cursor (10 bytes: id,..,+4 tier,..,+8 qty,+9 type); the routine returns by jmp 0x16AC8.
        .text
        .globl  start
start:
        jsr     (0x11C4C).l             | the instruction the hook replaced: print the price line
        movem.l d0-d7/a0-a5,-(sp)
        movea.l a3,a4                   | a4 = item
        movea.l a4,a2
        jsr     (0x6E70).l              | a3 = weapon table row of the item (a2 = item)
        movea.l a3,a5                   | a5 = row
        jsr     (0x6EEC).l              | a3 = slot, a2 = record of the character the screen is showing
        movea.l a3,a0                   | a0 = slot
        lea     (0xFFFFD5AE).l,a1       | a1 = text buffer (the game's own)
        tst.b   (a5)
        beq.s   weapon
        cmpi.b  #1,(a5)
        beq     armor
        bra     done

weapon:
        move.b  9(a4),d0                | explosives (type 5..12) get no line
        cmpi.b  #5,d0
        bcs.s   w_ok
        cmpi.b  #12,d0
        bls     done
w_ok:   tst.b   3(a5)                   | no damage dice: nothing to compare
        beq     done
        moveq   #0,d4
        move.b  5(a5),d4                | d4 = damage bonus: table bonus
        tst.b   1(a5)
        bne.s   w_rng
        movea.l a2,a1                   | melee: + strength damage bonus (0x6E9E: d1)
        jsr     (0x6E9E).l
        add.b   d1,d4
w_rng:  move.b  (a4),d0                 | + weapon specialisations of the character (0x6E80)
        movea.l a2,a1
        jsr     (0x6E80).l
        add.b   d0,d4
        add.b   4(a4),d4                | + the item's own modifier byte
        moveq   #0,d5
        move.b  4(a5),d5                | sides
        moveq   #0,d6
        move.b  3(a5),d6                | dice
        mulu.w  d6,d5
        add.b   d4,d5                   | max = dice*sides + bonus (byte, negative shows 0: as the sheet)
        bpl.s   w_max
        moveq   #0,d5
w_max:  add.b   d4,d6                   | min = dice + bonus
        bpl.s   w_min
        moveq   #0,d6
w_min:  moveq   #0,d7
        move.b  2(a5),d7
        lsr.b   #1,d7                   | attacks per round
        lea     (0xFFFFD5AE).l,a1
        lea     s_dmg(pc),a3
        bsr     puts
        bsr     range
        lea     s_now(pc),a3
        bsr     puts
        moveq   #0,d5                   | the weapon the character has now, as the character sheet shows it
        move.b  0xA(a0),d5
        moveq   #0,d6
        move.b  8(a0),d6
        mulu.w  d6,d5
        add.b   0xC(a0),d5
        bpl.s   n_max
        moveq   #0,d5
n_max:  add.b   0xC(a0),d6
        bpl.s   n_min
        moveq   #0,d6
n_min:  moveq   #0,d7
        move.b  6(a0),d7
        lsr.b   #1,d7
        bsr     range
        bra     show

armor:  moveq   #0,d4
        move.b  1(a5),d4
        subi.b  #0x32,d4
        add.b   4(a4),d4                | d4 = armor value of the item (as 0x6D1E adds it)
        moveq   #0,d5                   | d5 = value of what is worn now
        tst.b   0xC2(a2)
        beq.s   a_calc
        lea     0xC2(a2),a2
        jsr     (0x6E70).l
        move.b  1(a3),d5
        subi.b  #0x32,d5
        add.b   4(a2),d5
a_calc: move.b  4(a0),d6                | slot +4; the sheet shows 0x3C - that
        moveq   #0x3C,d1
        sub.b   d6,d1                   | d1 = AC now
        move.b  d6,d2
        sub.b   d5,d2
        add.b   d4,d2
        moveq   #0x3C,d3
        sub.b   d2,d3                   | d3 = AC with the item
        lea     (0xFFFFD5AE).l,a1
        lea     s_ac(pc),a3
        bsr     puts
        move.b  d3,d0
        bsr     sgn
        lea     s_now(pc),a3
        bsr     puts
        move.b  d1,d0
        bsr     sgn

show:   clr.b   (a1)
        move.w  #0xC000,(0xFFFFD5AC).l
        move.l  #0x00130018,(0xFFFFD5D6).l      | column 0x13, row 0x18: under the name (0x16) and the price (0x17)
        movea.l 48(sp),a4               | a4/a5 are the VDP control/data ports for the text routine: take the caller's values back
        movea.l 52(sp),a5
        jsr     (0x11C4C).l
done:   movem.l (sp)+,d0-d7/a0-a5
        jmp     (0x16AC8).l

| "min-max Xn" (or "n Xn" when equal): d6 min, d5 max, d7 attacks
range:  moveq   #0,d0
        move.b  d6,d0
        bsr.s   num
        cmp.b   d6,d5
        beq.s   r_x
        move.b  #'-',(a1)+
        moveq   #0,d0
        move.b  d5,d0
        bsr.s   num
r_x:    move.b  #' ',(a1)+
        move.b  #'X',(a1)+
        moveq   #0,d0
        move.b  d7,d0
        bra.s   num

| signed byte d0
sgn:    ext.w   d0
        bpl.s   sg_p
        move.b  #'-',(a1)+
        neg.w   d0
sg_p:   andi.l  #0xFFFF,d0
| unsigned d0 (0..65535 but only bytes are used here), digits appended at a1
num:    movem.l d1-d2,-(sp)
        moveq   #0,d2
        divu.w  #100,d0
        move.w  d0,d1
        swap    d0
        andi.l  #0xFFFF,d0
        tst.w   d1
        beq.s   n_t
        addi.b  #0x30,d1
        move.b  d1,(a1)+
        moveq   #1,d2
n_t:    divu.w  #10,d0
        move.w  d0,d1
        swap    d0
        tst.w   d1
        bne.s   n_t2
        tst.w   d2
        beq.s   n_o
n_t2:   addi.b  #0x30,d1
        move.b  d1,(a1)+
n_o:    addi.b  #0x30,d0
        move.b  d0,(a1)+
        movem.l (sp)+,d1-d2
        rts

puts:   move.b  (a3)+,d0
        beq.s   p_x
        move.b  d0,(a1)+
        bra.s   puts
p_x:    rts

s_dmg:  .asciz  "DMG "
s_now:  .asciz  "  NOW "
s_ac:   .asciz  "AC "
        .even
