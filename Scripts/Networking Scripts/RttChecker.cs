using System;
using System.Collections.Generic;
using Godot;

namespace ADK.Net
{
    public partial class RttChecker : Node
    {
        [Export] public bool DoRegularRttChecks = false;
        [Export] float checkIntervalSeconds = 3;
        double t;

        public event Action<(float playerId, float rtt)> RttUpdateForPlayer;
        Dictionary<long, DateTime> pendingPingTimes = new();
        Dictionary<long, float> lastRecordedRtt = new();

        public override void _Process(double delta)
        {
            t += delta;
            if (t >= checkIntervalSeconds)
            {
                t -= checkIntervalSeconds;
                RttCheckAll();
            }
        }

        public void RttCheckAll()
        {
            var players = NetworkManager.Instance.Players.Keys;
            foreach (var playerId in players)
            {
                if (!pendingPingTimes.ContainsKey(playerId))
                {
                    pendingPingTimes.Add(playerId, DateTime.Now);
                }
                RpcId(playerId, nameof(Ping));
                if (!lastRecordedRtt.ContainsKey(playerId))
                {
                    lastRecordedRtt.Add(playerId, -1);
                }
                Rpc(nameof(ReceiveRttUpdateForPlayer), (float)playerId, lastRecordedRtt[playerId]);
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void Ping()
        {
            RpcId(1, nameof(Pong));
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void Pong()
        {
            int senderId = Multiplayer.GetRemoteSenderId();
            float rttMs = (float)(DateTime.Now - pendingPingTimes[senderId]).TotalMilliseconds;
            pendingPingTimes.Remove(senderId);
            lastRecordedRtt[senderId] = rttMs;
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReceiveRttUpdateForPlayer(float playerId, float rttMs)
        {
            RttUpdateForPlayer?.Invoke((playerId, rttMs));
        }
    }
}
