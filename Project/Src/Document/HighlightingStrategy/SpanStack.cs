// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>
    /// A stack of Span instances. Works like Stack&lt;Span&gt;, but can be cloned quickly
    /// because it is implemented as linked list.
    /// </summary>
    public sealed class SpanStack : ICloneable, IEnumerable<Span>
    {
        internal sealed class StackNode
        {
            public readonly StackNode Previous;
            public readonly Span Data;

            public StackNode(StackNode previous, Span data)
            {
                Previous = previous;
                Data = data;
            }
        }

        private StackNode top;

        public Span Pop()
        {
            Span s = top.Data;
            top = top.Previous;
            return s;
        }

        public Span Peek()
        {
            return top.Data;
        }

        public void Push(Span s)
        {
            top = new StackNode(top, s);
        }

        public bool IsEmpty => top == null;

        public SpanStack Clone()
        {
            SpanStack n = new SpanStack();
            n.top = top;
            return n;
        }
        object ICloneable.Clone()
        {
            return Clone();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(new StackNode(top, null));
        }
        IEnumerator<Span> IEnumerable<Span>.GetEnumerator()
        {
            return GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<Span>
        {
            private StackNode c;

            internal Enumerator(StackNode node)
            {
                c = node;
            }

            public Span Current => c.Data;

            object IEnumerator.Current => c.Data;

            public void Dispose()
            {
                c = null;
            }

            public bool MoveNext()
            {
                c = c.Previous;
                return c != null;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
