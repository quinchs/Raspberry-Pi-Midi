using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class MidiEventArgs
    {
        public byte Channel { get; }

        public MidiMessage Message { get; }

        public MidiEventArgs(byte chan, MidiMessage msg)
        {
            this.Channel = chan;
            this.Message = msg;
        }
    }

    public class MidiNoteEventArgs
    {
        public int Note { get; }
        public int Velocity { get; }

        public MidiNoteEventArgs(NoteMessage msg)
        {
            this.Note = msg.MidiNoteNumber;
            this.Velocity = msg.Velocity;
        }
    }

    public class MidiSustainEventArg
    {
        public int Value { get; }

        public MidiSustainEventArg(SustainMessage msg)
        {
            this.Value = msg.SustainValue;
        }
    }

    public partial class MidiDriver
    {
        public event Func<Task> DeviceDisconnected;
        public event Func<Task> DeviceConnected;

        public event Func<Task> OnMidiClock;

        public event Func<MidiEventArgs, Task> OnMessage;

        public event Func<MidiNoteEventArgs, Task> OnNotePressed;

        public event Func<MidiNoteEventArgs, Task> OnNoteReleased;

        public event Func<MidiSustainEventArg, Task> OnSustain;

        public void DispatchMessageEvent(MidiMessage Message)
        {
            DispatchOnMessage(new MidiEventArgs(Message.Channel, Message));
            

            if(Message.Status == StatusType.MidiClock)
            {
                DispatchEvent(OnMidiClock);
            }
            else if(Message is NoteMessage note)
            {
                var arg = new MidiNoteEventArgs(note);
                if (note.State)
                    DispatchEvent(OnNotePressed, arg);
                else
                    DispatchEvent(OnNoteReleased, arg);
            }
            else if (Message is SustainMessage sustain)
            {
                var arg = new MidiSustainEventArg(sustain);
                DispatchEvent(OnSustain, arg);
            }
        }

        private void DispatchEvent(Func<Task> func)
        {
            _ = Task.Run(async () =>
            {
                var task = func?.Invoke();

                if (task == null)
                    return;

                await task;

                if (task.Exception != null)
                {
                    Console.WriteLine($"Exception in event listener: {task.Exception}");
                }
            });
        }
        private void DispatchEvent<TIn>(Func<TIn, Task> func, TIn val)
        {
            _ = Task.Run(async () =>
            {
                var task = func?.Invoke(val);

                if (task == null)
                    return;

                await task;

                if (task.Exception != null)
                {
                    Console.WriteLine($"Exception in event listener: {task.Exception}");
                }
            });
        }

        private void DispatchOnMessage(MidiEventArgs arg)
        {
            _ = Task.Run(async () =>
            {
                var task = OnMessage?.Invoke(arg);

                if (task == null)
                    return;

                await task;

                if (task.Exception != null)
                {
                    Console.WriteLine($"Exception in event listener: {task.Exception}");
                }
            });
        }
    }
}
