using MidiBackup.Http.Websocket.MessageTypes;
using MidiBackup.Http.Websocket.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket
{
    public class WebsocketServer
    {
        public IReadOnlyCollection<WebsocketClient> Clients
            => _clients;

        private List<WebsocketClient> _clients { get; set; } = new List<WebsocketClient>();

        private HttpServer Server { get; }

        public WebsocketServer(HttpServer server)
        {
            this.Server = server;

            this.Server.Driver.DeviceConnected           += SendStatusUpdate;
            this.Server.Driver.DeviceDisconnected        += SendStatusUpdate;
            this.Server.Driver.Recorder.RecordingStarted += SendStatusUpdate;
            this.Server.Driver.Recorder.RecordingStopped += SendStatusUpdate;
            this.Server.Driver.Playback.PlaybackStarted  += (a) => SendPlaybackStatus();
            this.Server.Driver.Playback.PlaybackStopped  += SendPlaybackStatus;
            this.Server.Driver.Playback.MidiTimeUpdated  += SendPlaybackSeek;

            this.Server.Driver.OnMetadataCreated += (arg1) => SendFileEvent(FileEventType.Created, arg1);
            this.Server.Driver.OnMetadataDeleted += (arg1) => SendFileEvent(FileEventType.Deleted, arg1);
            this.Server.Driver.OnMetadataUpdated += (arg1, arg2) => SendFileEvent(FileEventType.Updated, arg2);

            Logger.Write($"Websocket server {Logger.BuildColoredString("Online", ConsoleColor.Green)}!", Severity.Websocket);
        }

        private Task SendFileEvent(FileEventType type, MidiFileMetadata newMeta)
        {
            SendToAll(new FileEvent(type, newMeta, this.Server.Driver.FileManager.Files).BuildMessage());
            return Task.CompletedTask;
        }

        private Task SendPlaybackSeek(long arg1, long arg2)
        {
            SendToAll(new PlaybackEvent(this.Server.Driver.Playback, arg1, arg2).BuildMessage());
            return Task.CompletedTask;
        }

        private Task SendPlaybackStatus()
        {
            SendToAll(new PlaybackEvent(this.Server.Driver.Playback).BuildMessage());
            return Task.CompletedTask;
        }

        private Task SendStatusUpdate()
        {
            SendToAll(new StatusUpdate(this.Server.Driver).BuildMessage());
            return Task.CompletedTask;
        }

        public void SendToAll(IMessage message)
        {
            foreach(var client in Clients)
            {
                if (!client.IsOpen)
                {
                    client.Dispose();
                    _clients.Remove(client);
                }
                else
                    _ = Task.Run(async () => await client.SendAsync(message));
            }
        }

        public async Task AcceptWebsocketRequestAsync(HttpListenerContext context)
        {
            try
            {
                var socketContext = await context.AcceptWebSocketAsync(null);

                var client = new WebsocketClient(socketContext.WebSocket);

                _clients.Add(client);

                _ = Task.Run(async () => await Listen(client));
            }
            catch(Exception x)
            {
                Logger.Write($"{Logger.BuildColoredString(x, ConsoleColor.Red)}");
            }
        }

        public async Task Listen(WebsocketClient client)
        {
            while (client.IsOpen)
            {
                try
                {
                    var message = await client.ReceiveAsync();

                    if (message == null)
                    {
                        this._clients.Remove(client);
                        return;
                    }

                    switch (message.Code)
                    {
                        case OpCode.RemotePlayerCommand:
                            DispatchTask(HandleRemotePlayerCommand(message), client);
                            break;
                    }
                }
                catch(Exception x)
                {
                    Logger.Write($"{Logger.BuildColoredString($"{x}", ConsoleColor.Red)}");
                }
            }
        }

        private void DispatchTask(Task<WebsocketMessageResult> task, WebsocketClient client)
        {
            _ = Task.Run(async () =>
            {
                var result = await task;

                if(task.Exception != null)
                {
                    Logger.Write($"Exception in socket handler: {task.Exception}", Severity.Websocket, Severity.Error);
                    await client.SendAsync(new ExceptionResult(task.Exception).BuildMessage());
                    return;
                }

                Logger.Debug($"Returning {nameof(result)} for {nameof(task)}", Severity.Websocket);

                await client.SendAsync(result.BuildMessage());
            });
        }

        private async Task<WebsocketMessageResult> HandleRemotePlayerCommand(RemotePlayerCommand command)
        {
            switch (command.Command)
            {
                case Command.Play:
                    var file = command.Value;
                    if (!File.Exists($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}MidiFiles{Path.DirectorySeparatorChar}{file}"))
                        return PlayerCommandResult.FromResult(command, false, "No file found");

                    if (!Server.Driver.IsConnected)
                        return PlayerCommandResult.FromResult(command, false, "Device not connected");

                    Server.Driver.Playback.Start($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}MidiFiles{Path.DirectorySeparatorChar}{file}");
                    return PlayerCommandResult.FromResult(command, true);

                case Command.Stop:
                    if (!Server.Driver.Playback.IsPlaying)
                        return PlayerCommandResult.FromResult(command, false, "Player isn't playing");

                    Server.Driver.Playback.Stop();

                    return PlayerCommandResult.FromResult(command, true);

                case Command.Seek:
                    if (!Server.Driver.Playback.IsPlaying)
                        return PlayerCommandResult.FromResult(command, false, "Player isn't playing");

                    if(!long.TryParse(command.Value.ToString(), out long seek))
                        return PlayerCommandResult.FromResult(command, false, "Invalid seek location");

                    return Server.Driver.Playback.Seek(seek) 
                        ? PlayerCommandResult.FromResult(command, true) 
                        : PlayerCommandResult.FromResult(command, false, "Invalid seek location");

                case Command.Pause:
                    if (!Server.Driver.Playback.IsPlaying)
                        return PlayerCommandResult.FromResult(command, false, "Player isn't playing");

                    Server.Driver.Playback.Pause();
                    return PlayerCommandResult.FromResult(command, true);

                case Command.Resume:
                    if (!Server.Driver.Playback.IsPlaying)
                        return PlayerCommandResult.FromResult(command, false, "Player isn't playing");

                    Server.Driver.Playback.Resume();
                    return PlayerCommandResult.FromResult(command, true);

                default: return PlayerCommandResult.FromResult(command, false, "Unknown command.");
            }
        }
    }
}
