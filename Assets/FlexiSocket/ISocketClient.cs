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

namespace FlexiFramework.Networking
{
    /// <summary>
    /// Socket client
    /// </summary>
    public interface ISocketClient
    {
        /// <summary>
        /// Connected to server callback
        /// </summary>
        event ConnectedCallback Connected;

        /// <summary>
        /// Received message from server callback
        /// </summary>
        event ReceivedCallback Received;

        /// <summary>
        /// Received message from server callback
        /// </summary>
        event ReceivedStringCallback ReceivedString;

        /// <summary>
        /// Disconnected from server callback
        /// </summary>
        event DisconnectedCallback Disconnected;

        /// <summary>
        /// Message sent to server callback
        /// </summary>
        event SentCallback Sent;

        /// <summary>
        /// Socket closed callback
        /// </summary>
        event ClosedCallback Closed;

        /// <summary>
        /// Server ip address
        /// </summary>
        string IP { get; }

        /// <summary>
        /// Server listening port
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Close the client
        /// </summary>
        void Close();

        /// <summary>
        /// Connect to server
        /// </summary>
        void Connect();

        /// <summary>
        /// Connect to server
        /// </summary>
        /// <returns></returns>
        AsyncConnect ConnectAsync();

        /// <summary>
        /// Receive messages from server
        /// </summary>
        /// <returns></returns>
        AsyncReceive ReceiveAsync();

        /// <summary>
        /// Send message to server
        /// </summary>
        /// <param name="message"></param>
        void Send(byte[] message);

        /// <summary>
        /// Send message to server
        /// </summary>
        /// <param name="message"></param>
        void Send(string message);

        /// <summary>
        /// Send message to server
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        AsyncSend SendAsync(byte[] message);

        /// <summary>
        /// Send message to server
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        AsyncSend SendAsync(string message);

        /// <summary>
        /// Disconnect from server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Disconnect from server
        /// </summary>
        /// <returns></returns>
        AsyncDisconnect DisconnectAsync();
    }
}