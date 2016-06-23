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
using System.IO;
using System.Text;

namespace FlexiFramework.Networking
{
    public interface IProtocol
    {
        /// <summary>
        /// Check message
        /// </summary>
        /// <param name="stream">Message stream</param>
        /// <returns>True if complete</returns>
        bool CheckComplete(MemoryStream stream);

        /// <summary>
        /// Decode message
        /// </summary>
        /// <param name="stream">Message stream</param>
        /// <returns>Decoded data</returns>
        byte[] Decode(MemoryStream stream);

        /// <summary>
        /// Encode message
        /// </summary>
        /// <param name="buffer">Message buffer</param>
        /// <returns>Encoded data</returns>
        byte[] Encode(byte[] buffer);
    }

    public sealed class Protocol
    {
        /// <summary>
        /// Head + Body structure type
        /// </summary>
        /// <remarks>
        /// The message head is a 4-byte int type which represents the length of the coming message
        /// </remarks>
        public static readonly IProtocol LengthPrefix = new LengthPrefixProtocol();

        /// <summary>
        /// Body + TerminatTag structure type
        /// </summary>
        /// <remarks>
        /// The message tail is <c>&lt;EOF&gt;</c> which represents the end of a string message
        /// </remarks>
        public static readonly IProtocol StringTerminated = new StringTerminatedProtocol("<EOF>");

        /// <summary>
        /// Body + TerminatTag structure type
        /// </summary>
        /// <param name="tag">End tag</param>
        /// <returns></returns>
        /// <remarks>
        /// The message tail is a user-defined tag which represents the end of a string message
        /// </remarks>
        public static IProtocol StringTerminatedBy(string tag)
        {
            return new StringTerminatedProtocol(tag);
        }

        /// <summary>
        /// Fixed-length structure type
        /// </summary>
        /// <param name="length">Message length</param>
        /// <returns></returns>
        public static IProtocol FixedLengthOf(int length)
        {
            return new FixedLengthProtocol(length);
        }

        private sealed class LengthPrefixProtocol : IProtocol
        {
            #region Implementation of IProtocol

            public bool CheckComplete(MemoryStream stream)
            {
                if (stream.Length < sizeof (int))
                    return false;
                var position = stream.Position;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new BinaryReader(stream);
                var length = reader.ReadInt32();
                stream.Seek(position, SeekOrigin.Begin);
                return stream.Length >= length + sizeof (int);
            }

            public byte[] Decode(MemoryStream stream)
            {
                if (stream.Length < sizeof (int))
                    throw new InvalidOperationException();
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new BinaryReader(stream);
                var length = reader.ReadInt32();
                if (length + sizeof (int) > stream.Length)
                    throw new InvalidOperationException();
                return reader.ReadBytes(length);
            }

            public byte[] Encode(byte[] buffer)
            {
                var output = new byte[buffer.Length + sizeof (int)];
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

            public bool CheckComplete(MemoryStream stream)
            {
                var position = stream.Position;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new StreamReader(stream);
                var text = reader.ReadToEnd();
                stream.Seek(position, SeekOrigin.Begin);
                return text.EndsWith(tag);
            }

            public byte[] Decode(MemoryStream stream)
            {
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new StreamReader(stream);
                var data = reader.ReadToEnd();
                if (!data.EndsWith(tag))
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

            public bool CheckComplete(MemoryStream stream)
            {
                return stream.Length >= length;
            }

            public byte[] Decode(MemoryStream stream)
            {
                if (stream.Length < length)
                    throw new InvalidOperationException();
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new BinaryReader(stream);
                return reader.ReadBytes(length);
            }

            public byte[] Encode(byte[] buffer)
            {
                if (buffer.Length > length)
                    throw new InvalidOperationException();
                var data = new byte[length];
                buffer.CopyTo(data, 0);
                return data;
            }

            #endregion
        }
    }
}