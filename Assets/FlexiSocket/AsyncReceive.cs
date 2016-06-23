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
        private readonly IProtocol _protocol;
        private readonly ReceivedCallback _callback;
        private readonly ReceivedStringCallback _stringCallback;
        private readonly string _endTag;

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

        public AsyncReceive(Socket socket, MessageStructure structure, ReceivedCallback callback,
            ReceivedStringCallback stringCallback, string endTag)
            : base(socket, structure)
        {
            _callback = callback;
            _stringCallback = stringCallback;
            _endTag = endTag;
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
            using (MemoryStream stream = new MemoryStream())
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
                            if (_callback != null) _callback(false, Exception, Error, null);
                            if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception = ex;
                        if (_callback != null) _callback(false, Exception, Error, null);
                        if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
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
                        if (_callback != null) _callback(false, Exception, Error, null);
                        if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                        yield break;
                    }

                } while (!_protocol.CheckComplete(stream));

                Data = _protocol.Decode(stream);
            }


            /*switch (structure)
            {
                case MessageStructure.LengthPrefixed:
                {
                    var head = new byte[sizeof (int)];
                    while (transferedLength < head.Length)
                    {
                        try
                        {
                            SocketError error;
                            ar = socket.BeginReceive(head, transferedLength, head.Length - transferedLength,
                                SocketFlags.None, out error, null, null);
                            Error = error;

                            if (Error != SocketError.Success)
                            {
                                if (_callback != null) _callback(false, Exception, Error, null);
                                if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                                yield break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Exception = ex;
                            if (_callback != null) _callback(false, Exception, Error, null);
                            if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                            yield break;
                        }

                        while (!ar.IsCompleted)
                            yield return null;

                        try
                        {
                            transferedLength += socket.EndReceive(ar);
                        }
                        catch (Exception ex)
                        {
                            Exception = ex;
                            if (_callback != null) _callback(false, Exception, Error, null);
                            if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                            yield break;
                        }
                    }

                    var body = new byte[BitConverter.ToInt32(head, 0)];
                    while (transferedLength < head.Length + body.Length)
                    {
                        try
                        {
                            SocketError error;
                            ar = socket.BeginReceive(body, transferedLength - head.Length,
                                body.Length - transferedLength + head.Length, SocketFlags.None, out error, null, null);
                            Error = error;

                            if (Error != SocketError.Success)
                            {
                                if (_callback != null) _callback(false, Exception, Error, null);
                                if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                                yield break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Exception = ex;
                            if (_callback != null) _callback(false, Exception, Error, null);
                            if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                            yield break;
                        }

                        while (!ar.IsCompleted)
                            yield return null;

                        try
                        {
                            SocketError error;
                            transferedLength += socket.EndReceive(ar, out error);
                            Error = error;

                            if (Error != SocketError.Success)
                            {
                                if (_callback != null) _callback(false, Exception, Error, null);
                                if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                                yield break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Exception = ex;
                            if (_callback != null) _callback(false, Exception, Error, null);
                            if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                            yield break;
                        }
                    }

                    Data = body;
                }
                    break;
                case MessageStructure.StringTerminated:
                {
                    var builder = new StringBuilder();
                    while (!builder.ToString().EndsWith(_endTag))
                    {
                        var buffer = new byte[8192];
                        try
                        {
                            SocketError error;
                            ar = socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, out error, null, null);
                            Error = error;

                            if (Error != SocketError.Success)
                            {
                                if (_callback != null) _callback(false, Exception, Error, null);
                                if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                                yield break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Exception = ex;
                            if (_callback != null) _callback(false, Exception, Error, null);
                            if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                            yield break;
                        }

                        while (!ar.IsCompleted)
                            yield return null;

                        int length;
                        try
                        {
                            SocketError error;
                            length = socket.EndReceive(ar, out error);
                            transferedLength += length;
                            Error = error;

                            if (Error != SocketError.Success)
                            {
                                if (_callback != null) _callback(false, Exception, Error, null);
                                if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                                yield break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Exception = ex;
                            if (_callback != null) _callback(false, Exception, Error, null);
                            if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                            yield break;
                        }

                        builder.Append(Encoding.UTF8.GetString(buffer, 0, length));
                    }
                    Data = Encoding.UTF8.GetBytes(builder.ToString(0, builder.Length - _endTag.Length));
                }
                    break;
                case MessageStructure.Custom:

                {
                    var body = new byte[8192];
                    try
                    {
                        SocketError error;
                        ar = socket.BeginReceive(body, 0, body.Length, SocketFlags.None, out error, null, null);
                        Error = error;

                        if (Error != SocketError.Success)
                        {
                            if (_callback != null) _callback(false, Exception, Error, null);
                            if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception = ex;
                        if (_callback != null) _callback(false, Exception, Error, null);
                        if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                        yield break;
                    }

                    while (!ar.IsCompleted)
                        yield return null;

                    try
                    {
                        SocketError error;
                        transferedLength += socket.EndReceive(ar, out error);
                        Error = error;

                        if (Error != SocketError.Success)
                        {
                            if (_callback != null) _callback(false, Exception, Error, null);
                            if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception = ex;
                        if (_callback != null) _callback(false, Exception, Error, null);
                        if (_stringCallback != null) _stringCallback(false, Exception, Error, null);
                        yield break;
                    }

                    Data = body;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }*/

            if (_callback != null) _callback(true, Exception, Error, Data);
            if (_stringCallback != null) _stringCallback(true, Exception, Error, Encoding.UTF8.GetString(Data));
        }

        #endregion
    }
}