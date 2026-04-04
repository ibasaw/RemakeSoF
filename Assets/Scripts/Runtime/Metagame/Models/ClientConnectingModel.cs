using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    public class ClientConnectingModel : Model<MetagameApplication>
    {
        public float ElapsedTime { get; private set; }
        public string ServerAddress { get; private set; }
        public string ServerName { get; private set; }

        void Update()
        {
            ElapsedTime += Time.deltaTime;
        }

        public void InitializeTimer()
        {
            ElapsedTime = 0;
        }

        public void SetServerAddress(string serverAddress)
        {
            ServerAddress = serverAddress;
        }

        public void SetServerName(string serverName)
        {
            ServerName = serverName;
        }
    }
}
