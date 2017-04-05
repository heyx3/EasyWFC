using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace QM2D
{
    public class AppSettings
    {
        public int TileSizeX = 3,
                   TileSizeY = 3;
        public bool PeriodicInputX = true,
                    PeriodicInputY = true,
                    MirrorInput = true,
                    RotateInput = true;

        public string InputFilePath = "";

        public string Seed = "abc123";


        public AppSettings() { }
        public AppSettings(string settingsFileName)
        {
            string[] fileLines;
            {
                string filePath = Path.Combine(Environment.CurrentDirectory, settingsFileName);
                if (File.Exists(filePath))
                    fileLines = File.ReadAllLines(filePath);
                else
                    fileLines = new string[0];
            }

            for (int i = 0; i < fileLines.Length; ++i)
            {
                string line = fileLines[i].Trim();

                if (line.Length == 0)
                    continue;
                
                string[] keyAndVal = line.Split('=');
                if (keyAndVal.Length != 2)
                    throw new FileFormatException("The following line is malformed: " + line);
                switch (keyAndVal[0])
                {
                    case "tileSizeX": TileSizeX = int.Parse(keyAndVal[1]); break;
                    case "tileSizeY": TileSizeY = int.Parse(keyAndVal[1]); break;
                    case "inputFile": InputFilePath = keyAndVal[1]; break;
                    case "seed": Seed = keyAndVal[1]; break;
                    case "periodicInputX": PeriodicInputX = bool.Parse(keyAndVal[1]); break;
                    case "periodicInputY": PeriodicInputY = bool.Parse(keyAndVal[1]); break;
                    case "mirrorInput": MirrorInput = bool.Parse(keyAndVal[1]); break;
                    case "rotateInput": RotateInput = bool.Parse(keyAndVal[1]); break;

                    default: throw new FileFormatException("Unexpected key \"" + keyAndVal[0] + "\"");
                }
            }
        }


        public void SaveTo(string settingsFileName)
        {
            StringBuilder contents = new StringBuilder();

            contents.Append("tileSizeX=");
            contents.AppendLine(TileSizeX.ToString());

            contents.Append("tileSizeY=");
            contents.AppendLine(TileSizeY.ToString());

            contents.Append("inputFile=");
            contents.AppendLine(InputFilePath);

            contents.Append("seed=");
            contents.AppendLine(Seed);

            contents.Append("periodicInputX=");
            contents.AppendLine(PeriodicInputX.ToString());

            contents.Append("periodicInputY=");
            contents.AppendLine(PeriodicInputY.ToString());

            contents.Append("mirrorInput=");
            contents.AppendLine(MirrorInput.ToString());

            contents.Append("rotateInput=");
            contents.AppendLine(RotateInput.ToString());

            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, settingsFileName),
                              contents.ToString());
        }
    }
}
