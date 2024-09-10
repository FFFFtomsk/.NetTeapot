﻿using Microsoft.Extensions.DependencyInjection;
using nanoFramework.WebServer;
using System;
using System.Net;

namespace Teapot
{
    internal class WebServerDi : WebServer
    {
        private readonly IServiceProvider _serviceProvider;

        public WebServerDi(int port, HttpProtocol protocol, Type[] controllers, IServiceProvider serviceProvider) : base(port, protocol, controllers)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void InvokeRoute(CallbackRoutes route, HttpListenerContext context)
        {
            route.Callback.Invoke(ActivatorUtilities.CreateInstance(_serviceProvider, route.Callback.DeclaringType), new object[] { new WebServerEventArgs(context) });
        }
    }
}
