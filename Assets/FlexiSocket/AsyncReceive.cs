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
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace FlexiFramework.Networking
{
    /// <summary>
    /// Async receive operation
    /// </summary>
    public sealed class AsyncReceive : AsyncIOOperation
    {
        public event ReceivedCallback Completed;
        public event ReceivedStringCallback CompletedAsString;

        /// <summary>
        /// Received data
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Received data
        /// </summary>
        public string StringData
        {
            get { return Encoding.UTF8.GetString(Data); }
        }

        public AsyncReceive(Socket socket, IProtocol protocol) : base(socket, protocol)
        {
            
        }

        #region Overrides of AsyncSocketOperation

        public override bool IsSuccessful
        {
            get { return IsCompleted && Exception == null && Error == SocketError.Success; }
        }

        public override void Dispose()
        {
            try
            {
                if (ar != null && !ar.IsCompleted)
                    socket.EndReceive(ar);
            }
            catch
            {
                // ignored
            }
        }

        protected internal override IEnumerator GetEnumerator()
        {
            using (var stream = new MemoryStream())
            {
                var buffer = new byte[8192];
                do
                {
                    try
                    {
                        SocketError error;
                        ar = socket.BeginReceive(buffer, 0, buffer.Length,
                            SocketFlags.None, out error, null, null);
                        Error = error;

                        if (Error != SocketError.Success)
                        {
                            OnCompleted(false, Exception, Error, null);
                            OnCompletedAsString(false, Exception, Error, null);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception = ex;
                        OnCompleted(false, Exception, Error, null);
                        OnCompletedAsString(false, Exception, Error, null);
                        yield break;
                    }

                    while (!ar.IsCompleted)
                        yield return null;

                    try
                    {
                        var length = socket.EndReceive(ar);
                        stream.Write(buffer, 0, length);
                    }
                    catch (Exception ex)
                    {
                        Exception = ex;
                        OnCompleted(false, Exception, Error, null);
                        OnCompletedAsString(false, Exception, Error, null);
                        yield break;
                    }
                } while (!Protocol.CheckComplete(stream));

                Data = Protocol.Decode(stream);
            }


            OnCompleted(true, Exception, Error, Data);
            OnCompletedAsString(true, Exception, Error, Encoding.UTF8.GetString(Data));
        }

        #endregion

        private void OnCompleted(bool success, Exception exception, SocketError error, byte[] message)
        {
            var handler = Completed;
            if (handler != null) handler(success, exception, error, message);
        }

        private void OnCompletedAsString(bool success, Exception exception, SocketError error, string message)
        {
            var handler = CompletedAsString;
            if (handler != null) handler(success, exception, error, message);
        }
    }
}