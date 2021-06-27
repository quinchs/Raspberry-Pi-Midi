using MidiBackup.Http.RestService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Routes
{
    class WebsocketRoute : RestModuleBase
    {
        [Route("/socket", "GET")]
        public async Task<RestResult> GetSocket()
        {
            await AcceptWebsocketAsync();
            return RestResult.KeepOpen;
        }
    }
}
