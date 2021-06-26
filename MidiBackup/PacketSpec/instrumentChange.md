## Packet data

0xB10000B12070C105F043104C08010D42F7

**Packet count**

> 4

| Packet bytes       | Packet details                                   |
| ------------------ | ------------------------------------------------ |
| B10000             | Channel 1 Control change: BankSelect (`0x00`)    |
| B12070             | Channel 1 Control change: BankSelectLsb (`0x20`) |
| C105F0             | Channel 1 Program: Unknown (`0x05F0`)            |
| F043104C08010D42F7 | Channel 0 SysEx: Unknown (`0x43104C08010D42F7`)  |
|                    |                                                  |

## SysExCalls

Taken from the [midi](https://usa.yamaha.com/files/download/other_assets/8/314458/npv80_60_en_mr_v010a.pdf) specs

> GM System ON: 0xF07E7F0901F7H

-   This message automatically restores all default settings for the
    instrument, with the exception of MIDI Master Tuning.

---

> MIDI Master Volume: 0xF07F7F0401llmmF7

-   This message allows the volume of all channels to be changed
    simultaneously (Universal System Exclusive).
-   The values of “mm” is used for MIDI Master Volume. (Values for
    “ll” are ignored.)

> MIDI Master Tuning: 0xF0431n27300000mmllccF7

-   This message simultaneously changes the tuning value of all channels.
-   The values of “mm” and “ll” are used for MIDI Master Tuning.
-   The default value of “mm” and “ll” are 08H and 00respectively.
    Any values can be used for “n” and “cc”.

---

> Reverb Type: 0xF0431n4C020100mmllF7

-   mm : Reverb Type MSB
-   ll : Reverb Type LSB Refer to the Effect Map (page 126) for details.

> Chorus Type: 0xF0431n4C020120mmllF7

-   mm : Chorus Type MSB
-   ll : Chorus Type LSB Refer to the Effect Map (page 126) for details.
