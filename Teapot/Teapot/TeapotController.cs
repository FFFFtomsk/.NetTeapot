using nanoFramework.Json;
using nanoFramework.WebServer;
using System;
using System.Device.Gpio;
using System.Net;
using System.Text;
using Teapot.Services;

namespace Teapot
{
    class TeapotController
    {
        private readonly IStateService _stateService;

        public TeapotController(IStateService stateService)
        {
            _stateService = stateService;
        }

        /// <summary>
        /// get coffee 
        /// </summary>
        [Route("coffee")]
        [Method("GET")]
        public void Coffee(WebServerEventArgs e)
        {
            try
            {
                e.Context.Response.StatusCode = 418;
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }

        [Route("state")]
        [Method("GET")]
        public void GetState(WebServerEventArgs e)
        {
            try
            {
                var state = _stateService.GetState();
                var content = JsonConvert.SerializeObject(state);
                e.Context.Response.ContentType = "application/json";
                WebServer.OutPutStream(e.Context.Response, content);

            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }

        [Route("state")]
        [Method("POST")]
        public void SetState(WebServerEventArgs e)
        {
            try
            {
                byte[] buff = new byte[e.Context.Request.ContentLength64];
                e.Context.Request.InputStream.Read(buff, 0, buff.Length);
                string rawData = new string(Encoding.UTF8.GetChars(buff));
                TeapotTargetState state = (TeapotTargetState)JsonConvert.DeserializeObject(rawData, typeof(TeapotTargetState));
                _stateService.SetTargetState(state);
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }
    }
}
