namespace QM2D.Generator
{
    /// <summary>
    /// Transformations that can be done on a Vector2i.
    /// </summary>
    public enum Transforms
    {
        Rotate90CW = 0,
        Rotate180,
        Rotate270CW,

        MirrorX,
        MirrorY,

        None,

        NumberOfTransforms,
    }
    public static class TransformsExtensions
    {
        /// <summary>
        /// Gets the inverse of the given transformation.
        /// </summary>
        public static Transforms Inverse(this Transforms tr)
        {
            switch (tr)
            {
                case Transforms.Rotate90CW: return Transforms.Rotate270CW;
                case Transforms.Rotate270CW: return Transforms.Rotate90CW;

                case Transforms.Rotate180: return Transforms.None;

                case Transforms.MirrorX: return Transforms.MirrorX;
                case Transforms.MirrorY: return Transforms.MirrorY;

                case Transforms.None: return Transforms.None;

                default:
                    System.Windows.MessageBox.Show("Unknown Transforms type " + tr.ToString());
                    return Transforms.None;
            }
        }
    }


    public struct Vector2i : System.IEquatable<Vector2i>
    {
        public static Vector2i Zero { get { return new Vector2i(0, 0); } }

        public int x, y;

        public Vector2i(int _x, int _y) { x = _x; y = _y; }

        public Vector2i LessX { get { return new Vector2i(x - 1, y); } }
        public Vector2i LessY { get { return new Vector2i(x, y - 1); } }
        public Vector2i MoreX { get { return new Vector2i(x + 1, y); } }
        public Vector2i MoreY { get { return new Vector2i(x, y + 1); } }

        //The following transformations assume the pivot is halfway between {0, 0} and some "size".
        public Vector2i MirrorX(Vector2i size) { return new Vector2i(size.x - x - 1, y); }
        public Vector2i MirrorY(Vector2i size) { return new Vector2i(x, size.y - y - 1); }
        public Vector2i Rot90CW(Vector2i size)
        {
            //Swap X and Y, then mirror the new X.
            Vector2i result = new Vector2i(y, x);
            result.x = size.y - result.x - 1;
            return result;
        }
        public Vector2i Rot180(Vector2i size)
        {
            //Just mirror along both axes.
            return size - this - 1;
        }
        public Vector2i Rot270CW(Vector2i size)
        {
            //Swap X and Y, then mirror the new Y.
            Vector2i result = new Vector2i(y, x);
            result.y = size.x - result.y - 1;
            return result;
        }

        public static Vector2i operator +(Vector2i a, Vector2i b) { return new Vector2i(a.x + b.x, a.y + b.y); }
        public static Vector2i operator +(Vector2i a, int b) { return new Vector2i(a.x + b, a.y + b); }
        public static Vector2i operator -(Vector2i a, Vector2i b) { return new Vector2i(a.x - b.x, a.y - b.y); }
        public static Vector2i operator -(Vector2i a, int b) { return new Vector2i(a.x - b, a.y - b); }
        public static Vector2i operator *(Vector2i a, int b) { return new Vector2i(a.x * b, a.y * b); }
        public static Vector2i operator /(Vector2i a, int b) { return new Vector2i(a.x / b, a.y / b); }
        public static Vector2i operator -(Vector2i a) { return new Vector2i(-a.x, -a.y); }

        public static bool operator ==(Vector2i a, Vector2i b) { return a.x == b.x && a.y == b.y; }
        public static bool operator !=(Vector2i a, Vector2i b) { return !(a == b); }

        /// <summary>
        /// Transforms this Vector2i by the given transformation.
        /// </summary>
        /// <param name="size">
        /// The exclusive upper-bound on this Vector2i's size.
        /// Used to find the pivot for transformations (specifically, "size / 2").
        /// </param>
        public Vector2i Transform(Transforms transform, Vector2i size)
        {
            switch (transform)
            {
                case Transforms.Rotate90CW: return Rot90CW(size);
                case Transforms.Rotate180: return Rot180(size);
                case Transforms.Rotate270CW: return Rot270CW(size);
                case Transforms.MirrorX: return MirrorX(size);
                case Transforms.MirrorY: return MirrorY(size);
                case Transforms.None: return this;
                default:
                    System.Windows.MessageBox.Show("Unknown transform: " + transform.ToString());
                    return this;
            }
        }

        public override string ToString()
        {
            return "{" + x + ", " + y + "}";
        }
        public override int GetHashCode()
        {
            return (x * 73856093) ^ (y * 19349663);
        }
        public override bool Equals(object obj)
        {
            return (obj is Vector2i) && ((Vector2i)obj) == this;
        }
        public bool Equals(Vector2i v) { return v == this; }


        #region Iterator definition
        public struct Iterator
        {
            public Vector2i MinInclusive { get { return minInclusive; } }
            public Vector2i MaxExclusive { get { return maxExclusive; } }
            public Vector2i Current { get { return current; } }

            private Vector2i minInclusive, maxExclusive, current;

            public Iterator(Vector2i maxExclusive) : this(Vector2i.Zero, maxExclusive) { }
            public Iterator(Vector2i _minInclusive, Vector2i _maxExclusive)
            {
                minInclusive = _minInclusive;
                maxExclusive = _maxExclusive;

                current = Vector2i.Zero; //Just to make the compiler shut up
                Reset();
            }

            public bool MoveNext()
            {
                current.x += 1;
                if (current.x >= maxExclusive.x)
                    current = new Vector2i(minInclusive.x, current.y + 1);

                return (current.y < maxExclusive.y);
            }
            public void Reset() { current = new Vector2i(minInclusive.x - 1, minInclusive.y); }
            public void Dispose() { }

            public Iterator GetEnumerator() { return this; }
        }
        #endregion
    }
}