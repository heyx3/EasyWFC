using System;
using System.Collections.Generic;
using System.Linq;

using Color = System.Windows.Media.Color;


namespace QM2D.Generator
{
    /// <summary>
    /// The WFC algorithm at a certain state.
    /// </summary>
    public class State
    {
        public Input Input;
        public OutputPixel[,] Output;

        public Random Rng;

        /// <summary>
        /// Whether this output should wrap around an axis.
        /// </summary>
        public bool PeriodicX, PeriodicY;

        /// <summary>
        /// If true, then after every iteration,
        ///     the visualization color for any affected pixels will be re-computed.
        /// </summary>
        public bool UpdateVisualizationAfterIteration;
        /// <summary>
        /// If a constraint violation is found, an area surrounding that violation
        ///     will be cleared out and regenerated.
        /// The size of that area is [pattern size] * ViolationClearSize.
        /// If this is set to anything less than 1,
        ///     this generator just fails instead of clearing out the violation.
        /// </summary>
        public int ViolationClearSize;

        /// <summary>
        /// A function to map a given output position to account for wrapping along each axis.
        /// Note that if at least one axis isn't periodic, the mapped value may not be inside the output.
        /// </summary>
        public Func<Vector2i, Vector2i> OutputPosFilter
        {
            get
            {
                if (PeriodicX)
                    if (PeriodicY)
                        return (outputPos) => new Vector2i(outputPos.x.Wrap(Output.SizeX()), outputPos.y.Wrap(Output.SizeY()));
                    else
                        return (outputPos) => new Vector2i(outputPos.x.Wrap(Output.SizeX()), outputPos.y);
                else
                    if (PeriodicY)
                    return (outputPos) => new Vector2i(outputPos.x, outputPos.y.Wrap(Output.SizeY()));
                else
                    return (outputPos) => outputPos;
            }
        }
        /// <summary>
        /// A function to map a given output position to its pixel, wrapping along all periodic axes.
        /// The function returns "null" if the given position isn't actually in the output.
        /// </summary>
        public Func<Vector2i, OutputPixel> OutputPixelGetter
        {
            get
            {
                var posFilter = OutputPosFilter;
                return (outputPos) =>
                {
                    Vector2i filteredPos = posFilter(outputPos);
                    if (Output.IsInRange(filteredPos))
                        return Output.Get(filteredPos);
                    else
                        return null;
                };
            }
        }
        /// <summary>
        /// A function to map a given output position to its pixel color,
        ///     wrapping along all periodic axes.
        /// If the given position isn't in the output,
        ///     or the output pixel at that position isn't set yet,
        ///     the function returns "null".
        /// </summary>
        public Func<Vector2i, Color?> OutputColorGetter
        {
            get
            {
                var pixelGetter = OutputPixelGetter;
                return (outputPos) =>
                {
                    var pixel = pixelGetter(outputPos);
                    if (pixel == null)
                        return null;
                    else
                        return pixel.FinalValue;
                };
            }
        }


        public State(Input input, Vector2i outputSize, bool periodicX, bool periodicY, int seed)
        {
            Input = input;

            UpdateVisualizationAfterIteration = true;
            ViolationClearSize = 3;

            PeriodicX = periodicX;
            PeriodicY = periodicY;

            Reset(outputSize, seed);
        }


