// *************************************************************************************************
// The MIT License (MIT)
// 
// Copyright (c) 2016 Sean
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// *************************************************************************************************
// Project source: https://github.com/theoxuan/FlexiSocket

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace FlexiFramework.Networking
{
    internal sealed class FlexiSocket : ISocketClient, ISocketServer, ISocketClientToken
    {
        private readonly Socket _socket;
        private readonly bool _ipv6;
        private readonly IProtocol _protocol;

        public int Port { get; private set; }
        public event ClosedCallback Closed;

        #region client

        public string IP { get; private set; }

        public event ConnectedCallback Connected;

        public event ReceivedCallback Received;

        public event ReceivedStringCallback ReceivedString;

        public event DisconnectedCallback Disconnected;

        public event SentCallback Sent;

        private FlexiSocket(string ip, int port, IProtocol protocol)
            : this(port, protocol)
        {
            IP = ip;
        }

        void ISocketClient.Connect()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += ConnectCallback;
            args.UserToken = _socket;
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);
            try
            {
                _socket.ConnectAsync(args);
            }
            catch (Exception ex)
            {
                OnConnected(false, ex);
            }
        }

        private void ConnectCallback(object sender, SocketAsyncEventArgs args)
        {
            var socket = (Socket) args.UserToken;
            StartReceive(null, new StateObject(socket, _protocol));
            OnConnected(true, null);
        }

        AsyncConnect ISocketClient.ConnectAsync()
        {
            var @async = new AsyncConnect(_socket, new IPEndPoint(IPAddress.Parse(IP), Port));
            @async.Completed += OnConnected;
            return @async;
        }

        AsyncReceive ISocketClient.ReceiveAsync()
        {
            var @async = new AsyncReceive(_socket, _protocol);
            @async.Completed += OnReceived;
            return @async;
        }

        private void StartReceive(SocketAsyncEventArgs args, StateObject state)
        {
            if (args == null)
            {
                args = new SocketAsyncEventArgs();
                args.Completed += ReceiveCallback;
                args.UserToken = state;
                args.SetBuffer(state.buffer, 0, state.buffer.Length);
            }
            try
            {
                if (!state.handler.ReceiveAsync(args))
                    ReceiveCallback(null, args);
            }
            catch (Exception ex)
            {
                state.Dispose();
                OnReceived(false, ex, args.SocketError, null);
            }
        }

        private void ReceiveCallback(object sender, SocketAsyncEventArgs args)
        {
            var state = (StateObject) args.UserToken;
            if (args.SocketError != SocketError.Success)
            {
                state.Dispose();
                OnReceived(false, null, args.SocketError, null);
            }
            else if (args.BytesTransferred <= 0)
            {
                state.Dispose();
                Close();
            }
            else
            {
                state.stream.Write(state.buffer, 0, args.BytesTransferred);
                if (!state.protocol.CheckComplete(state.stream)) //incompleted
                    StartReceive(args, state);
                else //completed
                {
                    var data = state.protocol.Decode(state.stream);
                    state.Dispose();
                    OnReceived(true, null, args.SocketError, data);
                    OnReceivedString(true, null, args.SocketError, data);
                    StartReceive(null, new StateObject(state.handler, state.protocol));
                }
            }
        }

        public void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
        }

        public void Send(byte[] message)
        {
            StartSend(new StateObject(_socket, _protocol, message), null);
        }

        private void StartSend(StateObject state, SocketAsyncEventArgs args)
        {
            if (args == null)
            {
                args = new SocketAsyncEventArgs();
                args.SetBuffer(state.stream.GetBuffer(), 0, (int) state.stream.Length);
                args.Completed += SentCallback;
                args.UserToken = state;
            }

            try
            {
                _socket.SendAsync(args);
            }
            catch (Exception ex)
            {
                state.Dispose();
                OnSent(false, ex, args.SocketError);
            }
        }

        AsyncSend ISocketClient.SendAsync(byte[] message)
        {
            var @async = new AsyncSend(_socket, _protocol, message);
            @async.Completed += OnSent;
            return @async;
        }

        AsyncSend ISocketClient.SendAsync(string message)
        {
            return ((ISocketClient) this).SendAsync(Encoding.UTF8.GetBytes(message));
        }

        private void SentCallback(object sender, SocketAsyncEventArgs args)
        {
            var state = (StateObject) args.UserToken;
            if (args.SocketError == SocketError.Success)
            {
                if (args.BytesTransferred <= 0)
                {
                    state.Dispose();
                    Close();
                }
                else
                {
                    state.stream.Position += args.BytesTransferred;
                    if (state.stream.Position < state.stream.Length) //not finished yet
                        StartSend(state, args);
                    else
                    {
                        state.Dispose();
                        OnSent(true, null, args.SocketError);
                    }
                }
            }
            else
            {
                state.Dispose();
                OnSent(false, null, args.SocketError);
            }
        }

        void ISocketClient.Disconnect()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += DisconnectCallback;
            try
            {
                _socket.DisconnectAsync(args);
            }
            catch (Exception ex)
            {
                OnDisconnected(false, ex);
            }
        }

        AsyncDisconnect ISocketClient.DisconnectAsync()
        {
            var @async = new AsyncDisconnect(_socket);
            @async.Completed += OnDisconnected;
            return @async;
        }

        private void DisconnectCallback(object sender, SocketAsyncEventArgs args)
        {
            OnDisconnected(true, null);
        }

        #endregion

        #region Token

        public int ID
        {
            get { return _socket.GetHashCode(); }
        }

        #endregion

        #region Server

        private readonly List<ISocketClientToken> _clients = new List<ISocketClientToken>();

        public int Backlog { get; private set; }

        ReadOnlyCollection<ISocketClientToken> ISocketServer.Clients
        {
            get
            {
                lock (_clients)
                    return new ReadOnlyCollection<ISocketClientToken>(_clients);
            }
        }

        public event ClientConnectedCallback ClientConnected;

        public event ReceivedFromClientCallback ReceivedFromClient;

        public event ReceivedStringFromClientCallback ReceivedStringFromClient;

        public event ClientDisconnectedCallback ClientDisconnected;

        public event SentToClientCallback SentToClient;

        private FlexiSocket(int port, IProtocol protocol)
        {
            Port = port;
            _protocol = protocol;
            _socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName) 27, 0);
                _ipv6 = true;
            }
            catch (SocketException exception)
            {
                Debug.LogWarning(exception.Message);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _ipv6 = false;
            }
        }

        private FlexiSocket(Socket socket, IProtocol protocol)
        {
            _socket = socket;
            _protocol = protocol;
        }

        void ISocketServer.StartListen(int backlog)
        {
            Backlog = backlog;
            _socket.Bind(new IPEndPoint(_ipv6 ? IPAddress.IPv6Any : IPAddress.Any, Port));
            _socket.Listen(backlog);
            StartAccept(null);
        }

        void ISocketServer.SendToAll(byte[] message)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    client.Send(message);
                }
            }
        }

        void ISocketServer.SendToAll(string message)
        {
            ((ISocketServer) this).SendToAll(Encoding.UTF8.GetBytes(message));
        }

        private void StartAccept(SocketAsyncEventArgs args)
        {
            if (args == null)
            {
                args = new SocketAsyncEventArgs();
                args.Completed += AcceptCallback;
            }
            else
            {
                args.AcceptSocket = null;
            }
            _socket.AcceptAsync(args);
        }

        private void AcceptCallback(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success && args.AcceptSocket != null)
            {
                var client = new FlexiSocket(args.AcceptSocket, _protocol);
                lock (_clients)
                    _clients.Add(client);
                StartReceive(null, new StateObject(_socket, _protocol));
                if (ReceivedFromClient != null)
                    client.Received += delegate(bool success, Exception exception, SocketError error, byte[] message)
                    {
                        if (success)
                            ReceivedFromClient(client, message);
                    };
                if (ReceivedStringFromClient != null)
                    client.ReceivedString +=
                        delegate(bool success, Exception exception, SocketError error, string message)
                        {
                            if (success)
                                ReceivedStringFromClient(client, message);
                        };
                if (ClientDisconnected != null)
                {
                    client.Closed += delegate
                    {
                        lock (_clients)
                            _clients.Remove(client);
                        ClientDisconnected(client);
                    };
                }
                if (SentToClient != null)
                    client.Sent +=
                        delegate(bool success, Exception exception, SocketError error)
                        {
                            SentToClient(success, client);
                        };
                if (ClientConnected != null)
                    ClientConnected(client);
                StartAccept(args);
            }
        }

        #endregion

        public void Close()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                lock (_clients)
                {
                    _clients.ForEach(client => client.Close());
                    _clients.Clear();
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                OnClosed();
            }
        }


        /// <summary>
        /// Create a client
        /// </summary>
        /// <param name="ip">Server address</param>
        /// <param name="port">Server listening port</param>
        /// <param name="protocol">Protocol</param>
        /// <returns>Created client</returns>
        public static ISocketClient Create(string ip, int port, IProtocol protocol)
        {
            return new FlexiSocket(ip, port, protocol);
        }


        /// <summary>
        /// Create a server
        /// </summary>
        /// <param name="port">Listening port</param>
        /// <param name="protocol">Protocol</param>
        /// <returns>Created server</returns>
        public static ISocketServer Create(int port, IProtocol protocol)
        {
            return new FlexiSocket(port, protocol);
        }

        private void OnClosed()
        {
            var handler = Closed;
            if (handler != null) handler();
        }

        private void OnConnected(bool success, Exception exception)
        {
            var handler = Connected;
            if (handler != null) handler(success, exception);
        }

        private void OnReceived(bool success, Exception exception, SocketError error, byte[] message)
        {
            var handler = Received;
            if (handler != null) handler(success, exception, error, message);
        }

        private void OnReceivedString(bool success, Exception exception, SocketError error, byte[] message)
        {
            var handler = ReceivedString;
            if (handler != null) handler(success, exception, error, Encoding.UTF8.GetString(message));
        }

        private void OnDisconnected(bool success, Exception exception)
        {
            var handler = Disconnected;
            if (handler != null) handler(success, exception);
        }

        private void OnSent(bool success, Exception exception, SocketError error)
        {
            var handler = Sent;
            if (handler != null) handler(success, exception, error);
        }

        private class StateObject : IDisposable
        {
            public readonly Socket handler;
            public readonly MemoryStream stream;
            public readonly IProtocol protocol;
            public readonly byte[] buffer;

            private StateObject(Socket handler, IProtocol protocol, MemoryStream stream, byte[] buffer)
            {
                this.handler = handler;
                this.protocol = protocol;
                this.stream = stream;
                this.buffer = buffer;
            }

            /// <summary>
            /// Receive state
            /// </summary>
            /// <param name="handler"></param>
            /// <param name="protocol"></param>
            public StateObject(Socket handler, IProtocol protocol)
                : this(handler, protocol, new MemoryStream(), new byte[8192])
            {
            }

            /// <summary>
            /// Send state
            /// </summary>
            /// <param name="handler"></param>
            /// <param name="protocol"></param>
            /// <param name="buffer"></param>
            public StateObject(Socket handler, IProtocol protocol, byte[] buffer)
                : this(handler, protocol, null, null)
            {
                var data = protocol.Encode(buffer);
                stream = new MemoryStream(data, 0, data.Length, false, true);
            }

            #region Implementation of IDisposable

            public void Dispose()
            {
                stream.Dispose();
            }

            #endregion
        }
    }
}