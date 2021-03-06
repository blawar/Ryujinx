//
// Copyright (c) 2019-2020 Ryujinx
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Ryujinx.Audio.Renderer.Utils
{
    public sealed unsafe class SpanMemoryManager<T> : MemoryManager<T>
        where T : unmanaged
    {
        private readonly T* _pointer;
        private readonly int _length;

        public SpanMemoryManager(Span<T> span)
        {
            fixed (T* ptr = &MemoryMarshal.GetReference(span))
            {
                _pointer = ptr;
                _length = span.Length;
            }
        }

        public override Span<T> GetSpan() => new Span<T>(_pointer, _length);

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if (elementIndex < 0 || elementIndex >= _length)
            {
                throw new ArgumentOutOfRangeException(nameof(elementIndex));
            }

            return new MemoryHandle(_pointer + elementIndex);
        }

        public override void Unpin() { }

        protected override void Dispose(bool disposing) { }

        public static Memory<T> Cast<TFrom>(Memory<TFrom> memory) where TFrom : unmanaged
        {
            return new SpanMemoryManager<T>(MemoryMarshal.Cast<TFrom, T>(memory.Span)).Memory;
        }
    }
}
