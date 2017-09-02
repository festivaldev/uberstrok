﻿using Photon.SocketServer;

namespace UbzStuff.Realtime.Server.Game
{
    public class GameApplication : ApplicationBase
    {
        protected override PeerBase CreatePeer(InitRequest initRequest)
        {
            return new GamePeer(initRequest);
        }

        protected override void Setup()
        {
            // Space
        }

        protected override void TearDown()
        {
            // Space
        }
    }
}
