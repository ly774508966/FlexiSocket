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
using System.Net.Sockets;

namespace FlexiFramework.Networking
{
    /// <summary>
    /// Async send operation
    /// </summary>
    public sealed class AsyncSend : AsyncIOOperation
    {
        private readonly byte[] _message;
        private readonly SentCallback _callback;

        public AsyncSend(Socket socket, MessageStructure structure, byte[] message,
            SentCallback callback) : base(socket, structure)
        {
            _message = message;
            _callback = callback;
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
                    socket.EndSend(ar);
            }
            catch
            {
                // ignored
            }
        }

        protected internal override IEnumerator GetEnumerator()
        {
            byte[] buffer;
            switch (structure)
            {
                case MessageStructure.LengthPrefixed:
                    var length = _message.Length;
                    var head = BitConverter.GetBytes(length);
                    buffer = new byte[length + head.Length];
                    head.CopyTo(buffer, 0);
                    _message.CopyTo(buffer, head.Length);
                    break;
                case MessageStructure.StringTerminated:
                case MessageStructure.Custom:
                    buffer = _message;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            while (transferedLength < buffer.Length)
            {
                try
                {
                    SocketError error;
                    ar = socket.BeginSend(buffer, transferedLength, buffer.Length - transferedLength, SocketFlags.None,
                        out error,
                        null,
                        null);
                    Error = error;

                    if (Error != SocketError.Success)
                    {
                        if (_callback != null) _callback(false, Exception, Error);
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    Exception = ex;
                    if (_callback != null) _callback(false, Exception, Error);
                    yield break;
                }

                while (!ar.IsCompleted)
                    yield return null;

                try
                {
                    SocketError error;
                    transferedLength += socket.EndSend(ar, out error);
                    Error = error;

                    if (Error != SocketError.Success)
                    {
                        if (_callback != null) _callback(false, Exception, Error);
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    Exception = ex;
                    if (_callback != null) _callback(false, Exception, Error);
                    yield break;
                }
            }

            if (_callback != null) _callback(true, Exception, Error);
        }

        #endregion
    }
}