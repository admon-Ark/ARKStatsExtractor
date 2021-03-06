﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;


namespace ARKBreedingStats
{
    public class ArkOCR
    {
        int whiteThreshold = 230;
        public Bitmap[] alphabet = new Bitmap[255];
        public Bitmap[] reducedAlphabet = new Bitmap[255];
        public Dictionary<Int64, List<byte>> hashMap = new Dictionary<long, List<byte>>();
        public Dictionary<String, Point> statPositions = new Dictionary<string, Point>();
        private static ArkOCR _OCR;
        public static FlowLayoutPanel debugPanel { get; set; }
        private int[] calibrationResolution = new int[] { 0, 0 };
        public Dictionary<String, Point> lastLetterositions = new Dictionary<string, Point>();
        private bool coordsAfterDot = false;
        public Process ARKProcess;

        public static ArkOCR OCR
        {
            get
            {
                if (_OCR == null)
                {
                    _OCR = new ArkOCR();
                }
                return _OCR;
            }
        }

        public bool calibrate(Bitmap screenshot)
        {
            Process[] p = Process.GetProcessesByName("ShooterGame");
            if (p.Length > 0)
                ARKProcess = p[0];

            if (screenshot == null)
                return false;

            if (screenshot.Width == calibrationResolution[0] && screenshot.Height == calibrationResolution[1])
                return true;

            //debugPanel.Controls.Clear();
            alphabet = new Bitmap[255];
            hashMap = new Dictionary<long, List<byte>>();
            statPositions = new Dictionary<string, Point>(9);

            Bitmap bmp;
            Bitmap origBitmap = Properties.Resources.ARKCalibration1080;


            //CalibrateFromImage(bmp, @"1234567890?,.;/:+=@|#%abcdeghijklm " + @"n opqrstuvwxyz&é'(§è!çà)-ABCDEFGHIJLMNOPQRSTUVWXYZ£µ$[]{}ñ<>/\f lK");

            //GenerateLetterImagesFromFont(18); // <-- function should have generated "clean" image from source font, but ARK scales down from original values and adds a glow, leading to greater inaccuracies

            // positions depend on screen resolution.
            int resolution = 0;
            Win32Stuff.Rect res = new Win32Stuff.Rect(); // Win32Stuff.GetWindowRect("ShooterGame");
            if (screenshot != null)
            {
                res.left = 0;
                res.right = screenshot.Width;
                res.top = 0;
                res.bottom = screenshot.Height;
            }
            else
                return false; // error

            if (res.Width == 1920 && res.Height == 1080)
                resolution = 0;
            else if (res.Width == 1680 && res.Height == 1050)
                resolution = 1;
            else if (res.Width == 1600 && res.Height == 900)
                resolution = 2;
            else
            {
                resolution = -1;
                return false; // no supported resolution
            }


            switch (resolution)
            {
                case 0:
                    // coords for 1920x1080
                    statPositions["NameAndLevel"] = new Point(1280, 170);
                    statPositions["Health"] = new Point(1355, 630);
                    statPositions["Stamina"] = new Point(1355, 665);
                    statPositions["Oxygen"] = new Point(1355, 705);
                    statPositions["Food"] = new Point(1355, 740);
                    statPositions["Weight"] = new Point(1355, 810);
                    statPositions["Melee Damage"] = new Point(1355, 845);
                    statPositions["Movement Speed"] = new Point(1355, 885);
                    statPositions["Torpor"] = new Point(1355, 990);

                    origBitmap = Properties.Resources.ARKCalibration1080;
                    bmp = removePixelsUnderThreshold(GetGreyScale(origBitmap), whiteThreshold);
                    CalibrateFromImage(bmp, @"1234567890?,.;/:+=@|#%abcdeghijklm " + @"n opqrstuvwxyz&é'(§è!çà)-ABCDEFGHIJLMNOPQRSTUVWXYZ£µ$[]{}ñ<>/\f lK");
                    break;

                case 1:
                    // coords for 1680x1050
                    // 1680/1920 = height-factor; 50 = translation
                    // not yet correct x_1080 |--> (x_1080+60)*1680/1920
                    //statPositions["NameAndLevel"] = new Point(1111, 200);
                    //statPositions["Health"] = new Point(1183, 595);
                    //statPositions["Stamina"] = new Point(1183, 630);
                    //statPositions["Oxygen"] = new Point(1183, 665);
                    //statPositions["Food"] = new Point(1183, 691);
                    //statPositions["Weight"] = new Point(1183, 755);
                    //statPositions["Melee Damage"] = new Point(1183, 788);
                    //statPositions["Movement Speed"] = new Point(1183, 817);
                    //statPositions["Torpor"] = new Point(1183, 912);

                    // version without the "statName:"
                    coordsAfterDot = true;
                    statPositions["NameAndLevel"] = new Point(1111, 200);
                    statPositions["Health"] = new Point(1260, 595);
                    statPositions["Stamina"] = new Point(1277, 630);
                    statPositions["Oxygen"] = new Point(1271, 665);
                    statPositions["Food"] = new Point(1249, 691);
                    statPositions["Weight"] = new Point(1264, 755);
                    statPositions["Melee Damage"] = new Point(1340, 788);
                    statPositions["Movement Speed"] = new Point(1362, 817);
                    statPositions["Torpor"] = new Point(1260, 912);


                    origBitmap = Properties.Resources.ARKCalibration1050;
                    bmp = removePixelsUnderThreshold(GetGreyScale(origBitmap), whiteThreshold);
                    CalibrateFromImage(bmp, @"1234567890.,?;.:/=+ù%µ$* ABCDEFGHIJ-LMNOPQRSTUVWXYZabcdeghijklmnopqrstuvwxyz&#'()[]{}!@flK"); // £ missing
                    break;

                case 2:
                    // coords for 1600x960
                    statPositions["NameAndLevel"] = new Point(1130, 170);
                    statPositions["Health"] = new Point(1130, 550);
                    statPositions["Stamina"] = new Point(1130, 585);
                    statPositions["Oxygen"] = new Point(1130, 615);
                    statPositions["Food"] = new Point(1130, 645);
                    statPositions["Weight"] = new Point(1130, 705);
                    statPositions["Melee Damage"] = new Point(1130, 735);
                    statPositions["Movement Speed"] = new Point(1130, 765);
                    statPositions["Torpor"] = new Point(1130, 855);
                    break;
            }
            /*
            for (int i = 0; i < 255; i++)
            {
                bmp = alphabet[i];
                if (bmp == null)
                    continue;
                AddBitmapToDebug(bmp);
            }
             */
            calibrationResolution[0] = res.Width;
            calibrationResolution[1] = res.Height;
            /*
            AddBitmapToDebug(alphabet['µ']);
            AddBitmapToDebug(alphabet['%']);
            AddBitmapToDebug(alphabet['$']);
            AddBitmapToDebug(alphabet['A']);
            AddBitmapToDebug(alphabet['J']);
            AddBitmapToDebug(alphabet['µ']);
            AddBitmapToDebug(alphabet['?']);
            AddBitmapToDebug(alphabet['-']);
            AddBitmapToDebug(alphabet['[']);
            AddBitmapToDebug(alphabet['z']);
            AddBitmapToDebug(alphabet['(']);
            AddBitmapToDebug(alphabet[')']);
            AddBitmapToDebug(alphabet['f']);
            AddBitmapToDebug(alphabet['K']);

            foreach (char a in ":0123456789.%/")
                reducedAlphabet[a] = alphabet[a];
            */
            return true;
        }

