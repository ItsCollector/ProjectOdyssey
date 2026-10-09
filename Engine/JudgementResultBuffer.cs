using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Engine
{
    public class JudgementResultBuffer
    {
        private readonly JudgementResult[] buffer;
        private int capacity => buffer.Length;
        private readonly object bufferLock = new();
        private int writeIndex = 0;
        private int count = 0;

        public JudgementResultBuffer(int capacity)
        {
            buffer = new JudgementResult[capacity];
        }

        public int CopyTo(JudgementResult[] destination)
        {
            lock (bufferLock)
            {
                for (int i = 0; i < count; i++)
                {
                    destination[i] = buffer[(writeIndex - count + i + buffer.Length) % buffer.Length];
                }

                return count;
            }
        }

        public void Add(JudgementResult result)
        {
            lock (bufferLock)
            {
                buffer[writeIndex] = result;
                writeIndex = (writeIndex + 1) % buffer.Length;
                count = Math.Min(count + 1, buffer.Length);
            }
        }

        // A fresh, isolated copy, oldest-to-newest — safe for the renderer to
        // iterate without racing against new judgements being recorded mid-draw.
        public JudgementResult[] Snapshot()
        {
            lock (bufferLock)
            {
                var result = new JudgementResult[count];

                for (int i = 0; i < count; i++)
                {
                    int idx = (writeIndex - count + i + buffer.Length) % buffer.Length;
                    result[i] = buffer[idx];
                }

                return result;
            }
        }
    }
}
