using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SteganosaurusWPF
{
    class Steganography
    {
        private static int byteCnt, bitCnt;

        public static bool CanDoInsertionWithAlgorithmStandard(string imagePath, string messagePath)
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
            int height = bitmapImage.PixelHeight;
            int width = bitmapImage.PixelWidth;
            int nStride = (bitmapImage.PixelWidth * bitmapImage.Format.BitsPerPixel + 7) / 8;
            byte[] messageBytes = File.ReadAllBytes(messagePath);
            string fileName = Path.GetFileName(messagePath);
            if ((4 + fileName.Length * 2 + 4 + messageBytes.Length) * 8 <= bitmapImage.PixelHeight * nStride)
            {
                return true;
            }
            else return false;
        }

        public static BitmapSource InsertionWithAlgorithmStandard(string imagePath, string messagePath, string key)
        {
            // Read pixel
            BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
            int height = bitmapImage.PixelHeight;
            int width = bitmapImage.PixelWidth;
            int nStride = (bitmapImage.PixelWidth * bitmapImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[bitmapImage.PixelHeight * nStride];
            bitmapImage.CopyPixels(pixels, nStride, 0);
            Console.WriteLine("Capacity = " +  pixels.Length);

            // Create extended message
            string fileName = Path.GetFileName(messagePath);
            byte[] fileNameLengthBytes = BitConverter.GetBytes((Int32)fileName.Length);

            byte[] fileNameBytes = new byte[fileName.Length * 2];
            for (int i = 0; i < fileName.Length; i++)
            {
                byte[] bt = BitConverter.GetBytes(fileName[i]);
                fileNameBytes[i * 2] = bt[0];
                fileNameBytes[i * 2 + 1] = bt[1];
            }
            byte[] messageBytesBeforeEncrypt = File.ReadAllBytes(messagePath);
            byte[] messageBytes = Viginere.Encrypt(messageBytesBeforeEncrypt, key);
            byte[] messageLengthBytes = BitConverter.GetBytes((Int32)messageBytes.Length);
            byte[] messageExtended = new byte[4 + fileName.Length * 2 + 4 + messageBytes.Length];
            int idx = 0;
            for (int i = 0; i < fileNameLengthBytes.Length; i++)
            {
                messageExtended[idx] = fileNameLengthBytes[i];
                idx++;
            }
            for (int i = 0; i < fileNameBytes.Length; i++)
            {
                messageExtended[idx] = fileNameBytes[i];
                idx++;
            }
            for (int i = 0; i < messageLengthBytes.Length; i++)
            {
                messageExtended[idx] = messageLengthBytes[i];
                idx++;
            }
            for (int i = 0; i < messageBytes.Length; i++)
            {
                messageExtended[idx] = messageBytes[i];
                idx++;
            }

            // Inserting message
            List<int> seq = PRNG.GenerateSequence(key, pixels.Length);
            for (int i = 0; i < messageExtended.Length; i++)
            {
                for (byte j = 0; j < 8; j++)
                {
                    pixels[seq[i * 8 + j]] = pixels[seq[i * 8 + j]].SetBit(0, messageExtended[i].GetBit(j));
                }
            }
            BitmapSource bitmapOutput = BitmapImage.Create(bitmapImage.PixelWidth, bitmapImage.PixelHeight, bitmapImage.DpiX, bitmapImage.DpiY, bitmapImage.Format, bitmapImage.Palette, pixels, nStride);
            return bitmapOutput;
        }

        public static BitmapSource InsertionWithAlgorithmLiao(string imagePath, string messagePath, string key)
        {
            // Create extended message
            string fileName = Path.GetFileName(messagePath);
            byte[] fileNameLengthBytes = BitConverter.GetBytes((Int32)fileName.Length);

            byte[] fileNameBytes = new byte[fileName.Length * 2];
            for (int i = 0; i < fileName.Length; i++)
            {
                byte[] bt = BitConverter.GetBytes(fileName[i]);
                fileNameBytes[i * 2] = bt[0];
                fileNameBytes[i * 2 + 1] = bt[1];
            }
            byte[] messageBytesBeforeEncrypt = File.ReadAllBytes(messagePath);
            byte[] messageBytes = Viginere.Encrypt(messageBytesBeforeEncrypt, key);
            byte[] messageLengthBytes = BitConverter.GetBytes((Int32)messageBytes.Length);
            byte[] messageExtended = new byte[4 + fileName.Length * 2 + 4 + messageBytes.Length];
            int idx = 0;
            for (int i = 0; i < fileNameLengthBytes.Length; i++)
            {
                messageExtended[idx] = fileNameLengthBytes[i];
                idx++;
            }
            for (int i = 0; i < fileNameBytes.Length; i++)
            {
                messageExtended[idx] = fileNameBytes[i];
                idx++;
            }
            for (int i = 0; i < messageLengthBytes.Length; i++)
            {
                messageExtended[idx] = messageLengthBytes[i];
                idx++;
            }
            for (int i = 0; i < messageBytes.Length; i++)
            {
                messageExtended[idx] = messageBytes[i];
                idx++;
            }

            //Lockbitmap pixel handling

            LiaoAlgorithm lal = new LiaoAlgorithm();
            Bitmap bmp = (Bitmap)Image.FromFile(imagePath, true);
            LockBitmap lockBitmap = new LockBitmap(bmp);
            BitArray bitmsg = new BitArray(messageExtended);
            lockBitmap.LockBits();
            byte[] pixelInput = new byte[4];
            bool looping = true;
            for (int i = 0; (i < lockBitmap.Width / 2) && looping; i++)
            {
                for (int j = 0; j < lockBitmap.Height/2 && looping; j++)
                {
                    pixelInput[0] = lockBitmap.GetPixel(i * 2, j * 2).R;
                    pixelInput[1] = lockBitmap.GetPixel(i * 2 + 1, j * 2).R;
                    pixelInput[2] = lockBitmap.GetPixel(i * 2, j * 2 + 1).R;
                    pixelInput[3] = lockBitmap.GetPixel(i * 2 + 1, j * 2 + 1).R;

                    if (lal.liaoEncrypt(pixelInput, 5, 2, 3, bitmsg) != null) //parameter undefined
                    {
                        pixelInput = lal.liaoEncrypt(pixelInput, 5, 2, 3, bitmsg);
                        if ((bitmsg.Length - 4 * lal.recentk) <= 0)
                        {
                            looping = false;
                        }
                        else
                        {
                            BitArray temp = new BitArray(bitmsg.Length - 4 * lal.recentk);
                            for (int k = 0; k < temp.Length; k++)
                            {
                                temp[k] = bitmsg[k + 4 * lal.recentk];
                            }
                            bitmsg = temp;
                        }                       

                        lockBitmap.SetPixel(i * 2, j * 2, System.Drawing.Color.FromArgb(pixelInput[0], pixelInput[0], pixelInput[0]));
                        lockBitmap.SetPixel(i * 2 + 1, j * 2, System.Drawing.Color.FromArgb(pixelInput[1], pixelInput[1], pixelInput[1]));
                        lockBitmap.SetPixel(i * 2, j * 2 + 1, System.Drawing.Color.FromArgb(pixelInput[2], pixelInput[2], pixelInput[2]));
                        lockBitmap.SetPixel(i * 2 + 1, j * 2 + 1, System.Drawing.Color.FromArgb(pixelInput[3], pixelInput[3], pixelInput[3]));
                    }
                }
            }
            lockBitmap.UnlockBits();            
            BitmapSource bs = LiaoAlgorithm.ConvertBitmap(bmp);
            return bs;         
        }

        public static FileTemp ExtractionWithAlgorithmStandard(string imagePath, string key)
        {
            // Read pixel
            BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
            int height = bitmapImage.PixelHeight;
            int width = bitmapImage.PixelWidth;
            int nStride = (bitmapImage.PixelWidth * bitmapImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[bitmapImage.PixelHeight * nStride];
            bitmapImage.CopyPixels(pixels, nStride, 0);
            // Get message
            List<int> seq = PRNG.GenerateSequence(key, pixels.Length);
            byte[] fileNameLengthBytes = new byte[4];
            int idx = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    fileNameLengthBytes[i] = fileNameLengthBytes[i].SetBit(j, pixels[seq[idx]].GetBit(0));
                    idx++;
                }
            }
            Int32 filenameLength = BitConverter.ToInt16(fileNameLengthBytes, 0);
            byte[] fileNameBytes = new byte[filenameLength * 2];
            for (int i = 0; i < filenameLength * 2; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    fileNameBytes[i] = fileNameBytes[i].SetBit(j, pixels[seq[idx]].GetBit(0));
                    idx++;
                }
            }
            string fileName = "";
            for (int i = 0; i < filenameLength * 2; i += 2)
            {
                fileName = fileName + BitConverter.ToChar(fileNameBytes, i);
            }
            byte[] messageLengthBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    messageLengthBytes[i] = messageLengthBytes[i].SetBit(j, pixels[seq[idx]].GetBit(0));
                    idx++;
                }
            }
            Int32 messagelength = BitConverter.ToInt32(messageLengthBytes,0);            
            byte[] messageBytesBeforeDecrypt = new byte[messagelength];
            for (int i = 0; i < messagelength; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    messageBytesBeforeDecrypt[i] = messageBytesBeforeDecrypt[i].SetBit(j, pixels[seq[idx]].GetBit(0));
                    idx++;
                }
            }

            byte[] messageBytes = Viginere.Decrypt(messageBytesBeforeDecrypt, key);

            FileTemp file = new FileTemp(fileName);
            file.Data = messageBytes;

            return file;
        }

        public static double CalculatePSNR(string imagePath1, string imagePath2)
        {
            // Read pixel image before
            // Console.WriteLine("Halo 1");
            BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath1));
            int height = bitmapImage.PixelHeight;
            int width = bitmapImage.PixelWidth;
            int nStride = (bitmapImage.PixelWidth * bitmapImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[bitmapImage.PixelHeight * nStride];
            bitmapImage.CopyPixels(pixels, nStride, 0);

            // Read pixel image after
            // Console.WriteLine("Halo 2");
            BitmapImage bitmapImage2 = new BitmapImage(new Uri(imagePath2));
            int height2 = bitmapImage2.PixelHeight;
            int width2 = bitmapImage2.PixelWidth;
            int nStride2 = (bitmapImage2.PixelWidth * bitmapImage2.Format.BitsPerPixel + 7) / 8;
            byte[] pixels2 = new byte[bitmapImage2.PixelHeight * nStride];
            bitmapImage2.CopyPixels(pixels2, nStride2, 0);

            // Console.WriteLine("Halo 3");
            double totalDif = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                double dif = (pixels[i] - pixels2[i]) * (pixels[i] - pixels2[i]);
                totalDif += dif;
            }
            double rms = Math.Sqrt(totalDif);
            double psnr = 20 * Math.Log10(256 / rms);
            Console.WriteLine("PSNR = " + psnr);
            return psnr;
        }
        public static FileTemp ExtractionWithAlgorithmLiao(string imagePath, string key)
        {
            LiaoAlgorithm lal = new LiaoAlgorithm();
            Bitmap bmp = (Bitmap)Image.FromFile(imagePath, true);
            LockBitmap lockBitmap = new LockBitmap(bmp);
            lockBitmap.LockBits();
            byte[] pixelInput = new byte[4];
            bool looping = true;

            BitArray namelength = new BitArray(32);
            List<bool> remain = new List<bool>();
            BitArray result = new BitArray(lockBitmap.Height * lockBitmap.Width * 8);
            //BitArray temp = new BitArray()
            int excount = 0;

            for (int i = 0; (i < lockBitmap.Width / 2) && looping; i++)
            {
                for (int j = 0; j < lockBitmap.Height / 2 && looping; j++)
                {
                    pixelInput[0] = lockBitmap.GetPixel(i * 2, j * 2).R;
                    pixelInput[1] = lockBitmap.GetPixel(i * 2 + 1, j * 2).R;
                    pixelInput[2] = lockBitmap.GetPixel(i * 2, j * 2 + 1).R;
                    pixelInput[3] = lockBitmap.GetPixel(i * 2 + 1, j * 2 + 1).R;

                    if (lal.liaoDecrypt(pixelInput, 5, 2, 3) != null) //parameter undefined
                    {
                        BitArray temp = new BitArray(lal.liaoDecrypt(pixelInput, 5, 2, 3));
                        
                        for (int k = 0; k < temp.Count; k++)
                        {
                            result[excount++] = temp[k];
                        }
                    }
                }
            }

            byte[] fileNameLengthBytes = new byte[4];
            int resultcounter = 0;
            BitArray tempba = new BitArray(8);
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    tempba[j] = result[resultcounter++];                    
                }
                fileNameLengthBytes[i] = LiaoAlgorithm.ConvertToByte(tempba);
            }
            Int32 filenameLength = BitConverter.ToInt16(fileNameLengthBytes, 0);
            byte[] fileNameBytes = new byte[filenameLength * 2];
            for (int i = 0; i < filenameLength * 2; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    tempba[j] = result[resultcounter++];
                }
                fileNameBytes[i] = LiaoAlgorithm.ConvertToByte(tempba);
            }
            string fileName = "";
            for (int i = 0; i < filenameLength * 2; i += 2)
            {
                fileName = fileName + BitConverter.ToChar(fileNameBytes, i);
            }
            byte[] messageLengthBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    tempba[j] = result[resultcounter++];
                }
                messageLengthBytes[i] = LiaoAlgorithm.ConvertToByte(tempba);
            }
            Int32 messagelength = BitConverter.ToInt32(messageLengthBytes, 0);
            for (int te = 0; te < messageLengthBytes.Length; te++)
            {
                Console.Write(messageLengthBytes[te] + ",");
            }
            byte[] messageBytesBeforeDecrypt = new byte[messagelength];
            for (int i = 0; i < messagelength; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    tempba[j] = result[resultcounter++];
                }
                messageBytesBeforeDecrypt[i] = LiaoAlgorithm.ConvertToByte(tempba);
            }
            byte[] messageBytes = Viginere.Decrypt(messageBytesBeforeDecrypt, key);

            FileTemp file = new FileTemp(fileName);
            file.Data = messageBytes;

            return file;
        }

        public static int s2(int x)
        {
            int ret = 1;
            for (int i = 0; i < x; i++)
            {
                ret = ret * 2;
            }
            return ret;
        }

        public static byte[] z(byte[] y, byte[] x, int n)
        {
            int [] z = new int[9];
            for (int i = 0; i < 9; i++)
            {
                z[i] = (int)y[i];
            }
            for (int i = 0; i < 9; i++)
            {
                if (y[i] >= x[i] + s2(n - 1) + 1)
                {
                    z[i] = (int)y[i] - s2(n);
                }
                else if (y[i] <= x[i] - s2(n - 1) + 1)
                {
                    z[i] = (int)y[i] + s2(n);
                }
                else
                {
                    z[i] = y[i];
                }
            }

            for (int i = 0; i < 9; i ++) {
                while (z[i] < 0 || z[i] > 255)
                {
                    if (z[i] < 0)
                    {
                        z[i] = z[i] + s2(n);
                    }
                    else if (z[i] > 255)
                    {
                        z[i] = z[i] - s2(n);
                    }
                }
            }
            byte[] z2 = new byte[9];
            for (int i = 0; i < 9; i++)
                z2[i] = (byte)z[i];

            return z2;
        }

        public static byte[] y (byte[] x, int n, byte[] message)
        {
            if (n == 2)
            {
                x[8] = x[8].SetBit(0, false);
                x[8] = x[8].SetBit(1, false);
            }
            else if (n == 3)
            {
                x[8] = x[8].SetBit(0, true);
                x[8] = x[8].SetBit(1, false);
            }
            else if (n == 4)
            {
                x[8] = x[8].SetBit(0, false);
                x[8] = x[8].SetBit(1, true);
            }
            else
            {
                x[8] = x[8].SetBit(0, true);
                x[8] = x[8].SetBit(1, true);
            }

            int cnt = 0;
            for (int i = 0; i < 9; i++)
            {
                for (int j = n-1; j >= 0; j--)
                    if (i == 8 && (j == n - 1 || j == n - 2))
                        continue;
                    else
                    {
                        if (byteCnt >= message.Length)
                            break;
                        cnt++;
                        byte mc = message[byteCnt];
                        bool m = mc.GetBit(bitCnt);
                        x[i] = x[i].SetBit(j, m);
                        bitCnt++;
                        if (bitCnt == 8)
                        {
                            bitCnt = 0;
                            byteCnt++;
                        }
                    }
                if (byteCnt >= message.Length)
                    break;
            }/*
            Console.Write(message.Length);
            Console.Write(' ');
            Console.Write(n);
            Console.Write(' ');
            Console.WriteLine(cnt);*/

            return x;
        }

        public static byte[] processedBlock(byte[] x, byte[] message)
        {
            // find xmin
            byte xmin = x[0];
            for (int j = 1; j < 9; j++)
            {
                xmin = Math.Max(xmin, x[j]);
            }

            // find d
            int d = 0;
            for (int j = 0; j < 9; j++)
            {
                d = d + Math.Abs(x[j] - xmin);
            }
            d = d / 8;

            // find n
            int n = 0;
            if (d < 8) // 2-bit
                n = 2;
            else if (d < 16) // 3-bit 
                n = 3;
            else if (d < 32) // 4-bit 
                n = 4;
            else // 5-bit 
                n = 5;

//            return z(y,x,n);
            return y(x, n, message);
        }

        public static BitmapSource InsertionWithAlgorithmSwain(string imagePath, string messagePath, string key)
        {
            // Read pixel
            BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
            int height = bitmapImage.PixelHeight;
            int width = bitmapImage.PixelWidth;
            int nStride = (bitmapImage.PixelWidth * bitmapImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[bitmapImage.PixelHeight * nStride];
            bitmapImage.CopyPixels(pixels, nStride, 0);

            // Create extended message
            string fileName = Path.GetFileName(messagePath);
            byte[] fileNameLengthBytes = BitConverter.GetBytes((Int32)fileName.Length);

            byte[] fileNameBytes = new byte[fileName.Length * 2];
            for (int i = 0; i < fileName.Length; i++)
            {
                byte[] bt = BitConverter.GetBytes(fileName[i]);
                fileNameBytes[i * 2] = bt[0];
                fileNameBytes[i * 2 + 1] = bt[1];
            }
            byte[] messageBytesBeforeEncrypt = File.ReadAllBytes(messagePath);
            byte[] messageBytes = Viginere.Encrypt(messageBytesBeforeEncrypt, key);
            byte[] messageLengthBytes = BitConverter.GetBytes((Int32)messageBytes.Length);
            byte[] messageExtended = new byte[4 + fileName.Length * 2 + 4 + messageBytes.Length];
            int idx = 0;
            for (int i = 0; i < fileNameLengthBytes.Length; i++)
            {
                messageExtended[idx] = fileNameLengthBytes[i];
                idx++;
            }
            for (int i = 0; i < fileNameBytes.Length; i++)
            {
                messageExtended[idx] = fileNameBytes[i];
                idx++;
            }
            for (int i = 0; i < messageLengthBytes.Length; i++)
            {
                messageExtended[idx] = messageLengthBytes[i];
                idx++;
            }
            for (int i = 0; i < messageBytes.Length; i++)
            {
                messageExtended[idx] = messageBytes[i];
                idx++;
            }

            // Inserting message
            byteCnt = 0;
            bitCnt = 0;
            // process per block
            for (int i = 0; i < pixels.Length-8; i += 9)
            {
                // copy pixels to block
                byte[] block = new byte[9];
                block[0] = pixels[i];
                block[1] = pixels[i + 1];
                block[2] = pixels[i + 2];
                block[3] = pixels[i + 3];
                block[4] = pixels[i + 4];
                block[5] = pixels[i + 5];
                block[6] = pixels[i + 6];
                block[7] = pixels[i + 7];
                block[8] = pixels[i + 8];
                block = processedBlock(block,messageExtended);

                // fill the pixels with processed block
                pixels[i] = block[0];
                pixels[i + 1] = block[1];
                pixels[i + 2] = block[2];
                pixels[i + 3] = block[3];
                pixels[i + 4] = block[4];
                pixels[i + 5] = block[5];
                pixels[i + 6] = block[6];
                pixels[i + 7] = block[7];
                pixels[i + 8] = block[8];
            }

            BitmapSource bitmapOutput = BitmapImage.Create(bitmapImage.PixelWidth, bitmapImage.PixelHeight, bitmapImage.DpiX, bitmapImage.DpiY, bitmapImage.Format, bitmapImage.Palette, pixels, nStride);
            return bitmapOutput;
        }

        public static byte[] x(byte[] y, int n, int size)
        {
            byte[] ret = new byte[size];
            for (int i = 0; i < 9; i++)
            {
                for (int j = n - 1; j >= 0; j--)
                    if (i == 8 && (j == n - 1 || j == n - 2))
                        continue;
                    else
                    {
                        ret[byteCnt] = ret[byteCnt].SetBit(bitCnt, y[i].GetBit(j));
                        bitCnt++;
                        if (bitCnt == 8)
                        {
                            bitCnt = 0;
                            byteCnt++;
                        }
                    }
            }
            return ret;
        }

        public static byte[] message(byte[] s)
        {
            int n = 0;
            if (!s[8].GetBit(0)  && !s[8].GetBit(1)) // 2-bit
                n = 2;
            else if (s[8].GetBit(0) && !s[8].GetBit(1)) // 3-bit
                n = 3;
            else if (!s[8].GetBit(0) && s[8].GetBit(1)) // 4-bit
                n = 4;
            else if (s[8].GetBit(0) && s[8].GetBit(1)) // 5-bit
                n = 5;

            return x(s, n, n * 9 - 2);
        }

        public static FileTemp ExtractionWithAlgorithmSwain(string imagePath, string key)
        {
            // Read pixel
            BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
            int height = bitmapImage.PixelHeight;
            int width = bitmapImage.PixelWidth;
            int nStride = (bitmapImage.PixelWidth * bitmapImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[bitmapImage.PixelHeight * nStride];
            bitmapImage.CopyPixels(pixels, nStride, 0);
            
            // Get message
            byte[] messageExtended = new byte[]{};
            // process per block
            for (int i = 0; i < pixels.Length - 8; i += 9)
            {
                // copy pixels to block
                byteCnt = 0;
                bitCnt = 0;
                byte[] block = new byte[9];
                block[0] = pixels[i];
                block[1] = pixels[i + 1];
                block[2] = pixels[i + 2];
                block[3] = pixels[i + 3];
                block[4] = pixels[i + 4];
                block[5] = pixels[i + 5];
                block[6] = pixels[i + 6];
                block[7] = pixels[i + 7];
                block[8] = pixels[i + 8];
                byte[] newMessage = message(block);
                byte[] prevMessage = messageExtended;
                messageExtended = new byte[newMessage.Length + prevMessage.Length];
//                Console.WriteLine(messageExtended.Length);
                for (int j = 0; j < prevMessage.Length; j++)
                    messageExtended[j] = prevMessage[j];
                for (int j = 0; j < newMessage.Length; j++)
                    messageExtended[j+prevMessage.Length] = newMessage[j];
            }


            byte[] fileNameLengthBytes = new byte[4];
            byteCnt = 0; 
            for (int i = 0; i < 4; i++)
            {
                fileNameLengthBytes[i] = messageExtended[byteCnt];
                byteCnt++;
            }
            Int32 filenameLength = BitConverter.ToInt16(fileNameLengthBytes, 0);
            byte[] fileNameBytes = new byte[filenameLength * 2];
            for (int i = 0; i < filenameLength * 2; i++)
            {
                fileNameBytes[i] = messageExtended[byteCnt];
                byteCnt++;
            }
            string fileName = "";
            for (int i = 0; i < filenameLength * 2; i += 2)
            {
                fileName = fileName + BitConverter.ToChar(fileNameBytes, i);
            }
            byte[] messageLengthBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                messageLengthBytes[i] = messageExtended[byteCnt];
                byteCnt++;
            }
            Int32 messagelength = BitConverter.ToInt32(messageLengthBytes, 0);
            byte[] messageBytesBeforeDecrypt = new byte[messagelength];
            for (int i = 0; i < messagelength; i++)
            {
                messageBytesBeforeDecrypt[i] = messageExtended[byteCnt];
                byteCnt++;
            }

            byte[] messageBytes = Viginere.Decrypt(messageBytesBeforeDecrypt, key);

            FileTemp file = new FileTemp(fileName);
            file.Data = messageBytes;

            return file;
        }
    }
}
