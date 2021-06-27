using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket.Types
{
    public class ExceptionResult : WebsocketMessageResult
    {
        public ExceptionResult(Exception x)
            : base(x)
        {

        }
    }
}
