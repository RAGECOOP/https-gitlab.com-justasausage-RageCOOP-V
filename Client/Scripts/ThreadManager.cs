﻿using RageCoop.Client.Scripting;
using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RageCoop.Client
{

    /// <summary>
    /// Needed to properly stop all thread when the module unloads
    /// </summary>
    internal static class ThreadManager
    {
        private static List<Thread> _threads = new();
        private static Thread _watcher = new(() => _removeStopped());
        private static void _removeStopped()
        {
            while (!Main.IsUnloading)
            {
                lock (_threads)
                {
                    _threads.RemoveAll(t => !t.IsAlive);
                }
                Thread.Sleep(1000);
            }
        }
        public static Thread CreateThread(Action callback, string name = "CoopThread", bool startNow = true)
        {
            lock (_threads)
            {
                var created = new Thread(() =>
                {
                    try
                    {
                        callback();
                    }
                    catch (ThreadInterruptedException) { }
                    catch (Exception ex)
                    {
                        Main.Logger.Error($"Unhandled exception caught in thread {Environment.CurrentManagedThreadId}", ex);
                    }
                    finally
                    {
                        Main.Logger.Debug($"Thread stopped: " + Environment.CurrentManagedThreadId);
                    }
                });
                created.Name = name;
                Main.Logger.Debug($"Thread created: {name}, id: {created.ManagedThreadId}");
                _threads.Add(created);
                if (startNow) created.Start();
                return created;
            }
        }

        public static void OnUnload()
        {
            lock (_threads)
            {
                foreach (var thread in _threads)
                {
                    if (thread.IsAlive)
                    {
                        Main.Logger.Debug($"Waiting for thread {thread.ManagedThreadId} to stop");
                        // thread.Interrupt(); PlatformNotSupportedException ?
                        thread.Join();
                    }
                }
                _threads.Clear();
                _threads = null;
                _watcher.Join();
            }
        }
    }
}