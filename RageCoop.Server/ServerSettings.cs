﻿namespace RageCoop.Server
{
    public class ServerSettings
    {
        public int Port { get; set; } = 4499;
        public int MaxPlayers { get; set; } = 32;
        public int MaxLatency { get; set; } = 500;
        public string Name { get; set; } = "RAGECOOP server";
        public string WelcomeMessage { get; set; } = "Welcome on this server :)";
        public bool HolePunch { get; set; } = true;
        public bool AnnounceSelf { get; set; } = false;
        public string MasterServer { get; set; } = "[AUTO]";

        /// <summary>
        /// See <see cref="Core.Logger.LogLevel"/>.
        /// </summary>
        public int LogLevel=2;
        /// <summary>
        /// NPC data won't be sent to a player if their distance is greater than this value. -1 for unlimited.
        /// </summary>
        public float NpcStreamingDistance { get; set; } = 1000;
        /// <summary>
        /// Player's data won't be sent to another player if their distance is greater than this value. -1 for unlimited.
        /// </summary>
        public float PlayerStreamingDistance { get; set; } = -1;
    }
}