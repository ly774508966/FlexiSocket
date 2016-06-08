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

using System.Collections.ObjectModel;

namespace FlexiFramework.Networking
{
    /// <summary>
    /// Socket server
    /// </summary>
    public interface ISocketServer
    {
        /// <summary>
        /// Client accepted callback
        /// </summary>
        event ClientConnectedCallback ClientConnected;

        /// <summary>
        /// Received message from client callback
        /// </summary>
        event ReceivedFromClientCallback ReceivedFromClient;

        /// <summary>
        /// Received message from client callback
        /// </summary>
        event ReceivedStringFromClientCallback ReceivedStringFromClient;

        /// <summary>
        /// Client disconnected callback
        /// </summary>
        event ClientDisconnectedCallback ClientDisconnected;

        /// <summary>
        /// Sent to client
        /// </summary>
        event SentToClientCallback SentToClient;

        /// <summary>
        /// Socket closed callback
        /// </summary>
        event ClosedCallback Closed;

        /// <summary>
        /// Max connection
        /// </summary>
        int Backlog { get; }

        /// <summary>
        /// Listening port
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Connected clients
        /// </summary>
        ReadOnlyCollection<ISocketClientToken> Clients { get; }

        /// <summary>
        /// Close the server
        /// </summary>
        void Close();

        /// <summary>
        /// Start listening
        /// </summary>
        /// <param name="backlog">Max connection</param>
        void StartListen(int backlog);

        /// <summary>
        /// Send message to all connected clients
        /// </summary>
        /// <param name="message">Message</param>
        /// <remarks>
        /// This won't trigger <see cref="SentToClient"/>
        /// </remarks>
        void SendToAll(byte[] message);

        /// <summary>
        /// Send message to all connected clients
        /// </summary>
        /// <param name="message">Message</param>
        /// <remarks>
        /// This won't trigger <see cref="SentToClient"/>
        /// </remarks>
        void SendToAll(string message);
    }
}