        private PictureBox AddBitmapToDebug(Bitmap bmp)
        {
            if (debugPanel != null)
            {
                PictureBox b = new PictureBox();
                b.SizeMode = PictureBoxSizeMode.AutoSize;
                b.Image = bmp;
                debugPanel.Controls.Add(b);
                debugPanel.Controls.SetChildIndex(b, 0);
                return b;
            }
            else
                return null;
        }

        public Bitmap SubImage(Bitmap source, int x, int y, int width, int height)
        {
            Rectangle cropRect = new Rectangle(x, y, width, height);
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(source, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            return target;
        }

        public Bitmap GetGreyScale(Bitmap source)
        {
            Bitmap dest = (Bitmap)source.Clone();

            PixelFormat pxf = PixelFormat.Format24bppRgb;

            Rectangle rect = new Rectangle(0, 0, dest.Width, dest.Height);
            BitmapData bmpData = dest.LockBits(rect, ImageLockMode.ReadWrite, pxf);

            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap. 
            int numBytes = bmpData.Stride * dest.Height;
            byte[] rgbValues = new byte[numBytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, numBytes);

            int extraBytes = bmpData.Stride % 3;
            // convert to greyscale
            for (int i = 0; i < rect.Width; i++)
            {
                for (int j = 0; j < rect.Height; j++)
                {
                    int idx = j * bmpData.Stride + i * 3;
                    int sum = rgbValues[idx] + rgbValues[idx + 1] + rgbValues[idx + 2];

                    byte grey = (byte)(sum / 3);
                    rgbValues[idx] = grey;
                    rgbValues[idx + 1] = grey;
                    rgbValues[idx + 2] = grey;

                }
            }

            // Copy the RGB values back to the bitmap
            Marshal.Copy(rgbValues, 0, ptr, numBytes);

            dest.UnlockBits(bmpData);

            return dest;
        }

        public Bitmap removePixelsUnderThreshold(Bitmap source, int threshold)
        {
            Bitmap dest = (Bitmap)source.Clone();

            PixelFormat pxf = PixelFormat.Format24bppRgb;

            Rectangle rect = new Rectangle(0, 0, dest.Width, dest.Height);
            BitmapData bmpData = dest.LockBits(rect, ImageLockMode.ReadWrite, pxf);

            IntPtr ptr = bmpData.Scan0;

            int numBytes = bmpData.Stride * dest.Height;
            byte[] rgbValues = new byte[numBytes];

            Marshal.Copy(ptr, rgbValues, 0, numBytes);

            for (int counter = 0; counter < rgbValues.Length; counter++)
            {
                if (rgbValues[counter] < threshold)
                    rgbValues[counter] = 0;
                else
                    rgbValues[counter] = 255; // maximize the white
            }

            Marshal.Copy(rgbValues, 0, ptr, numBytes);

            dest.UnlockBits(bmpData);

            return dest;
        }

        // function currently unused. ARK seems to be scaling down larger fonts rather than using entire pixel heights
        private void GenerateLetterImagesFromFont(Int16 pixelSize)
        {

            return;

            string fontName = "Sansation-Bold.ttf"; // font used by ARK

            //ARKBreedingStats.Properties.Resources.Sansation_Bold;
            fontName = System.Reflection.Assembly.GetExecutingAssembly().Location + @"\..\..\Resources\Sansation-Bold.ttf";

            PrivateFontCollection pfcoll = new PrivateFontCollection();
            //put a font file under a Fonts directory within your application root
            pfcoll.AddFontFile(fontName);
            FontFamily ff = pfcoll.Families[0];
            string firstText = "Health: 2660.1 / 2660.1";

            PointF firstLocation = new PointF(10f, 10f);
            PointF secondLocation = new PointF(10f, 50f);
            //put an image file under a Images directory within your application root
            Bitmap bitmap = new Bitmap(300, 200);//load the image file

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                using (Font f = new Font(ff, pixelSize, FontStyle.Regular))
                {
                    graphics.DrawString(firstText, f, Brushes.Red, firstLocation);
                }
            }
            //save the new image file within Images directory
            bitmap.Save(@"D:\Temp\test.png");
        }


