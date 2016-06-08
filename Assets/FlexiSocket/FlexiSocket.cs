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
using System.Linq;
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
        private readonly MessageStructure _structure;
        private readonly string _endTag;

        public int Port { get; private set; }
        public event ClosedCallback Closed;

        #region client

        public string IP { get; private set; }

        public event ConnectedCallback Connected;

        public event ReceivedCallback Received;

        public event ReceivedStringCallback ReceivedString;

        public event DisconnectedCallback Disconnected;

        public event SentCallback Sent;

        private FlexiSocket(string ip, int port, MessageStructure structure, string endTag)
            : this(port, structure, endTag)
        {
            IP = ip;
        }

        void ISocketClient.Connect()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += OnConnected;
            args.UserToken = _socket;
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);
            try
            {
                _socket.ConnectAsync(args);
            }
            catch (Exception ex)
            {
                if (Connected != null)
                    Connected(false, ex);
            }
        }

        private void OnConnected(object sender, SocketAsyncEventArgs args)
        {
            var socket = (Socket) args.UserToken;
            switch (_structure)
            {
                case MessageStructure.LengthPrefixed:
                    StartReceive(null, new StateObject(socket));
                    break;
                case MessageStructure.StringTerminated:
                    StartReceive(null, new StateObject(socket, new StringBuilder()));
                    break;
                case MessageStructure.Custom:
                    StartReceive(null, new StateObject(socket, new byte[8192]));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (Connected != null)
                Connected(true, null);
        }

        AsyncConnect ISocketClient.ConnectAsync()
        {
            return new AsyncConnect(_socket, new IPEndPoint(IPAddress.Parse(IP), Port), Connected);
        }

        AsyncReceive ISocketClient.ReceiveAsync()
        {
            return new AsyncReceive(_socket, _structure, Received, ReceivedString, _endTag);
        }

        private void StartReceive(SocketAsyncEventArgs args, StateObject state)
        {
            if (args == null)
            {
                args = new SocketAsyncEventArgs();
                args.Completed += OnReceived;
                args.UserToken = state;
                switch (_structure)
                {
                    case MessageStructure.LengthPrefixed:
                        args.SetBuffer(state.head, 0, state.head.Length);
                        break;
                    case MessageStructure.StringTerminated:
                    case MessageStructure.Custom:
                        args.SetBuffer(state.body, 0, state.body.Length);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            try
            {
                if (!state.handler.ReceiveAsync(args))
                    OnReceived(null, args);
            }
            catch (Exception ex)
            {
                if (Received != null) Received(false, ex, args.SocketError, null);
            }
        }

        private void OnReceived(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                if (Received != null)
                    Received(false, null, args.SocketError, null);
            }
            else if (args.BytesTransferred > 0)
            {
                var state = (StateObject) args.UserToken;
                switch (_structure)
                {
                    case MessageStructure.LengthPrefixed:
                    {
                        state.length += args.BytesTransferred;
                        if (state.length < state.head.Length) //unable to decide packet size(incomplete head)
                            StartReceive(args, state);
                        else
                        {
                            if (state.body == null) //head received
                            {
                                state.body = new byte[BitConverter.ToInt32(state.head, 0)];
                                args.SetBuffer(state.body, 0, state.body.Length);
                                StartReceive(args, state);
                            }
                            else if (state.length < state.body.Length + state.head.Length) //incomplete packet
                                StartReceive(args, state);
                            else
                            {
                                if (Received != null) //dispatch message
                                    Received(true, null, args.SocketError, state.body);
                                StartReceive(null, new StateObject(state.handler));
                            }
                        }
                    }
                        break;
                    case MessageStructure.StringTerminated:
                    {
                        state.builder.Append(Encoding.UTF8.GetString(state.body, 0, args.BytesTransferred));
                        var content = state.builder.ToString();
                        if (content.EndsWith(_endTag)) //check endtag
                        {
                            if (ReceivedString != null)
                                ReceivedString(true, null, args.SocketError,
                                    content.Substring(0, content.Length - _endTag.Length));
                            if (Received != null)
                                Received(true, null, args.SocketError,
                                    Encoding.UTF8.GetBytes(content.Substring(0, content.Length - _endTag.Length)));

                            StartReceive(null, new StateObject(state.handler, new StringBuilder()));
                        }
                        else
                        {
                            StartReceive(null, new StateObject(state.handler, state.builder));
                        }
                    }
                        break;
                    case MessageStructure.Custom:
                    {
                        if (Received != null) //dispatch message
                        {
                            var buffer = state.body.Take(args.BytesTransferred).ToArray();
                            Received(true, null, args.SocketError, buffer);
                        }
                        StartReceive(null, new StateObject(state.handler, new byte[8192]));
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                Close(); //shutdown
            }
        }

        public void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
        }

        public void Send(byte[] message)
        {
            switch (_structure)
            {
                case MessageStructure.LengthPrefixed:
                    var length = message.Length;
                    var head = BitConverter.GetBytes(length);
                    var state = new StateObject(_socket, head, message);
                    StartSend(state, null);
                    break;
                case MessageStructure.StringTerminated:
                    var msg = Encoding.UTF8.GetString(message);
                    if (!msg.EndsWith(_endTag))
                        msg += _endTag;
                    StartSend(new StateObject(_socket, Encoding.UTF8.GetBytes(msg)), null);
                    break;
                case MessageStructure.Custom:
                    StartSend(new StateObject(_socket, message), null);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void StartSend(StateObject state, SocketAsyncEventArgs args)
        {
            if (args == null)
            {
                args = new SocketAsyncEventArgs();
                switch (_structure)
                {
                    case MessageStructure.LengthPrefixed:
                        var buffer = new byte[state.head.Length + state.body.Length];
                        state.head.CopyTo(buffer, 0);
                        state.body.CopyTo(buffer, state.head.Length);
                        args.SetBuffer(buffer, 0, buffer.Length);
                        break;
                    case MessageStructure.StringTerminated:
                    case MessageStructure.Custom:
                        args.SetBuffer(state.body, 0, state.body.Length);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                args.Completed += OnSent;
                args.UserToken = state;
            }

            try
            {
                _socket.SendAsync(args);
            }
            catch (Exception ex)
            {
                if (Sent != null)
                    Sent(false, ex, args.SocketError);
            }
        }

        AsyncSend ISocketClient.SendAsync(byte[] message)
        {
            return new AsyncSend(_socket, _structure, message, Sent);
        }

        AsyncSend ISocketClient.SendAsync(string message)
        {
            if (!message.EndsWith(_endTag))
                message += _endTag;
            return ((ISocketClient) this).SendAsync(Encoding.UTF8.GetBytes(message));
        }

        private void OnSent(object sender, SocketAsyncEventArgs args)
        {
            var state = (StateObject) args.UserToken;
            if (args.SocketError == SocketError.Success)
            {
                if (args.BytesTransferred > 0)
                {
                    state.length += args.BytesTransferred;
                    if (state.length < state.head.Length + state.body.Length) //not finished yet
                    {
                        StartSend(state, args);
                    }
                    else
                    {
                        if (Sent != null)
                            Sent(true, null, args.SocketError);
                    }
                }
                else
                {
                    Close();
                }
            }
            else
            {
                if (Sent != null)
                    Sent(false, null, args.SocketError);
            }
        }

        void ISocketClient.Disconnect()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += OnDisconnected;
            try
            {
                _socket.DisconnectAsync(args);
            }
            catch (Exception ex)
            {
                if (Disconnected != null)
                    Disconnected(false, ex);
            }
        }

        AsyncDisconnect ISocketClient.DisconnectAsync()
        {
            return new AsyncDisconnect(_socket, Disconnected);
        }

        private void OnDisconnected(object sender, SocketAsyncEventArgs args)
        {
            if (Disconnected != null)
                Disconnected(true, null);
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

        private FlexiSocket(int port, MessageStructure structure, string endTag)
        {
            Port = port;
            _structure = structure;
            _endTag = endTag;
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

        private FlexiSocket(Socket socket)
        {
            _socket = socket;
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
            if (!message.EndsWith(_endTag))
                message += _endTag;
            ((ISocketServer) this).SendToAll(Encoding.UTF8.GetBytes(message));
        }

        private void StartAccept(SocketAsyncEventArgs args)
        {
            if (args == null)
            {
                args = new SocketAsyncEventArgs();
                args.Completed += OnAccepted;
            }
            else
            {
                args.AcceptSocket = null;
            }
            _socket.AcceptAsync(args);
        }

        private void OnAccepted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success && args.AcceptSocket != null)
            {
                var client = new FlexiSocket(args.AcceptSocket);
                lock (_clients)
                    _clients.Add(client);
                switch (_structure)
                {
                    case MessageStructure.LengthPrefixed:
                        StartReceive(null, new StateObject(client._socket));
                        break;
                    case MessageStructure.StringTerminated:
                        StartReceive(null, new StateObject(client._socket, new StringBuilder()));
                        break;
                    case MessageStructure.Custom:
                        StartReceive(null, new StateObject(client._socket, new byte[8192]));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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
                if (Closed != null)
                    Closed();
            }
        }

        /// <summary>
        /// Create a client
        /// </summary>
        /// <param name="ip">Server address</param>
        /// <param name="port">Server listening port</param>
        /// <returns>Created client</returns>
        public static ISocketClient Create(string ip, int port)
        {
            return Create(ip, port, MessageStructure.LengthPrefixed);
        }

        /// <summary>
        /// Create a client
        /// </summary>
        /// <param name="ip">Server address</param>
        /// <param name="port">Server listening port</param>
        /// <param name="structure"></param>
        /// <param name="endTag"></param>
        /// <returns>Created client</returns>
        public static ISocketClient Create(string ip, int port, MessageStructure structure, string endTag = "<EOF>")
        {
            return new FlexiSocket(ip, port, structure, endTag);
        }

        /// <summary>
        /// Create a server
        /// </summary>
        /// <param name="port">Listening port</param>
        /// <returns>Created server</returns>
        public static ISocketServer Create(int port)
        {
            return Create(port, MessageStructure.LengthPrefixed);
        }

        /// <summary>
        /// Create a server
        /// </summary>
        /// <param name="port">Listening port</param>
        /// <param name="structure"></param>
        /// <param name="endTag"></param>
        /// <returns>Created server</returns>
        public static ISocketServer Create(int port, MessageStructure structure, string endTag = "<EOF>")
        {
            return new FlexiSocket(port, structure, endTag);
        }

        private class StateObject
        {
            public readonly Socket handler;
            public readonly StringBuilder builder;
            public readonly byte[] head;
            public byte[] body;
            public int length;

            public StateObject(Socket handler)
            {
                this.handler = handler;
                head = new byte[sizeof (int)];
            }

            public StateObject(Socket handler, byte[] body)
            {
                this.handler = handler;
                this.body = body;
                head = new byte[0];
            }

            public StateObject(Socket handler, byte[] head, byte[] body)
            {
                this.handler = handler;
                this.body = body;
                this.head = head;
            }

            public StateObject(Socket handler, StringBuilder builder)
            {
                this.handler = handler;
                this.builder = builder;
                body = new byte[8192];
                head = new byte[0];
            }
        }
    }
}