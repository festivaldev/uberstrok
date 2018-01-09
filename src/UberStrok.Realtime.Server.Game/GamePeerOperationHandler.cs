﻿using log4net;
using System;
using System.Collections.Generic;
using System.Text;
using UberStrok.Core.Views;
using UberStrok.WebServices.Client;

namespace UberStrok.Realtime.Server.Game
{
    public class GamePeerOperationHandler : BaseGamePeerOperationHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(nameof(GamePeerOperationHandler));

        public GamePeerOperationHandler(GamePeer peer) : base(peer)
        {
            // Space
        }

        protected override void OnGetGameListUpdates()
        {
            //TODO: Don't use that, cause apparently there is a FullGameList event which we can send to wipe stuff and things.
            // Clear the client list of games available.
            Peer.Events.SendGameListUpdateEnd();

            var rooms = new List<GameRoomDataView>(GameApplication.Instance.Games.Rooms.Count);
            foreach (var room in GameApplication.Instance.Games.Rooms.Values)
                rooms.Add(room.Data);

            Peer.Events.SendGameListUpdate(rooms, new List<int>());

            Log.Info($"OnGetGameListUpdates: Room Count -> {rooms.Count}");
        }

        protected override void OnGetServerLoad()
        {
            var view = new PhotonServerLoadView
            {
                // UberStrike does not care about this value, it uses its client side value.
                Latency = Peer.RoundTripTime / 2,
                State = PhotonServerLoadView.Status.Alive,
                MaxPlayerCount = 100,

                PeersConnected = GameApplication.Instance.PeerCount,
                // UberStrike also does not care about this value, it uses PeersConnected.
                PlayersConnected = GameApplication.Instance.PeerCount,

                RoomsCreated = GameApplication.Instance.Games.Rooms.Count,
                TimeStamp = DateTime.UtcNow
            };

            Peer.Events.SendServerLoadData(view);

            Log.Info($"OnGetServerLoad: Load -> {view.PeersConnected}/{view.MaxPlayerCount} Rooms: {view.RoomsCreated}");
        }

        protected override void OnCreateRoom(GameRoomDataView roomData, string password, string clientVersion, string authToken, string magicHash)
        {
            var bytes = Convert.FromBase64String(authToken);
            var data = Encoding.UTF8.GetString(bytes);

            var webServer = data.Substring(0, data.IndexOf("#####"));

            // Retrieve user data from the web server.
            var client = new UserWebServiceClient(webServer);
            var member = client.GetMember(authToken);

            Peer.Member = member;

            GameApplication.Instance.Games.AddRoom(roomData, password);

            LogManager.GetLogger(typeof(GamePeerOperationHandler)).Info($"Creating new room -> {roomData.Name}:{roomData.Number}");

            var room = default(GameManager.Room);
            if (GameApplication.Instance.Games.Rooms.TryGetValue(roomData.Number, out room))
            {
                Peer.Game = new GameRoom(Peer, room);
                Peer.Events.SendRoomEntered(room.Data);

                Peer.Game.Room.Data.ConnectedPlayers++;
            }
            else
            {
                // wtf fam?
            }
        }

        protected override void OnJoinRoom(int roomId, string password, string clientVersion, string authToken, string magicHash)
        {
            var bytes = Convert.FromBase64String(authToken);
            var data = Encoding.UTF8.GetString(bytes);

            var webServer = data.Substring(0, data.IndexOf("#####"));

            // Retrieve user data from the web server.
            var client = new UserWebServiceClient(webServer);
            var member = client.GetMember(authToken);

            Peer.Member = member;

            var room = default(GameManager.Room);
            if (GameApplication.Instance.Games.Rooms.TryGetValue(roomId, out room))
            {
                LogManager.GetLogger(typeof(GamePeerOperationHandler)).Info("Joining stuff ey!");
                if (room.Data.IsPasswordProtected)
                {
                    //TODO: Check password.
                    Peer.Events.SendRoomEntered(room.Data);
                }
                else
                {
                    Peer.Game = new GameRoom(Peer, room);
                    Peer.Events.SendRoomEntered(room.Data);

                    Peer.Game.Room.Data.ConnectedPlayers++;
                }
            }
            else
            {
                // SendRoomEnterFailed
            }
        }

        protected override void OnLeaveRoom()
        {
            //TODO: Kill room if the number of connected players is 0.
            //TODO: Thread safety cause we need it, possibly.

            Peer.Game.Room.Data.ConnectedPlayers--;
            Peer.Game = null;
        }

        protected override void OnUpdatePing(ushort ping)
        {
            //TODO: Client should not have the ability to change its ping but it can cause uberstrike.
            Peer.Ping = ping;
        }
    }
}