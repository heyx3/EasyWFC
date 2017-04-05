using System.Collections.Generic;

using Color = System.Windows.Media.Color;


namespace QM2D.Generator
{
    /// <summary>
    /// An atomic piece of the output image.
    /// </summary>
    public class OutputPixel
    {
        /// <summary>
        /// The final chosen value for this pixel.
        /// Set to "null" if this pixel isn't set yet.
        /// </summary>
        public Color? FinalValue;
        /// <summary>
        /// A weighted blend of the most likely colors for this pixel.
        /// </summary>
        public Color VisualizedValue;
        
        /// <summary>
        /// For every color that this pixel could have (based on nearby pixels and the input patterns),
        ///     this dictionary provides the number of ways this pixel could be given that color.
        /// </summary>
        public Dictionary<Color, uint> ApplicableColorFrequencies;


        public OutputPixel()
        {
            FinalValue = null;
            VisualizedValue = Color.FromRgb(255, 0, 255);
            ApplicableColorFrequencies = new Dictionary<Color, uint>();
        }
    }
}
