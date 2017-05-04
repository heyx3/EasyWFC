using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Vector2i = QM2D.Generator.Vector2i;

namespace QM2D
{
    /// <summary>
    /// Interaction logic for GeneratorWindow.xaml
    /// </summary>
    public partial class GeneratorWindow : Window
    {
		public static int HashSeed(string seed)
		{
			int hash;
			if (!int.TryParse(seed, out hash))
				hash = seed.GetHashCode();
			return hash;
		}


        private Generator.State state;
        private BitmapSource outputImg;

        private bool visualizeUnsetPixels = true;
        private int stepIncrement;


        public GeneratorWindow()
        {
            InitializeComponent();
            
            Label_Failed.Content = "";
        }


        public void Reset(int outputWidth, int outputHeight, string seed,
                          BitmapImage input, int patternSizeX, int patternSizeY,
                          bool periodicInputX, bool periodicInputY,
                          bool inputPatternRotations, bool inputPatternReflections)
        {
            outputWidth = Math.Max(outputWidth, 9);
            outputHeight = Math.Max(outputHeight, 9);

            Img_Input.Source = input;
            RenderOptions.SetBitmapScalingMode(Img_Input, BitmapScalingMode.NearestNeighbor);

            //Set up the input.

            Color[,] inputPixelGrid = new Color[input.PixelWidth, input.PixelHeight];
            Utilities.Convert(input, ref inputPixelGrid);
            
            state = new Generator.State(new Generator.Input(inputPixelGrid,
                                                            new Vector2i(patternSizeX, patternSizeY),
                                                            periodicInputX, periodicInputY,
                                                            inputPatternRotations,
                                                            inputPatternReflections),
                                        new Vector2i(outputWidth, outputHeight),
                                        Check_PeriodicOutputX.IsChecked.Value,
                                        Check_PeriodicOutputY.IsChecked.Value,
                                        HashSeed(seed));
            Textbox_ViolationClearSize.Text = state.ViolationClearSize.ToString();
            Textbox_OutputWidth.Text = outputWidth.ToString();
            Textbox_OutputHeight.Text = outputHeight.ToString();
            Readonly_Seed.Content = seed;

            UpdateOutputTex();
        }

        private void UpdateOutputTex()
        {
            Color[,] cols = new Color[state.Output.SizeX(), state.Output.SizeY()];
            foreach (Vector2i pixelPos in cols.AllIndices())
                cols.Set(pixelPos, state.Output.Get(pixelPos).VisualizedValue);

            outputImg = Utilities.Convert(cols);
            if (outputImg is BitmapImage)
                ((BitmapImage)outputImg).CacheOption = BitmapCacheOption.OnLoad;

            Img_Output.Source = outputImg;
            RenderOptions.SetBitmapScalingMode(Img_Output, BitmapScalingMode.NearestNeighbor);
        }


        private void Button_Step_Click(object sender, RoutedEventArgs e)
        {
            HashSet<Vector2i> failPoses = null;
            state.UpdateVisualizationAfterIteration = visualizeUnsetPixels;
            for (int i = 0; i < stepIncrement; ++i)
            {
                var status = state.Iterate(ref failPoses);

                if (status.HasValue)
                {
                    Button_Step.IsEnabled = false;

                    if (!status.Value)
                    {
                        Vector2i failPos = failPoses.First();
                        Label_Failed.Content = "Failed at: " + failPos.x + "," + failPos.y;
                    }

                    break;
                }
                else if (false)
                {
                    //See if the constraints got violated somehow.
                    //If they did, make this obvious by modifying the color of positions that violate it.
                    var outputPixelGetter = state.OutputPixelGetter;
                    var outputColorGetter = state.OutputColorGetter;
                    HashSet<Vector2i> toClear = new HashSet<Vector2i>();
                    foreach (Vector2i pos in new Vector2i.Iterator(-state.Input.MaxPatternSize,
                                                                   state.Output.SizeXY()))
                    {
                        Vector2i v = pos;
                        if (!state.Input.Patterns.Any(patt => patt.DoesFit(pos, outputColorGetter)))
                            toClear.Add(pos);
                    }
                    foreach (Vector2i posToClear in toClear)
                    {
                        var pixel = outputPixelGetter(posToClear);
                        if (pixel != null)
                        {
                            Color? oldVal = pixel.FinalValue;
                            Color newVal = (oldVal.HasValue ?
                                                Color.FromRgb(oldVal.Value.R, 100, oldVal.Value.B) :
                                                Color.FromRgb(0, 255, 0));
                            pixel.FinalValue = newVal;
                            pixel.VisualizedValue = newVal;
                        }
                    }
                }
            }

            UpdateOutputTex();
        }
        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            Label_Failed.Content = "";
            state.Reset(null, HashSeed(Readonly_Seed.Content.ToString()));
            UpdateOutputTex();
            
            Button_Step.IsEnabled = true;
        }

        private void Button_SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.DefaultExt = "png";
            saveFileDialog.FileName = System.IO.Path.Combine(Environment.CurrentDirectory,
                                                             "Output.png");
            saveFileDialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp";
            saveFileDialog.FilterIndex = 0;

            var result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                string err = Utilities.ToFile((BitmapSource)Img_Output.Source,
                                              saveFileDialog.FileName);
                if (err != null)
                {
                    MessageBox.Show(err, "Error saving output tex");
                }
            }
        }
        
        private void Textbox_OutputWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (state == null)
                return;

            int i;
            if (int.TryParse(Textbox_OutputWidth.Text, out i) && i > 7)
            {
                state.Reset(new Vector2i(i, state.Output.SizeY()),
                            HashSeed(Readonly_Seed.Content.ToString()));
                UpdateOutputTex();
            }
        }
        private void Textbox_OutputHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (state == null)
                return;

            int i;
            if (int.TryParse(Textbox_OutputHeight.Text, out i) && i > 7)
            {
                state.Reset(new Vector2i(state.Output.SizeX(), i),
                            HashSeed(Readonly_Seed.Content.ToString()));
                UpdateOutputTex();
            }
        }
        
        private void Textbox_ViolationClearSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (state == null)
                return;

            int i;
            if (int.TryParse(Textbox_ViolationClearSize.Text, out i))
                state.ViolationClearSize = i;
        }

        private void Check_VisualizePixels_Checked(object sender, RoutedEventArgs e)
        {
            visualizeUnsetPixels = Check_VisualizePixels.IsChecked.Value;
        }

        private void Check_PeriodicOutputX_Checked(object sender, RoutedEventArgs e)
        {
            state.PeriodicX = Check_PeriodicOutputX.IsChecked.Value;
        }
        private void Check_PeriodicOutputY_Checked(object sender, RoutedEventArgs e)
        {
            state.PeriodicY = Check_PeriodicOutputY.IsChecked.Value;
        }

        private void Textbox_StepIncrement_TextChanged(object sender, TextChangedEventArgs e)
        {
            int i;
            if (int.TryParse(Textbox_StepIncrement.Text, out i))
                stepIncrement = i;
        }
    }
}
