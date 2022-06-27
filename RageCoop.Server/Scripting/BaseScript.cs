﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;

namespace RageCoop.Server.Scripting
{
    internal class BaseScript:ServerScript
    {
        public override void OnStart()
        {
            API.RegisterCustomEventHandler(CustomEvents.NativeResponse, NativeResponse);
        }
        public override void OnStop()
        {
        }
        public void SetAutoRespawn(Client c,bool toggle)
        {
            c.SendCustomEvent(CustomEvents.SetAutoRespawn, new() { toggle });
        }
        void NativeResponse(CustomEventReceivedArgs e)
        {
            try
            {
                int id = (int)e.Args[0];
                Action<object> callback;
                lock (e.Sender.Callbacks)
                {
                    if (e.Sender.Callbacks.TryGetValue(id, out callback))
                    {
                        callback(e.Args[1]);
                        e.Sender.Callbacks.Remove(id);
                    }
                }
            }
            catch (Exception ex)
            {
                API.GetLogger().Error("Failed to parse NativeResponse");
                API.GetLogger().Error(ex);
            }
        }
    }
}