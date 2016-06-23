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
using System.Linq;
using System.Text;

namespace FlexiFramework.Networking
{
    public interface IProtocol
    {
        /// <summary>
        /// Check message
        /// </summary>
        /// <param name="buffer">Message buffer</param>
        /// <returns>True if complete</returns>
        bool CheckComplete(byte[] buffer);

        /// <summary>
        /// Decode message
        /// </summary>
        /// <param name="buffer">Message buffer</param>
        /// <returns>Decoded data</returns>
        byte[] Decode(byte[] buffer);

        /// <summary>
        /// Encode message
        /// </summary>
        /// <param name="buffer">Message buffer</param>
        /// <returns>Encoded data</returns>
        byte[] Encode(byte[] buffer);
    }

    public sealed class Protocol
    {
        public static readonly IProtocol LengthPrefix = new LengthPrefixProtocol();
        public static readonly IProtocol StringTerminated = new StringTerminatedProtocol("<EOF>");

        public static IProtocol StringTerminatedBy(string tag)
        {
            return new StringTerminatedProtocol(tag);
        }

        public static IProtocol FixedLengthOf(int length)
        {
            return new FixedLengthProtocol(length);
        }

        private sealed class LengthPrefixProtocol : IProtocol
        {
            
            #region Implementation of IProtocol

            public bool CheckComplete(byte[] buffer)
            {
                if (buffer.Length < sizeof (int))
                    return false;
                var length = BitConverter.ToInt32(buffer, 0);
                return buffer.Length >= length + sizeof (int);
            }

            public byte[] Decode(byte[] buffer)
            {
                if (buffer.Length < sizeof (int))
                    throw new InvalidOperationException();
                var length = BitConverter.ToInt32(buffer, 0);
                if(length + sizeof(int) > buffer.Length)
                    throw new InvalidOperationException();
                return buffer.Skip(sizeof (int)).Take(length).ToArray();
            }

            public byte[] Encode(byte[] buffer)
            {
                var output = new byte[buffer.Length + sizeof(int)];
                var prefix = BitConverter.GetBytes(buffer.Length);
                prefix.CopyTo(output, 0);
                buffer.CopyTo(output, prefix.Length);
                return output;
            }

            #endregion
        }

        private sealed class StringTerminatedProtocol : IProtocol
        {
            private readonly string tag;

            public StringTerminatedProtocol(string tag)
            {
                this.tag = tag;
            }

            #region Implementation of IProtocol

            public bool CheckComplete(byte[] buffer)
            {
                return BitConverter.ToString(buffer).EndsWith(tag);
            }

            public byte[] Decode(byte[] buffer)
            {
                var data =  Encoding.UTF8.GetString(buffer);
                if(!data.EndsWith(tag))
                    throw new InvalidOperationException();
                return Encoding.UTF8.GetBytes(data.Substring(0, data.Length - tag.Length));
            }

            public byte[] Encode(byte[] buffer)
            {
                return Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(buffer) + tag);
            }

            #endregion
        }

        private sealed class FixedLengthProtocol : IProtocol
        {
            private readonly int length;

            public FixedLengthProtocol(int length)
            {
                this.length = length;
            }

            #region Implementation of IProtocol

            public bool CheckComplete(byte[] buffer)
            {
                return buffer.Length >= length;
            }

            public byte[] Decode(byte[] buffer)
            {
                if (buffer.Length < length)
                    throw new InvalidOperationException();

                return buffer.Take(length).ToArray();
            }

            public byte[] Encode(byte[] buffer)
            {
                if(buffer.Length > length)
                    throw new InvalidOperationException();
                var data = new byte[length];
                buffer.CopyTo(data, 0);
                return data;
            }

            #endregion
        }
    }
}