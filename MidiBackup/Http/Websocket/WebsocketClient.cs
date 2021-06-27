using MidiBackup.Http.Websocket.MessageTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket
{
    public class WebsocketClient : IDisposable
    {
        public WebSocket Socket { get; private set; }

        public bool IsOpen
            => Socket != null ? Socket.State == WebSocketState.Open : false;

        public WebsocketClient(WebSocket socket)
        {
            this.Socket = socket;
        }

        public async Task<Message> ReceiveAsync(CancellationToken token = default)
        {
            if (!this.IsOpen)
                return null;

            byte[] buffer = new byte[1024];

            var result = await Socket.ReceiveAsync(buffer, token).ConfigureAwait(false);

            Logger.Write($"Got {result.Count} bytes from client: {result.MessageType}",  Severity.Websocket, Severity.Log);

            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    string json = Encoding.UTF8.GetString(buffer);
                    try
                    {
                        return JsonConvert.DeserializeObject<Message>(json);
                    }
                    catch (JsonException)
                    {
                        await Socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, null, default);
                        this.Dispose();
                        return null;
                    }
                case WebSocketMessageType.Binary:
                    await Socket.CloseAsync(WebSocketCloseStatus.ProtocolError, null, default);
                    this.Dispose();
                    return null;
                case WebSocketMessageType.Close:
                    this.Dispose();
                    return null;
                default: return null;
            }

        }

        public Task SendAsync(IMessage Message, CancellationToken token = default)
        {
            string json = JsonConvert.SerializeObject(Message);

            byte[] data = Encoding.UTF8.GetBytes(json);

            return Socket.SendAsync(data, WebSocketMessageType.Text, true, token);
        }

        public Task DisconnectAsync(string reason = "Normal closure", CancellationToken token = default)
        {
            return Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, token);
        }

        public void Dispose()
        {
            try
            {
                this.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default).GetAwaiter().GetResult();
                this.Socket.Dispose();
                Socket = null;
            }
            catch(Exception x)
            {
                Logger.Write($"Tried to dispose client: {x}", Severity.Websocket, Severity.Warning);
            }
        }
    }
}
