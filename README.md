# Raspberry-Pi-Midi

Welcome to the C# raspberry pi midi reader. I dont know if this is technically a "driver" or not.. This project was made because I had a pi and wanted to record my jam sessions on my piaggero np v60.

## Features

The app will record you playing automatically and save them to `$CURRENT_DIR$/MidiFIles/` using iso8601 file names. You can also playback these files by running the app with the parameters `-p <path-to-midi>`.

### How it works

The application doesnt rely on 3rd party drivers for midi. The pi will create a file in `/dev/snd` named `midiXXXX`. this file is a stream directly to the connected midi device.
The app opens this file and starts reading it and decoding the data.

### Whats supported and whats not?

Not all of the midi specs are implemented, and I dont have other midi devices to test with. Its quite easy to expand on the message set by making a new class and inheriting either `MidiMessage` for receiving or `BaseOutgoing` for sending.

#### MidiMessage

| Property Name | Property Description                                                        |
| ------------- | --------------------------------------------------------------------------- |
| `Status`      | Gets the status byte of the message and parses it to the `StatusType` enum. |
| `CC?`         | Tries to parse the second byte of the message to the `CCType` enum.         |
| `Meta?`       | Attemps to parse the third byte of the message to a the `MetaType` enum.    |
| `Value`       | The `int` representation of the message.                                    |
| `Data`        | The data (excluding the status byte) of the message.                        |
| `DataOffset`  | The optional offset of the data if the packet is known to contain it.       |
| `Size`        | The size of the packet.                                                     |
| `RawPacket`   | The intire buffer recieved, this buffer can contain other packets           |
| `StatusByte`  | The `Status` in byte form                                                   |
| `MetaType`    | The `Meta` in byte form                                                     |
| `Channel`     | The channel this message was sent on                                        |
| `EventType`   | The event type of this message                                              |

#### BaseOutgoing

If you want to send outgoing messages, you will need to inherit `BaseOutgoing`, this abstract class requires you to have a `Build` method that will return a byte[] of your payload. `BaseOutgion` also contains a useful method called `CompilePacket` that takes in a channel, a `StatusType` and a byte[] of your data. it will return a concatination of the parameters that are MIDI compliant.

#### Incoming message

| Raw Data         | Packet class           | Description                                                                                                                                                                                                                                         |
| ---------------- | ---------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `0xSSCCMM...`    | MidiMessage            | This abstract class is the base class that all incoming midi messages inherit from. its what has the packet details including the status type, control type, and meta type.                                                                         |
| `0x90NNVV`       | NoteMessage            | This message is a note on mesage, `NN` is the note number and `VV` is the velocity.                                                                                                                                                                 |
| `0x80NNVV`       | NoteMessage            | This is a note off message, it gets deserialized into the same class as note on.                                                                                                                                                                    |
| `0x0BNNVV`       | ControlChangeMessage   | This message represents a control change. `NN` is the control type and `VV` is the control value.                                                                                                                                                   |
| `0x0B40VV`       | SustainMessage         | This message represents the sustain change control message. `VV` is the value of the sustain pedal, if your midi device supports variable sustain then its value is going to be from 0 to 127. otherwise its either 0 or 127 (true or false.)       |
| `0xFOIISSPPVV..` | SystemExclusiveMessage | This message is a system exclusive message, it can contain multiple bytes as values, the class does not attempt to parse the values it gets into anything, it only contains the `IdentByte`, `SubStatus`, `Parameter` and the value ( `SysExValue`) |
| `0xC0VV`         | ProgramMessage         | Represents an instrument change, it only supports the MIDI general standard instrument list, it doesnt not include device specific instuments, although you can parse it yourself with the `InstrumentValue` inside the class                       |

> Any unknown packets will get deserialized into the `DefaultMidiMessage` class.

### Configuration.

You cansetup a `conf.json` file in the same directory as the executable. it contains only one property for now
| Name | Description |
| ---- | ----------- |
| `Debug` | `true` if you want the app to print debug data of the midi device, this includes listing all packets that are coming, otherwise `false` |
