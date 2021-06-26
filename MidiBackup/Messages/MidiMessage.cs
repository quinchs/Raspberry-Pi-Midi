using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class DefaultMidiMessage : MidiMessage
    {
        public DefaultMidiMessage(MidiPacket pack)
            : base(pack)
        {

        }
    }

    public enum StatusType : byte
    {
        NoteOff = 0x80,
        NoteOn = 0x90,
        PAf = 0xA0,
        CC = 0xB0,
        Program = 0xC0,
        CAf = 0xD0,
        Pitch = 0xE0,
        SysEx1 = 0xF0,
        MtcQuarterFrame = 0xF1,
        SongPositionPointer = 0xF2,
        SongSelect = 0xF3,
        TuneRequest = 0xF6,
        SysEx2 = 0xF7,
        MidiClock = 0xF8,
        MidiTick = 0xF9,
        MidiStart = 0xFA,
        MidiContinue = 0xFB,
        MidiStop = 0xFC,
        ActiveSense = 0xFE,
        Reset = 0xFF,
        EndSysEx = 0xF7,
        Meta = 0xFF,
    }
    public enum CCType : byte
    {
        BankSelect = 0x00,
        Modulation = 0x01,
        Breath = 0x02,
        Foot = 0x04,
        PortamentoTime = 0x05,
        DteMsb = 0x06,
        Volume = 0x07,
        Balance = 0x08,
        Pan = 0x0A,
        Expression = 0x0B,
        EffectControl1 = 0x0C,
        EffectControl2 = 0x0D,
        General1 = 0x10,
        General2 = 0x11,
        General3 = 0x12,
        General4 = 0x13,
        BankSelectLsb = 0x20,
        ModulationLsb = 0x21,
        BreathLsb = 0x22,
        FootLsb = 0x24,
        PortamentoTimeLsb = 0x25,
        DteLsb = 0x26,
        VolumeLsb = 0x27,
        BalanceLsb = 0x28,
        PanLsb = 0x2A,
        ExpressionLsb = 0x2B,
        Effect1Lsb = 0x2C,
        Effect2Lsb = 0x2D,
        General1Lsb = 0x30,
        General2Lsb = 0x31,
        General3Lsb = 0x32,
        General4Lsb = 0x33,
        Hold = 0x40,
        PortamentoSwitch = 0x41,
        Sostenuto = 0x42,
        SoftPedal = 0x43,
        Legato = 0x44,
        Hold2 = 0x45,
        SoundController1 = 0x46,
        SoundController2 = 0x47,
        SoundController3 = 0x48,
        SoundController4 = 0x49,
        SoundController5 = 0x4A,
        SoundController6 = 0x4B,
        SoundController7 = 0x4C,
        SoundController8 = 0x4D,
        SoundController9 = 0x4E,
        SoundController10 = 0x4F,
        General5 = 0x50,
        General6 = 0x51,
        General7 = 0x52,
        General8 = 0x53,
        PortamentoControl = 0x54,
        Rsd = 0x5B,
        Effect1 = 0x5B,
        Tremolo = 0x5C,
        Effect2 = 0x5C,
        Csd = 0x5D,
        Effect3 = 0x5D,
        Celeste = 0x5E,
        Effect4 = 0x5E,
        Phaser = 0x5F,
        Effect5 = 0x5F,
        DteIncrement = 0x60,
        DteDecrement = 0x61,
        NrpnLsb = 0x62,
        NrpnMsb = 0x63,
        RpnLsb = 0x64,
        RpnMsb = 0x65,
        // Channel mode messages
        AllSoundOff = 0x78,
        ResetAllControllers = 0x79,
        LocalControl = 0x7A,
        AllNotesOff = 0x7B,
        OmniModeOff = 0x7C,
        OmniModeOn = 0x7D,
        PolyModeOnOff = 0x7E,
        PolyModeOn = 0x7F,
    }
    public enum MetaType : byte
    {
        SequenceNumber = 0x00,
        Text = 0x01,
        Copyright = 0x02,
        TrackName = 0x03,
        InstrumentName = 0x04,
        Lyric = 0x05,
        Marker = 0x06,
        Cue = 0x07,
        ChannelPrefix = 0x20,
        EndOfTrack = 0x2F,
        Tempo = 0x51,
        SmpteOffset = 0x54,
        TimeSignature = 0x58,
        KeySignature = 0x59,
        SequencerSpecific = 0x7F,
    }
    public abstract class MidiMessage : IMidiMessage
    {
        public StatusType Status
        {
            get
            {
                var raw = (Value & 0xFF);
                if (raw >= 0xF0)
                    return (StatusType)raw;
                else // remove the last 4 bits as it contains a channel and the values below F0 dont have them. 
                    return (StatusType)(((Value & 0xFF) >> 4) << 4);
            }
        }
        public CCType? CC { get => Status == StatusType.CC ? (CCType)((Value & 0xFF00) >> 8) : null; }
        public MetaType? Meta { get => Status == StatusType.Meta ? (MetaType)((Value & 0xFF00) >> 8) : null; }

        public int Value { get; }
        public byte[] Data { get; }
        public int DataOffset { get; }

        public int Size { get; }

        public byte[] RawPacket { get; }

        public byte StatusByte
        {
            get => (byte)(Value & 0xFF);
        }
        public byte MetaType
        {
            get => (byte)((Value & 0xFF00) >> 8);
        }
        public byte Channel
        {
            get => (byte)(Value & 0x0F);
        }

        public byte EventType
        {
            get
            {
                switch (Status)
                {
                    case StatusType.Meta:
                    case StatusType.SysEx1:
                    case StatusType.SysEx2:
                        return StatusByte;
                    default:
                        return (byte)(Value & 0xF0);
                }
            }
        }

        public MidiMessage(MidiPacket packet)
        {
            this.Value = packet.Value; //type + (arg1 << 8) + (arg2 << 16);
            this.Data = packet.Data;
            this.DataOffset = packet.DataOffset;
            this.Size = packet.DataLength;
            this.RawPacket = packet.Raw;
        }

        public override string ToString()
        {
            return $"{this.Channel.ToString().PadRight(4)} -> {{{this.Size}}} |{this.StatusByte.ToString("X2")}:{this.MetaType.ToString("X2")}:{this.EventType.ToString("X2")}|  " +
                   $"{this.Status}:{(this.Meta.HasValue ? this.Meta.Value : "none")}:{(this.CC.HasValue ? this.CC : "none")} {(this.Data != null ? "\nD: " + BitConverter.ToString(this.Data).Replace("-", "") : "")}";

        }
    }
}
