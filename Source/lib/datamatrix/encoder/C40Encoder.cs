/*
 * Copyright 2006-2007 Jeremias Maerki.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;

namespace ZXing.Datamatrix.Encoder
{
    internal class C40Encoder : Encoder
    {
        public virtual int EncodingMode
        {
            get { return Encodation.C40; }
        }

        public void encodeMaximal(EncoderContext context)
        {
            var buffer = new StringBuilder();
            var lastCharSize = 0;
            var backtrackStartPosition = context.Pos;
            var backtrackBufferLength = 0;
            while (context.HasMoreCharacters)
            {
                char c = context.CurrentChar;
                context.Pos++;
                lastCharSize = encodeChar(c, buffer);
                if (buffer.Length % 3 == 0)
                {
                    backtrackStartPosition = context.Pos;
                    backtrackBufferLength = buffer.Length;
                }
            }
            if (backtrackBufferLength != buffer.Length)
            {
                var unwritten = (buffer.Length / 3) * 2;

                var curCodewordCount = context.CodewordCount + unwritten + 1; // +1 for the latch to C40
                context.updateSymbolInfo(curCodewordCount);
                var available = context.SymbolInfo.dataCapacity - curCodewordCount;
                var rest = buffer.Length % 3;
                if ((rest == 2 && available != 2) ||
                    (rest == 1 && (lastCharSize > 3 || available != 1)))
                {
                    buffer.Capacity = backtrackBufferLength;
                    context.Pos = backtrackStartPosition;
                }
            }
            if (buffer.Length > 0)
            {
                context.writeCodeword(HighLevelEncoder.LATCH_TO_C40);
            }

            handleEOD(context, buffer);
        }

        public virtual void encode(EncoderContext context)
        {
            //step C
            var buffer = new StringBuilder();
            while (context.HasMoreCharacters)
            {
                char c = context.CurrentChar;
                context.Pos++;

                int lastCharSize = encodeChar(c, buffer);

                int unwritten = (buffer.Length / 3) * 2;

                int curCodewordCount = context.CodewordCount + unwritten;
                context.updateSymbolInfo(curCodewordCount);
                int available = context.SymbolInfo.dataCapacity - curCodewordCount;

                if (!context.HasMoreCharacters)
                {
                    //Avoid having a single C40 value in the last triplet
                    var removed = new StringBuilder();
                    if ((buffer.Length % 3) == 2 && available != 2)
                    {
                        lastCharSize = backtrackOneCharacter(context, buffer, removed, lastCharSize);
                    }
                    while ((buffer.Length % 3) == 1 && (lastCharSize > 3 || available != 1))
                    {
                        lastCharSize = backtrackOneCharacter(context, buffer, removed, lastCharSize);
                    }
                    break;
                }

                int count = buffer.Length;
                if ((count % 3) == 0)
                {
                    int newMode = HighLevelEncoder.lookAheadTest(context.Message, context.Pos, EncodingMode);
                    if (newMode != EncodingMode)
                    {
                        // Return to ASCII encodation, which will actually handle latch to new mode
                        context.signalEncoderChange(Encodation.ASCII);
                        break;
                    }
                }
            }
            handleEOD(context, buffer);
        }

        private int backtrackOneCharacter(EncoderContext context,
            StringBuilder buffer, StringBuilder removed, int lastCharSize)
        {
            int count = buffer.Length;
            buffer.Remove(count - lastCharSize, lastCharSize);
            context.Pos--;
            char c = context.CurrentChar;
            lastCharSize = encodeChar(c, removed);
            context.resetSymbolInfo(); //Deal with possible reduction in symbol size
            return lastCharSize;
        }

        internal static void writeNextTriplet(EncoderContext context, StringBuilder buffer)
        {
            context.writeCodewords(encodeToCodewords(buffer));
            buffer.Remove(0, 3);
        }

        /// <summary>
        /// Handle "end of data" situations
        /// </summary>
        /// <param name="context">the encoder context</param>
        /// <param name="buffer">the buffer with the remaining encoded characters</param>
        protected virtual void handleEOD(EncoderContext context, StringBuilder buffer)
        {
            int unwritten = (buffer.Length / 3) * 2;
            int rest = buffer.Length % 3;

            int curCodewordCount = context.CodewordCount + unwritten;
            context.updateSymbolInfo(curCodewordCount);
            int available = context.SymbolInfo.dataCapacity - curCodewordCount;

            if (rest == 2)
            {
                buffer.Append('\u0000'); //Shift 1
                while (buffer.Length >= 3)
                {
                    writeNextTriplet(context, buffer);
                }
                if (context.HasMoreCharacters)
                {
                    context.writeCodeword(HighLevelEncoder.C40_UNLATCH);
                }
            }
            else if (available == 1 && rest == 1)
            {
                while (buffer.Length >= 3)
                {
                    writeNextTriplet(context, buffer);
                }
                if (context.HasMoreCharacters)
                {
                    context.writeCodeword(HighLevelEncoder.C40_UNLATCH);
                }
                // else no unlatch
                context.Pos--;
            }
            else if (rest == 0)
            {
                while (buffer.Length >= 3)
                {
                    writeNextTriplet(context, buffer);
                }
                if (available > 0 || context.HasMoreCharacters)
                {
                    context.writeCodeword(HighLevelEncoder.C40_UNLATCH);
                }
            }
            else
            {
                throw new InvalidOperationException("Unexpected case. Please report!");
            }
            context.signalEncoderChange(Encodation.ASCII);
        }

        protected virtual int encodeChar(char c, StringBuilder sb)
        {
            if (c == ' ')
            {
                sb.Append('\u0003');
                return 1;
            }
            if (c >= '0' && c <= '9')
            {
                sb.Append((char) (c - 48 + 4));
                return 1;
            }
            if (c >= 'A' && c <= 'Z')
            {
                sb.Append((char) (c - 65 + 14));
                return 1;
            }
            if (c <= '\u001f')
            {
                sb.Append('\u0000'); //Shift 1 Set
                sb.Append(c);
                return 2;
            }
            if (c <= '/')
            {
                sb.Append('\u0001'); //Shift 2 Set
                sb.Append((char) (c - 33));
                return 2;
            }
            if (c <= '@')
            {
                sb.Append('\u0001'); //Shift 2 Set
                sb.Append((char) (c - 58 + 15));
                return 2;
            }
            if (c <= '_')
            {
                sb.Append('\u0001'); //Shift 2 Set
                sb.Append((char) (c - 91 + 22));
                return 2;
            }
            if (c <= '\u007f')
            {
                sb.Append('\u0002'); //Shift 3 Set
                sb.Append((char) (c - 96));
                return 2;
            }
            sb.Append("\u0001\u001e"); //Shift 2, Upper Shift
            int len = 2;
            len += encodeChar((char) (c - 128), sb);
            return len;
        }

        private static String encodeToCodewords(StringBuilder sb)
        {
            int v = (1600 * sb[0]) + (40 * sb[1]) + sb[2] + 1;
            char cw1 = (char) (v / 256);
            char cw2 = (char) (v % 256);
            return new String(new char[] {cw1, cw2});
        }
    }
}