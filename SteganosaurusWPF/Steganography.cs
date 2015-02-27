﻿using System;
using System.Collections.Generic;
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
        private static int cnt, cnt2;

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
            return null;
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

        public static byte[] findY (byte[] x, int n, byte[] message)
        {
            if (n == 2)
            {
                x[8].SetBit(0, false);
                x[8].SetBit(1, false);
            }
            else if (n == 3)
            {
                x[8].SetBit(0, true);
                x[8].SetBit(1, false);
            }
            else if (n == 4)
            {
                x[8].SetBit(0, false);
                x[8].SetBit(1, true);
            }
            else
            {
                x[8].SetBit(0, true);
                x[8].SetBit(1, true);
            }

            for (int j = 0; j < 9; j++)
            {
                for (int k = 0; k < n; k++)
                    if (j == 8 && (k == n - 1 || k == n - 2))
                        continue;
                    else
                    {
                        Console.WriteLine(j);
                        Console.WriteLine(k);
                        Console.WriteLine(cnt);
                        Console.WriteLine();
                        x[j].SetBit(k, message[cnt].GetBit(cnt2));
                        cnt2++;
                        if (cnt2 == 8)
                        {
                            cnt2 = 0;
                            cnt++;
                        }
                        if (cnt >= message.Length)
                            break;
                    }
                if (cnt >= message.Length)
                    break;
            }

            return x;
        }

        public static byte[] processedBlock(byte[] x, byte[] message)
        {
            // find xmin
            byte xmin = x[0];
            byte[] y = x;
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
            bool b1 = false;
            bool b2 = true;
            if (d < 8) // 2-bit
            {
                n = 2;
                y = findY(x, n, message);
            }
            else if (d < 16) // 3-bit 
            {
                n = 3;
                y = findY(x, n, message);
            }
            else if (d < 32) // 4-bit 
            {
                n = 4;
                y = findY(x, n, message);
            }
            else // 5-bit 
            {
                n = 5;
                y = findY(x, n, message);
            }

            return z(y,x,n);
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
            cnt = 0;
            cnt2 = 0;
            // process per block
            for (int i = 0; i < pixels.Length; i += 9)
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
            Int32 messagelength = BitConverter.ToInt32(messageLengthBytes, 0);
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
            Int32 messagelength = BitConverter.ToInt32(messageLengthBytes, 0);
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
    }
}
