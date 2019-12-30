using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace TextToBitmap {
	internal class Program {

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);
        public static FontFamily ff;
        public static Font font;

        public static void Main(string[] args)
        {
            byte[] fontarray = TextToBitmap.Properties.Resources.latha;
            int dataLenght = TextToBitmap.Properties.Resources.latha.Length;

            System.IntPtr ptrData = Marshal.AllocCoTaskMem(dataLenght);

            Marshal.Copy(fontarray, 0, ptrData, dataLenght);
            uint cFonts = 0;
            AddFontMemResourceEx(ptrData, (uint)fontarray.Length, IntPtr.Zero, ref cFonts);
            PrivateFontCollection private_fonts = new PrivateFontCollection();
            private_fonts.AddMemoryFont(ptrData, dataLenght);
            Marshal.FreeCoTaskMem(ptrData);

            ff = private_fonts.Families[0];
            font = new Font(ff, 35, FontStyle.Bold);

            Bitmap bmpA = RenderText("அடுத்த பணியைப் பற்றி விவாதிப்போம். நீங்கள் நலமாக இருக்கிறீர்களா?", "output.bmp");

            string filename = "test.txt";
            byte[] bTransposeBuffer = new byte[30000];
            int ixcnt = 0;
            int iycnt = 0;
            int btf = buffertransfile(filename, bmpA, bTransposeBuffer, ixcnt, iycnt);

        }

        private static Bitmap RenderText(string text, string file) {
            
            int iWindowLength = (text.Length) * 40;

            Bitmap bmp = ConvertTextToImage(text, Color.White, Color.Black, iWindowLength, 65, -3, -1);

            bmp.Save(file);
            return bmp;
        }

        public static Bitmap ConvertTextToImage(string txt, Color bgcolor, Color fcolor, int width, int Height, int IXINDEX, int IYINDEX)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile("test.bmp");
            Bitmap newImage = ResizeBitmap(bmp, width, Height);

            using (Graphics graphics = Graphics.FromImage(newImage))
            {
                graphics.FillRectangle(new SolidBrush(bgcolor), 0, 0, newImage.Width, newImage.Height);

                graphics.DrawString(txt, font, new SolidBrush(fcolor), IXINDEX, IYINDEX);
                graphics.Flush();
            }

            return newImage;
        }               

        public static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }
            return result;
        }


        public static int buffertransfile(string filename, Bitmap bmpA, byte[] bTransposeBuffer, int iXCnt, int iYCnt)
        {
            // StreamWriter sw = File.CreateText(@"C:\BSDAT\" + filename); user can change
            StreamWriter sw = File.CreateText(filename);
            int iNoOfRows;
            int iNoOfCols;
            Color color;
            iNoOfRows = (bmpA.Height - (7 + 5)) / 3;
            iNoOfCols = (bmpA.Width) / 3;
            char bSwap;
            byte[] byteshexval = new byte[iNoOfCols];
            byte[] byteshexvalFull = new byte[16384];
            byte dSwap = 0;
            string sLineBuffer;
            string hexval;
            sLineBuffer = null;
            hexval = null;
            iNoOfCols = 0;
            int ibyteArrayCnt = 0;
            int iTransCnt = 0;

            for (int y = 5; y < bmpA.Height - 7; y = y + 3)
            {
                iYCnt++;
                sLineBuffer = null;
                hexval = null;
                byteshexval = null;
                iXCnt = 0;

                for (int x = 0; x < bmpA.Width; x = x + 3)
                {
                    iXCnt++;
                    color = bmpA.GetPixel(x, y);
                    if ((color.R == 0xFF) && (color.G == 0xFF) && (color.B == 0xFF))
                    {
                        bSwap = '0';
                        sw.Write(0);
                    }
                    else
                    {
                        bSwap = '1';
                        sw.Write(1);
                    }
                    sLineBuffer += bSwap;
                }
                if ((sLineBuffer.Length % 8) != 0)
                    sLineBuffer = sLineBuffer.PadRight(sLineBuffer.Length + (8 - (sLineBuffer.Length % 8)), '0');
                hexval = BinaryStringToHexString(sLineBuffer); // Hex values in ASCII
                byteshexval = Hex_to_ByteArray(hexval);
                for (int k = 0; k < byteshexval.Length; k++)
                {
                    byteshexvalFull[ibyteArrayCnt++] = byteshexval[k];
                }
                sw.WriteLine("\t");
            }
            sw.Close();


            int iRowCount = 16;
            int iColCount = ibyteArrayCnt / iRowCount;
            iTransCnt = 0;
            FileStream writeStreamTranspose = new FileStream("imgfiletrans.bin", FileMode.Create, FileAccess.Write);
            BinaryWriter file = new BinaryWriter(writeStreamTranspose);

            for (int j = 0; j < iColCount; j++)
            {
                for (int i = 0; i < iRowCount; i++)
                {
                    dSwap = 0;
                    dSwap = byteshexvalFull[(i * iColCount) + j];
                    bTransposeBuffer[iTransCnt++] = byteshexvalFull[(i * iColCount) + j];
                    file.Write(dSwap);
                }
            }
            file.Close();
            return iTransCnt++;
        }

        public static string BinaryStringToHexString(string binary)
        {
            StringBuilder result = new StringBuilder(binary.Length / 8 + 1);
            // TODO: check all 1's or 0's... Will throw otherwise
            int mod4Len = binary.Length % 8;
            if (mod4Len != 0)
            {
                // pad to length multiple of 8
                binary = binary.PadLeft(((binary.Length / 8) + 1) * 8, '0');
            }
            for (int i = 0; i < binary.Length; i += 8)
            {
                string eightBits = binary.Substring(i, 8);
                result.AppendFormat("{0:X2}", Convert.ToByte(eightBits, 2));
            }
            return result.ToString();
        }

        
        private static byte[] Hex_to_ByteArray(string s)
        {
            //s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
            {
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            }
            return buffer;
        }

       

    }
}