        public void CalibrateFromImage(Bitmap source, String textInImage)
        {
            int posXInImage = 0;

            // iterate for each letter in the text image
            for (int i = 0; i < textInImage.Length && posXInImage < source.Width; i++)
            {
                char letter = textInImage[i];
                if (letter == ' ')
                    continue;


                bool foundLetter = false;
                int letterStart = 0;
                int letterEnd = 0;

                // look for the start pixel of the letter
                while (!(foundLetter == true || posXInImage >= source.Width))
                {
                    foundLetter = HasWhiteInVerticalLine(source, posXInImage);
                    posXInImage++;
                }

                if (foundLetter)
                {
                    letterStart = posXInImage - 1;
                    // look for the end of the letter
                    do
                    {
                        posXInImage++;
                    } while (HasWhiteInVerticalLine(source, posXInImage) && posXInImage < source.Width);
                    letterEnd = posXInImage;
                }

                // store the image in the alphabet
                if (alphabet[letter] == null && posXInImage != source.Width)
                    StoreImageInAlphabet(letter, source, letterStart, letterEnd);
            }

        }

        public int lastLetterPosition(Bitmap source)
        {
            for (int i = source.Width; i > 0; i--)
            {
                if (HasWhiteInVerticalLine(source, i))
                    return i + 1;
            }
            return 0;
        }

