namespace Akka.Pathfinder.Layout;

public static class Extensions
{
    private class ChunkedEnumerable<T> : IEnumerable<T>
    {
        private class ChildEnumerator : IEnumerator<T>
        {
            private readonly ChunkedEnumerable<T> parent;
            private int position;
            private bool done = false;
            private T current;


            public ChildEnumerator(ChunkedEnumerable<T> parent)
            {
                this.parent = parent;
                position = -1;
                parent.wrapper.AddRef();
            }

            public T Current
            {
                get
                {
                    if (position == -1 || done)
                    {
                        throw new InvalidOperationException();
                    }
                    return current;

                }
            }

            public void Dispose()
            {
                if (!done)
                {
                    done = true;
                    parent.wrapper.RemoveRef();
                }
            }

            object System.Collections.IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                position++;

                if (position + 1 > parent.chunkSize)
                {
                    done = true;
                }

                if (!done)
                {
                    done = !parent.wrapper.Get(position + parent.start, out current);
                }

                return !done;

            }

            public void Reset()
            {
                // per http://msdn.microsoft.com/en-us/library/system.collections.ienumerator.reset.aspx
                throw new NotSupportedException();
            }
        }

        private readonly EnumeratorWrapper<T> wrapper;
        private readonly int chunkSize;
        private readonly int start;

        public ChunkedEnumerable(EnumeratorWrapper<T> wrapper, int chunkSize, int start)
        {
            this.wrapper = wrapper;
            this.chunkSize = chunkSize;
            this.start = start;
        }

        public IEnumerator<T> GetEnumerator() => new ChildEnumerator(this);

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class EnumeratorWrapper<T>
    {
        public EnumeratorWrapper(IEnumerable<T> source)
        {
            SourceEumerable = source;
        }

        private IEnumerable<T> SourceEumerable { get; set; }

        private Enumeration currentEnumeration;

        private class Enumeration
        {
            public IEnumerator<T> Source { get; set; } = null!;
            public int Position { get; set; }
            public bool AtEnd { get; set; }
        }

        public bool Get(int pos, out T item)
        {

            if (currentEnumeration != null && currentEnumeration.Position > pos)
            {
                currentEnumeration.Source.Dispose();
                currentEnumeration = null!;
            }

            if (currentEnumeration == null)
            {
                currentEnumeration = new Enumeration { Position = -1, Source = SourceEumerable.GetEnumerator(), AtEnd = false };
            }

            item = default!;
            if (currentEnumeration.AtEnd)
            {
                return false;
            }

            while (currentEnumeration.Position < pos)
            {
                currentEnumeration.AtEnd = !currentEnumeration.Source.MoveNext();
                currentEnumeration.Position++;

                if (currentEnumeration.AtEnd)
                {
                    return false;
                }

            }

            item = currentEnumeration.Source.Current;

            return true;
        }

        private int refs = 0;

        // needed for dispose semantics 
        public void AddRef()
        {
            refs++;
        }

        public void RemoveRef()
        {
            refs--;
            if (refs == 0 && currentEnumeration != null)
            {
                var copy = currentEnumeration;
                currentEnumeration = null!;
                copy.Source.Dispose();
            }
        }
    }

    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
    {
        if (chunksize < 1) throw new InvalidOperationException();

        var wrapper = new EnumeratorWrapper<T>(source);

        int currentPos = 0;
        try
        {
            wrapper.AddRef();
            while (wrapper.Get(currentPos, out T ignore))
            {
                yield return new ChunkedEnumerable<T>(wrapper, chunksize, currentPos);
                currentPos += chunksize;
            }
        }
        finally
        {
            wrapper.RemoveRef();
        }
    }
}
