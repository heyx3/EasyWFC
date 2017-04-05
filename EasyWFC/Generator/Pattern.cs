using System;

using Color = System.Windows.Media.Color;


namespace QM2D.Generator
{
    /// <summary>
    /// A section of the input data that can be matched against the output data.
    /// </summary>
    public struct Pattern : IEquatable<Pattern>
    {
        /// <summary>
        /// This pattern's pixels.
        /// </summary>
        public Color[,] Values;
        /// <summary>
        /// The number of times this pattern appears in the input.
        /// </summary>
        public uint Frequency;

        public Color this[Vector2i pos]
        {
            get { return Values.Get(pos); }
            set { Values.Set(pos, value); }
        }
        

        /// <summary>
        /// Creates a pattern based on the given piece of the given input.
        /// </summary>
        /// <param name="inputMin">
        /// The position of this pattern's min corner in the input.
        /// </param>
        /// <param name="size">
        /// The size of this pattern along each axis (before transformation).
        /// </param>
        /// <param name="patternTransform">
        /// Transforms this pattern's region in the input.
        /// Note that using Rotate270CW or Rotate90CW results in the X and Y size swapping places.
        /// </param>
        public Pattern(Input input, Vector2i inputMin, Vector2i size, Transforms patternTransform)
        {
            Frequency = 1;

            //Get the size of this pattern after transformation.
            Vector2i transformedSize = size;
            switch (patternTransform)
            {
                case Transforms.MirrorX:
                case Transforms.MirrorY:
                case Transforms.Rotate180:
                case Transforms.None:
                    break;
                case Transforms.Rotate270CW:
                case Transforms.Rotate90CW:
                    transformedSize = new Vector2i(size.y, size.x);
                    break;
                default: throw new NotImplementedException(patternTransform.ToString());
            }

            //Sample the pixel values from the input.
            Values = new Color[transformedSize.x, transformedSize.y];
            foreach (Vector2i patternPos in new Vector2i.Iterator(size))
            {
                Vector2i transformedPatternPos = patternPos.Transform(patternTransform, size);
                Vector2i inputPos = inputMin + patternPos;

                Values.Set(transformedPatternPos, input[inputPos]);
            }
        }
        /// <summary>
        /// Creates a Pattern with the given pixel data.
        /// </summary>
        public Pattern(Color[,] values, uint frequency)
        {
            Values = values;
            Frequency = frequency;
        }


        /// <summary>
        /// Gets whether this pattern can fit into the given output at the given position.
        /// </summary>
        /// <param name="outputMinCorner">
        /// The position in the output of this pattern's min corner.
        /// </param>
        /// <param name="output">
        /// A way to get the current value of any output tile.
        /// A "null" result means that that tile isn't set yet, or that it's out of bounds.
        /// </param>
        public bool DoesFit(Vector2i outputMinCorner,
                            Func<Vector2i, Color?> output)
        {
            //For every tile in this pattern,
            //    see if the corresponding output tile
            //    contains something different than the corresponding input tile.
            foreach (Vector2i patternPos in Values.AllIndices())
            {
                Color patternCol = this[patternPos];

                Vector2i outputPos = patternPos + outputMinCorner;
                Color? outputCol = output(outputPos);

                if (outputCol.HasValue && outputCol.Value != patternCol)
                    return false;
            }

            return true;
        }


        public override string ToString()
        {
            return "" + Values.SizeX() + "x" + Values.SizeY() + "|" + Frequency;
        }
        public override int GetHashCode()
        {
            const int prime = 59;
            int hash = 1;
            foreach (Vector2i tilePos in Values.AllIndices())
                hash = unchecked(unchecked(hash * prime) + this[tilePos].GetHashCode());
            return hash;
        }
        public override bool Equals(object obj)
        {
            return obj is Pattern && Equals((Pattern)obj);
        }
        public bool Equals(Pattern obj)
        {
            if (Values.SizeXY() != obj.Values.SizeXY())
                return false;
            foreach (Vector2i tilePos in Values.AllIndices())
                if (this[tilePos] != obj[tilePos])
                    return false;
            return true;
        }
    }
}