        /// <summary>
        /// Resets this generator's output state.
        /// </summary>
        /// <param name="newSize">If not null, the output will be resized to the given size.</param>
        public void Reset(Vector2i? newSize, int? seed)
        {
            if (seed.HasValue)
                Rng = new Random(seed.Value);

            //If necessary, re-allocate output data.
            if (newSize.HasValue)
            {
                Output = new OutputPixel[newSize.Value.x, newSize.Value.y];
                foreach (Vector2i pos in Output.AllIndices())
                    Output.Set(pos, new OutputPixel());
            }

            //Reset output data.
            foreach (var pixel in Output)
            {
                pixel.FinalValue = null;
                pixel.VisualizedValue = Input.AverageColor;
                pixel.ApplicableColorFrequencies.Clear();
                foreach (KeyValuePair<Color, uint> kvp in Input.ColorFrequencies)
                    pixel.ApplicableColorFrequencies.Add(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Runs one iteration.
        /// Returns the following values:
        ///     "true": Finished
        ///     "false": Failed
        ///     "null": Not done yet
        /// </summary>
        /// <param name="output_FailedAt">
        /// If the algorithm failed, this collection is filled with
        ///     the positions of the pixels that it failed at.
        /// </param>
        public bool? Iterate(ref HashSet<Vector2i> output_FailedAt)
        {
            var outputColorGetter = OutputColorGetter;

            var lowestEntropyPixelPoses = GetBestPixels();

            //If all pixels are set in stone, we're done.
            if (lowestEntropyPixelPoses.Count == 0)
                return true;

            //If any pixels are impossible to solve, handle it.
            int nColorChoices =
                    Output.Get(lowestEntropyPixelPoses.First()).ApplicableColorFrequencies.Count;
            if (nColorChoices == 0)
            {
                //Either clear out the violating pixels, or give up.
                if (ViolationClearSize > 0)
                {
                    foreach (Vector2i brokenOutputPos in lowestEntropyPixelPoses)
                        ClearArea(brokenOutputPos);
                    return null;
                }
                else
                {
                    output_FailedAt = lowestEntropyPixelPoses;
                    return false;
                }
            }

            //Pick one of these pixels at random to fill in.
            int pixelIndex = Rng.Next(lowestEntropyPixelPoses.Count);
            var chosenPixelPos = lowestEntropyPixelPoses.Skip(pixelIndex).First();
            var chosenPixel = Output.Get(chosenPixelPos);

            //Randomly choose a color for it.
            Color chosenColor = Color.FromRgb(255, 0, 255);
            if (chosenPixel.ApplicableColorFrequencies.Count == 1)
            {
                chosenColor = chosenPixel.ApplicableColorFrequencies.First().Key;
            }
            else
            {
                //Weighted random choice.

                var colorsAndFrequenciesList = chosenPixel.ApplicableColorFrequencies.ToList();
                int nTotalColors = colorsAndFrequenciesList.Sum(c_f => (int)c_f.Value);
                int chosenIndex = Rng.Next(nTotalColors);

                int totalCount = 0,
                    colorCount = 0,
                    colorIndex = 0;
                while (totalCount < chosenIndex)
                {
                    colorCount += 1;
                    totalCount += 1;

                    while (colorCount >= colorsAndFrequenciesList[colorIndex].Value)
                    {
                        colorCount -= (int)colorsAndFrequenciesList[colorIndex].Value;
                        colorIndex += 1;
                    }
                }
                chosenColor = colorsAndFrequenciesList[colorIndex].Key;
            }
            SetPixel(chosenPixelPos, chosenColor);

            return null;
        }


        //The below are all helper methods for "Iterate()".

        /// <summary>
        /// Gets all output pixels with the fewest number of possible colors.
        /// Ignores any pixels whose color is already set in stone.
        /// </summary>
        public HashSet<Vector2i> GetBestPixels()
        {
            HashSet<Vector2i> result = new HashSet<Vector2i>();
            uint minEntropy = uint.MaxValue;

			foreach (Vector2i outputPos in Output.AllIndices())
			{
				var pixel = Output.Get(outputPos);
				if (!pixel.FinalValue.HasValue)
				{
					uint pixelEntropy = 0;
					foreach (uint optionFrequency in pixel.ApplicableColorFrequencies.Values)
						pixelEntropy += optionFrequency;

					if (pixelEntropy < minEntropy)
					{
						minEntropy = pixelEntropy;
						result.Clear();
						result.Add(outputPos);
					}
					else if (minEntropy == pixelEntropy)
					{
						result.Add(outputPos);
					}
				}
			}

            return result;
        }
        /// <summary>
        /// Sets the given pixel to have the given color.
        /// Updates the status of neighboring pixels to take this into account.
        /// </summary>
        public void SetPixel(Vector2i pixelPos, Color value)
        {
            var newPixel = Output.Get(pixelPos);
            newPixel.FinalValue = value;
            newPixel.VisualizedValue = value;

            var outputPosFilter = OutputPosFilter;
            var outputColorGetter = OutputColorGetter;

            //Any pixel that can share a pattern with the changed pixel is affected.
            Vector2i minAffectedPixelPos = pixelPos - Input.MaxPatternSize,
                     maxAffectedPixelPos = pixelPos + Input.MaxPatternSize;
            RecalculatePixelChances(minAffectedPixelPos, maxAffectedPixelPos + 1);
        }
        /// <summary>
        /// For every output pixel in the given region that isn't set yet,
        ///     this method recalculates that pixel's
        ///     "ApplicableColorFrequencies" and "VisualizedColor" fields.
        /// </summary>
        public void RecalculatePixelChances(Vector2i startAreaInclusive, Vector2i endAreaExclusive)
        {
            var outputPosFilter = OutputPosFilter;
            var outputColorGetter = OutputColorGetter;
			
            foreach (Vector2i _pixelPos in new Vector2i.Iterator(startAreaInclusive,
                                                                 endAreaExclusive))
            {
				Vector2i pixelPos = outputPosFilter(_pixelPos);

				//If the position is outside the output, or the pixel is already set, don't bother.
				if (!Output.IsInRange(pixelPos) || Output.Get(pixelPos).FinalValue.HasValue)
					continue;

				var pixel = Output.Get(pixelPos);

				//Find any colors that, if placed here, would cause a violation of the WFC constraint.
				HashSet<Color> badColors = new HashSet<Color>();
				foreach (Color color in Input.ColorFrequencies.Keys)
				{
					//Assume that the color is placed.
					Func<Vector2i, Color?> newOutputColorGetter = (outputPos) =>
					{
						Vector2i filteredOutputPos = outputPosFilter(outputPos);
						if (filteredOutputPos == pixelPos)
							return color;
						else if (Output.IsInRange(filteredOutputPos))
							return Output.Get(filteredOutputPos).FinalValue;
						else
							return null;
					};

					//Test the constraint: any NxM pattern in the output
					//    appears at least once in the input.
					Vector2i minNearbyPixelPos = pixelPos - Input.MaxPatternSize + 1,
							 maxNearbyPixelPos = pixelPos;
					foreach (Vector2i nearbyAffectedPixelPos in new Vector2i.Iterator(minNearbyPixelPos,
																					  maxNearbyPixelPos + 1))
					{
						if (!Input.Patterns.Any(pattern => pattern.DoesFit(nearbyAffectedPixelPos,
																			newOutputColorGetter)))
						{
							badColors.Add(color);
							break;
						}
					}
				}

				//Check which patterns can be applied at which positions around this pixel.
				pixel.ApplicableColorFrequencies.Clear();
				foreach (Pattern pattern in Input.Patterns)
				{
					Vector2i patternSize = pattern.Values.SizeXY();
					var patternPoses = new Vector2i.Iterator(pixelPos - patternSize + 1,
															 pixelPos + 1);
					foreach (Vector2i patternMinCornerPos in patternPoses)
					{
						//See if the pattern can be placed here
						//    (and the color this pixel would become isn't blacklisted).

						Vector2i pixelPatternPos = pixelPos - patternMinCornerPos;
						Color pixelPatternColor = pattern[pixelPatternPos];

						if (!badColors.Contains(pixelPatternColor) &&
							pattern.DoesFit(patternMinCornerPos, outputColorGetter))
						{
							if (!pixel.ApplicableColorFrequencies.ContainsKey(pixelPatternColor))
								pixel.ApplicableColorFrequencies.Add(pixelPatternColor, 0);
							pixel.ApplicableColorFrequencies[pixelPatternColor] += pattern.Frequency;
						}
					}
				}

				//Now update the visualized color of that pixel.
				//To do that, average together every color the pixel COULD be.
				if (!UpdateVisualizationAfterIteration)
					continue;
				float sumR = 0.0f,
					  sumG = 0.0f,
					  sumB = 0.0f;
				foreach (var colorAndFrequency in pixel.ApplicableColorFrequencies)
				{
					sumR += colorAndFrequency.Key.R * colorAndFrequency.Value;
					sumG += colorAndFrequency.Key.G * colorAndFrequency.Value;
					sumB += colorAndFrequency.Key.B * colorAndFrequency.Value;
				}
				float invN = 1.0f / pixel.ApplicableColorFrequencies.Values.Sum(u => (float)u);
				pixel.VisualizedValue = Color.FromRgb((byte)Math.Max(0, Math.Min(255, (int)(sumR * invN))),
													  (byte)Math.Max(0, Math.Min(255, (int)(sumG * invN))),
													  (byte)Math.Max(0, Math.Min(255, (int)(sumB * invN))));
            }
        }
        /// <summary>
        /// Clears the output tiles surrounding the given position.
        /// The size of the region to clear is determined by ViolationClearSize.
        /// Assumes that ViolationClearSize is greater than 0.
        /// </summary>
        public void ClearArea(Vector2i center)
        {
            Vector2i clearSize = Input.MaxPatternSize * ViolationClearSize,
                     halfClearSize = clearSize / 2;
            Vector2i clearCenter = center + (Input.MaxPatternSize / 2);

            var outputGetter = OutputPixelGetter;
            Vector2i minPos = clearCenter - halfClearSize,
                     maxPos = clearCenter + halfClearSize;
            foreach (Vector2i posToClear in new Vector2i.Iterator(minPos, maxPos + 1))
            {
                var pixel = outputGetter(posToClear);
                if (pixel != null)
                {
                    pixel.FinalValue = null;
                    pixel.VisualizedValue = Input.AverageColor;
                }
            }

            //Recalculate color chances for any nearby unset pixels.
            RecalculatePixelChances(minPos - Input.MaxPatternSize + 1,
                                    maxPos + Input.MaxPatternSize - 1);
        }
    }
}