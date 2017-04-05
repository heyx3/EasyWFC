using System;
using System.Collections.Generic;
using System.Linq;

using Color = System.Windows.Media.Color;


namespace QM2D.Generator
{
    /// <summary>
    /// The input data, and all the patterns that were pulled out of it.
    /// </summary>
    public class Input
    {
        /// <summary>
        /// Whether the input data wraps around an axis.
        /// </summary>
        public bool PeriodicX, PeriodicY;

        /// <summary>
        /// The size of the patterns, without any transformations.
        /// </summary>
        public Vector2i OriginalPatternSize;
        /// <summary>
        /// The maximum-possible size of a pattern along each axis.
        /// This is different from "OriginalPatternSize"
        ///     if the patterns aren't square and rotations are allowed.
        /// </summary>
        public Vector2i MaxPatternSize;

        /// <summary>
        /// All the patterns this instance contains.
        /// </summary>
        public List<Pattern> Patterns;

        /// <summary>
        /// The average of every pixel color in this input's patterns.
        /// Used when initializing pixels in the WFC algorithm.
        /// </summary>
        public Color AverageColor;
        /// <summary>
        /// The frequency of every pixel color in this input's patterns.
        /// Used when initializing pixels in the WFC algorithm.
        /// </summary>
        public Dictionary<Color, uint> ColorFrequencies;

        private Color[,] data;
        

        public Vector2i Size { get { return data.SizeXY(); } }

        /// <summary>
        /// Gets/sets a pixel in this input.
        /// Automatically handles things like wrapping the position if this instance is periodic.
        /// </summary>
        public Color this[Vector2i pos]
        {
            get
            {
                Vector2i size = Size;
                if (PeriodicX)
                {
                    while (pos.x < 0)
                        pos.x += size.x;
                    pos.x %= size.x;
                }
                if (PeriodicY)
                {
                    while (pos.y < 0)
                        pos.y += size.y;
                    pos.y %= size.y;
                }

                return data.Get(pos);
            }
            set
            {
                Vector2i size = Size;
                if (PeriodicX)
                {
                    while (pos.x < 0)
                        pos.x += size.x;
                    pos.x %= size.x;
                }
                if (PeriodicY)
                {
                    while (pos.y < 0)
                        pos.y += size.y;
                    pos.y %= size.y;
                }

                if (!IsInside(pos))
                    return;

                data.Set(pos, value);
            }
        }


        public Input(Color[,] _data, Vector2i patternSize, bool periodicX, bool periodicY,
                     bool useRotations, bool useReflections)
        {
            PeriodicX = periodicX;
            PeriodicY = periodicY;
            OriginalPatternSize = patternSize;

            //Copy the color data.
            data = new Color[_data.GetLength(0), _data.GetLength(1)];
            foreach (Vector2i pos in data.AllIndices())
                this[pos] = _data.Get(pos);

            //Compute the average color.
            float sumR = 0.0f,
                  sumG = 0.0f,
                  sumB = 0.0f;
            foreach (Vector2i pos in data.AllIndices())
            {
                Color color = data.Get(pos);

                sumR += color.R;
                sumG += color.G;
                sumB += color.B;

            }
            float invN = 1.0f / (data.SizeX() * data.SizeY());
            AverageColor = Color.FromRgb((byte)Math.Max(0, Math.Min(255, (int)(sumR * invN))),
                                         (byte)Math.Max(0, Math.Min(255, (int)(sumG * invN))),
                                         (byte)Math.Max(0, Math.Min(255, (int)(sumB * invN))));

            //Get the list of transformations we'll be using on the input data.
            List <Transforms> transformations = new List<Transforms>() { Transforms.None };
            if (useReflections)
            {
                transformations.Add(Transforms.MirrorX);
                transformations.Add(Transforms.MirrorY);
            }
            //The use of rotations also determines our maximum pattern size along each axis.
            if (useRotations)
            {
                transformations.Add(Transforms.Rotate90CW);
                transformations.Add(Transforms.Rotate270CW);
                transformations.Add(Transforms.Rotate180);
                
                int maxPatternSize = Math.Max(OriginalPatternSize.x, OriginalPatternSize.y);
                MaxPatternSize = new Vector2i(maxPatternSize, maxPatternSize);
            }
            else
            {
                MaxPatternSize = OriginalPatternSize;
            }


            //For every pixel in the input, create one pattern starting from that pixel
            //    for each type of transformation.
            Patterns = new List<Pattern>(Size.x * Size.y * transformations.Count);
            //If the input is periodic, we can make our patterns all the way
            //    to the max edge of the input.
            //Otherwise, we have to back off the max edge a bit.
            Vector2i maxPatternMinCorner = Size - 1;
            if (!PeriodicX)
                maxPatternMinCorner.x -= patternSize.x;
            if (!PeriodicY)
                maxPatternMinCorner.y -= patternSize.y;
            foreach (Vector2i patternMinCornerPos in new Vector2i.Iterator(maxPatternMinCorner + 1))
                foreach (var transform in transformations)
                    Patterns.Add(new Pattern(this, patternMinCornerPos, patternSize, transform));

            //Remove identical patterns, using hashes to compare them quickly.
            List<int> patternHashes = Patterns.Select(p => p.GetHashCode()).ToList();
            for (int i = 0; i < Patterns.Count; ++i)
            {
                var basePattern = Patterns[i];
                for (int j = i + 1; j < Patterns.Count; ++j)
                {
                    var patternToCheck = Patterns[j];

                    //If the patterns match, remove the second one.
                    if (patternHashes[i] == patternHashes[j] && basePattern.Equals(patternToCheck))
                    {
                        //Increment the frequency of the original pattern.
                        Patterns[i] = new Pattern(Patterns[i].Values, Patterns[i].Frequency + 1);

                        Patterns.RemoveAt(j);
                        patternHashes.RemoveAt(j);
                        j -= 1;
                    }
                }
            }

            //Compute the color frequencies.
            ColorFrequencies = new Dictionary<Color, uint>();
            foreach (Pattern pattern in Patterns)
                foreach (Vector2i patternPos in pattern.Values.AllIndices())
                {
                    Color patternColor = pattern.Values.Get(patternPos);
                    if (!ColorFrequencies.ContainsKey(patternColor))
                        ColorFrequencies.Add(patternColor, 0);
                    ColorFrequencies[patternColor] += pattern.Frequency;
                }
        }
        

        public bool IsInside(Vector2i inputPos)
        {
            return inputPos.x >= 0 && inputPos.y >= 0 &&
                   inputPos.x < Size.x && inputPos.y < Size.y;
        }

        public override string ToString()
        {
            return "[" + Size.x + "x" + Size.y + ", " +
                   (PeriodicX ? "periodicX" : "not periodicX") + ", " +
                   (PeriodicY ? "periodicY" : "not periodicY") + ", " +
                   OriginalPatternSize.x + "x" + OriginalPatternSize.y + " patterns]";
        }
    }
}