        private void StoreImageInAlphabet(char letter, Bitmap source, int letterStart, int letterEnd)
        {
            Rectangle cropRect = letterRect(source, letterStart, letterEnd); //new Rectangle(x, y, width, height);
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(source, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            alphabet[letter] = target;

            int pcount = 0;
            for (int i = 0; i < target.Width; i++)
                for (int j = 0; j < target.Height; j++)
                    if (target.GetPixel(i, j).R != 0)
                        pcount++;

            if (!hashMap.ContainsKey(pcount))
                hashMap[pcount] = new List<byte>();

            hashMap[pcount].Add((byte)letter);
        }

        private static bool HasWhiteInVerticalLine(Bitmap source, int posXInImage)
        {
            bool hasWhite = false;
            if (posXInImage >= source.Width)
                return false;

            for (int h = 0; h < source.Height; h++)
            {
                if (source.GetPixel(posXInImage, h).R != 0)
                {
                    hasWhite = true;
                    break;
                }
            }
            return hasWhite;
        }

        private static Rectangle letterRect(Bitmap source, int hStart, int hEnd)
        {

            int startWhite = -1, endWhite = -1;
            for (int j = 0; j < source.Height; j++)
            {
                for (int i = hStart; i < hEnd; i++)
                {
                    if (startWhite == -1 && source.GetPixel(i, j).R != 0)
                    {
                        startWhite = j;
                    }

                    if (endWhite == -1 && source.GetPixel(i, (source.Height - j) - 1).R != 0)
                    {
                        endWhite = (source.Height - j);
                    }
                    if (startWhite != -1 && endWhite != -1)
                        return new Rectangle(hStart, startWhite, hEnd - hStart, endWhite - startWhite);
                }
            }


            return Rectangle.Empty;
        }

        public float[] doOCR(out string OCRText, out string dinoName, string useImageFilePath = "", bool changeForegroundWindow = true)
        {
            string finishedText = "";
            dinoName = "";
            float[] finalValues = new float[1] {0};

            Bitmap screenshotbmp = null;// = (Bitmap)Bitmap.FromFile(@"D:\ScreenshotsArk\Clipboard12.png");
            Bitmap testbmp;

            debugPanel.Controls.Clear();

            if (System.IO.File.Exists(useImageFilePath))
            {
                screenshotbmp = (Bitmap)Bitmap.FromFile(useImageFilePath);
            }
            else
            {
                // grab screenshot from ark
                screenshotbmp = Win32Stuff.GetSreenshotOfProcess("ShooterGame");
                //screenshotbmp.Save(@"D:\ScreenshotsArk\Clipboard02.png");
            }
            if (screenshotbmp == null)
            {
                OCRText = "Error: no image for OCR. Is ARK running?";
                return finalValues;
            }
            if (!calibrate(screenshotbmp))
            {
                OCRText = "Error while calibrating: probably game-resolution is not supported by this OCR";
                return finalValues;
            }
            finalValues = new float[statPositions.Count];

            AddBitmapToDebug(screenshotbmp);
            if ( changeForegroundWindow )
                Win32Stuff.SetForegroundWindow(Application.OpenForms[0].Handle);

            int count = -1;
            foreach (String statName in statPositions.Keys)
            {
                count++;
                testbmp = SubImage(screenshotbmp, statPositions[statName].X, statPositions[statName].Y, 500, 30); // 300 is enough, except for the name
                AddBitmapToDebug(testbmp);

                bool onlyNumbers = coordsAfterDot; // hack for 1050, position just for coordinates

                string statOCR = "";

                if (statName == "NameAndLevel")
                    statOCR = readImage(testbmp, true, false);
                else
                    statOCR = readImage(testbmp, true, onlyNumbers);

                if (statName == "Oxygen" && statOCR == "")
                    continue;

                lastLetterositions[statName] = new Point(statPositions[statName].X + lastLetterPosition(removePixelsUnderThreshold(GetGreyScale(testbmp), whiteThreshold)), statPositions[statName].Y);

                finishedText += "\r\n" + statName + ": " + statOCR;

                // parse the OCR String

                Regex r;
                if (onlyNumbers)
                    r = new Regex(@"((\d*[\.,']?\d?)\/)?(\d*[\.,']?\d?)");
                else
                    r = new Regex(@"([a-zA-Z]*)[:;]((\d*[\.,']?\d?)\/)?(\d*[\.,']?\d?)");
                if (statName == "NameAndLevel")
                    r = new Regex(@"(.*)-?Lv[liI](\d*)Eq");

                MatchCollection mc = r.Matches(statOCR);

                if (mc.Count == 0)
                {
                    if (statName == "NameAndLevel")
                        continue;
                    else
                    {
                        OCRText = finishedText + "error reading stat " + statName;
                        return finalValues;
                    }
                }

                String testStatName = mc[0].Groups[1].Value;
                float v = 0;
                float.TryParse(mc[0].Groups[mc[0].Groups.Count - 1].Value.Replace('\'', '.').Replace(',', '.').Replace('O', '0'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("en-US"), out v); // common substitutions: comma and apostrophe to dot, 

                if (statName == "NameAndLevel")
                {
                    if (testStatName.EndsWith("-"))
                        dinoName = testStatName.Substring(0, testStatName.Length - 1);
                    else
                        dinoName = testStatName;
                }
                // TODO: test here that the read stat name corresponds to the stat supposed to be read
                finalValues[count] = v;
            }

            OCRText = finishedText;

            return finalValues;

            Bitmap grab = Win32Stuff.GetSreenshotOfProcess("ShooterGame");
            AddBitmapToDebug(grab);

            //grab.Save("E:\\Temp\\Calibration8.png", ImageFormat.Png);
            if (changeForegroundWindow )
                Win32Stuff.SetForegroundWindow(Application.OpenForms[0].Handle);
        }

        private string readImageAtCoords(Bitmap source, int x, int y, int width, int height, bool onlyMaximal, bool onlyNumbers)
        {
            return readImage(SubImage(source, x, y, width, height), onlyMaximal, onlyNumbers);
        }

        private string readImage(Bitmap source, bool onlyMaximal, bool onlyNumbers)
        {
            string result = "";
            Bitmap[] theAlphabet = alphabet;

            if (onlyNumbers)
                theAlphabet = reducedAlphabet;


            //source.Save("D:\\temp\\debug.png");
            Bitmap cleanedImage = removePixelsUnderThreshold(GetGreyScale(source), whiteThreshold);
            AddBitmapToDebug(cleanedImage);
            //cleanedImage.Save("D:\\temp\\debug_cleaned.png");


            for (int x = 0; x < cleanedImage.Width; x++)
            {
                bool foundLetter = false;
                int letterStart = 0;
                int letterEnd = 0;

                // look for the start pixel of the letter
                while (!(foundLetter == true || x >= cleanedImage.Width))
                {
                    foundLetter = HasWhiteInVerticalLine(cleanedImage, x);
                    x++;
                }

                if (foundLetter)
                {
                    letterStart = x - 1;
                    // look for the end of the letter
                    do
                    {
                        x++;
                    } while (HasWhiteInVerticalLine(cleanedImage, x) && x < cleanedImage.Width - 1);
                    letterEnd = x;
                }
                if (letterEnd > cleanedImage.Width)
                    letterEnd = cleanedImage.Width;

                if (letterStart != letterEnd)
                {
                    // found a letter, see if a match can be found
                    Rectangle letterR = letterRect(cleanedImage, letterStart, letterEnd);

                    Bitmap testImage = SubImage(cleanedImage, letterR.Left, letterR.Top, letterR.Width, letterR.Height);
                    Dictionary<int, float> matches = new Dictionary<int, float>();
                    float bestMatch = 0;
                    for (int l = 0; l < theAlphabet.Length; l++)
                    {
                        float match = 0;
                        if (theAlphabet[l] != null)
                            match = PercentageMatch(theAlphabet[l], testImage);
                        else
                            continue;

                        if (match > 0.5)
                        {
                            matches[l] = match;
                            if (bestMatch < match)
                                bestMatch = match;
                        }
                    }

                    if (matches.Count == 0)
                        continue;

                    Dictionary<int, float> goodMatches = new Dictionary<int, float>();

                    if (matches.Count == 1)
                        goodMatches = matches;
                    else
                    {
                        foreach (KeyValuePair<int, float> kv in matches)
                            if (kv.Value > 0.95 * bestMatch)
                                goodMatches[kv.Key] = kv.Value; // discard matches that are not at least 90% as good as the best match
                    }

                    if (goodMatches.Count == 1)
                        result += (char)(goodMatches.Keys.ToArray()[0]);
                    else
                    {
                        if (onlyMaximal)
                        {
                            foreach (int l in goodMatches.Keys)
                            {
                                if (goodMatches[l] == bestMatch)
                                    result += (char)l;
                            }
                        }
                        else
                        {
                            result += "[";
                            foreach (int l in goodMatches.Keys)
                                result += (char)l + goodMatches[l].ToString("{0.00}") + " ";
                            result += "]";
                        }
                    }
                }
            }

            // replace half letters.
            result = result.Replace((char)15 + "n", "n");
            result = result.Replace((char)16 + "lK", "K");

            return result;

        }


        public float PercentageMatch(Bitmap img1, Bitmap img2)
        {
            int whiteInFirstAlsoInSecond = 0;
            int whiteInFirstNotInSecond = 0;
            int blackInFirstAlsoInSecond = 0;
            int blackInFirstNotInSecond = 0;

            int minWidth = Math.Min(img1.Width, img2.Width);
            int minHeight = Math.Min(img1.Height, img2.Height);

            for (int i = 0; i < minWidth; i++)
                for (int j = 0; j < minHeight; j++)
                {
                    if (img1.GetPixel(i, j).R != 0)
                    {
                        if (img2.GetPixel(i, j).R != 0)
                            whiteInFirstAlsoInSecond++;
                        else
                            whiteInFirstNotInSecond++;
                    }
                    else
                    {
                        if (img2.GetPixel(i, j).R == 0)
                            blackInFirstAlsoInSecond++;
                        else
                            blackInFirstNotInSecond++;
                    }
                }

            // assume whites count as much as blacks.

            int totalMatches = whiteInFirstAlsoInSecond + blackInFirstAlsoInSecond;
            int totalFails = whiteInFirstNotInSecond + blackInFirstNotInSecond;

            totalFails += (Math.Max(img1.Width, img2.Width) * Math.Max(img1.Height, img2.Height) - minWidth * minHeight);

            float percentage = (float)totalMatches / (totalMatches + totalFails);
            return percentage;
        }

        internal void setDebugPanel(FlowLayoutPanel OCRDebugLayoutPanel)
        {
            debugPanel = OCRDebugLayoutPanel;
        }

        public bool isDinoInventoryVisible()
        {
            string finishedText = "";
            float[] finalValues = new float[1] { 0 };

            Bitmap screenshotbmp = null;// = (Bitmap)Bitmap.FromFile(@"D:\ScreenshotsArk\Clipboard12.png");
            Bitmap testbmp;

            if (Win32Stuff.GetForegroundWindow() != ARKProcess.MainWindowHandle)
                return false;

            screenshotbmp = Win32Stuff.GetSreenshotOfProcess("ShooterGame");

            if (screenshotbmp == null)
                return false;
            if (!calibrate(screenshotbmp))
                return false;

            String statName = "NameAndLevel";
            testbmp = SubImage(screenshotbmp, statPositions[statName].X, statPositions[statName].Y, 500, 30);
            String statOCR = readImage(testbmp, true, false);

            Regex r = new Regex(@"(.*)-?Lv[liI](\d*)Eq");
            MatchCollection mc = r.Matches(statOCR);

            if (mc.Count != 0)
                return true;

            return false;
        }

    }
}
