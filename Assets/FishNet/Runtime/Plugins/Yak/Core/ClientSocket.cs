using FishNet.Transporting.Yak.Server;
using System;
using System.Collections.Generic;

namespace FishNet.Transporting.Yak.Client
{
    /// <summary>
    /// Creates a fake client connection to interact with the ServerSocket.
    /// </summary>
    public class ClientSocket : CommonSocket
    {
        #region Private.
        /// <summary>
        /// Socket for the server.
        /// </summary>
        private ServerSocket _server;
        /// <summary>
        /// Incomimg data.
        /// </summary>
        private Queue<LocalPacket> _incoming = new();
        #endregion

        //PROSTART
        /// <summary>
        /// Initializes this for use.
        /// </summary>
        internal override void Initialize(Transport t, CommonSocket socket)
        {
            base.Initialize(t, socket);
            _server = (ServerSocket)socket;
        }
        //PROEND

        /// <summary>
        /// Starts the client connection.
        /// </summary>
        internal bool StartConnection()
        {
            //PROSTART
            //Already starting/started, or stopping.
            if (base.GetLocalConnectionState() != LocalConnectionState.Stopped)
                return false;

            SetLocalConnectionState(LocalConnectionState.Starting, false);
            /* Certain conditions need the client state to change as well.
             * Such as, if the server state is stopping then the client should
             * also be stopping, rather than starting. Or if the server state
             * is already started then client should immediately be set started
             * rather than waiting for server started callback. */
            LocalConnectionState serverState = _server.GetLocalConnectionState();
            if (serverState == LocalConnectionState.Stopping || serverState == LocalConnectionState.Started)
                OnLocalServerConnectionState(_server.GetLocalConnectionState());
            //PROEND
            return true;
        }

        //PROSTART
        /// <summary>
        /// Sets a new connection state.
        /// </summary>
        protected override void SetLocalConnectionState(LocalConnectionState connectionState, bool server)
        {
            base.SetLocalConnectionState(connectionState, server);
            if (connectionState == LocalConnectionState.Started || connectionState == LocalConnectionState.Stopped)
                _server.OnLocalClientConnectionState(connectionState);
        }
        //PROEND

        /// <summary>
        /// Stops the local socket.
        /// </summary>
        internal bool StopConnection()
        {
            //PROSTART
            if (base.GetLocalConnectionState() == LocalConnectionState.Stopped || base.GetLocalConnectionState() == LocalConnectionState.Stopping)
                return false;

            base.ClearQueue(ref _incoming);
            //Immediately set stopped since no real connection exists.
            SetLocalConnectionState(LocalConnectionState.Stopping, false);
            SetLocalConnectionState(LocalConnectionState.Stopped, false);
            //PROEND
            return true;
        }

        //PROSTART
        /// <summary>
        /// Iterations data received.
        /// </summary>
        internal void IterateIncoming()
        {
            if (base.GetLocalConnectionState() != LocalConnectionState.Started)
                return;

            while (_incoming.Count > 0)
            {
                LocalPacket packet = _incoming.Dequeue();
                ArraySegment<byte> segment = new(packet.Data, 0, packet.Length);
                ClientReceivedDataArgs dataArgs = new(segment, (Channel)packet.Channel, base.Transport.Index);
                base.Transport.HandleClientReceivedDataArgs(dataArgs);
                packet.Dispose();
            }
        }
        //PROEND

        //PROSTART
        /// <summary>
        /// Called when the server sends the local client data.
        /// </summary>
        internal void ReceivedFromLocalServer(LocalPacket packet)
        {
            _incoming.Enqueue(packet);
        }
        //PROEND

        //PROSTART
        /// <summary>
        /// Queues data to be sent to server.
        /// </summary>
        internal void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            if (base.GetLocalConnectionState() != LocalConnectionState.Started)
                return;
            if (_server.GetLocalConnectionState() != LocalConnectionState.Started)
                return;

            LocalPacket packet = new(segment, channelId);
            _server.ReceivedFromLocalClient(packet);
        }
        //PROEND

        #region Local server.
        //PROSTART
        /// <summary>
        /// Called when the local server starts or stops.
        /// </summary>
        internal void OnLocalServerConnectionState(LocalConnectionState state)
        {
            //Server started.
            if (state == LocalConnectionState.Started &&
                base.GetLocalConnectionState() == LocalConnectionState.Starting)
            {
                SetLocalConnectionState(LocalConnectionState.Started, false);
            }
            //Server not started.
            else
            {
                //If stopped or stopping then disconnect client if also not stopped or stopping.
                if ((state == LocalConnectionState.Stopping || state == LocalConnectionState.Stopped) &&
                    (base.GetLocalConnectionState() == LocalConnectionState.Started ||
                    base.GetLocalConnectionState() == LocalConnectionState.Starting)
                    )
                {
                    SetLocalConnectionState(LocalConnectionState.Stopping, false);
                    SetLocalConnectionState(LocalConnectionState.Stopped, false);
                }
            }
        }
        //PROEND
        #endregion


    }
}