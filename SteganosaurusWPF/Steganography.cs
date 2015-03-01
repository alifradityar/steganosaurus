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
        public static bool CanDoInsertionWithAlgorithmStandard(string imagePath, string messagePath)
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
            int height = bitmapImage.PixelHeight;
            int width = bitmapImage.PixelWidth;
            int nStride = (bitmapImage.PixelWidth * bitmapImage.Format.BitsPerPixel + 7) / 8;
            byte[] messageBytes = File.ReadAllBytes(messagePath);
            string fileName = Path.GetFileName(messagePath);
            if ((4 + fileName.Length*2 + 4 + messageBytes.Length) * 8 <= bitmapImage.PixelHeight * nStride)
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

            byte[] fileNameBytes = new byte[fileName.Length*2];
            for (int i=0;i<fileName.Length;i++){
                byte[] bt = BitConverter.GetBytes(fileName[i]);
                fileNameBytes[i*2] = bt[0];
                fileNameBytes[i*2+1] = bt[1];
            }
            byte[] messageBytesBeforeEncrypt = File.ReadAllBytes(messagePath);
            byte[] messageBytes = Viginere.Encrypt(messageBytesBeforeEncrypt,key);
            byte[] messageLengthBytes = BitConverter.GetBytes((Int32)messageBytes.Length);
            byte[] messageExtended = new byte[4 + fileName.Length*2 + 4 + messageBytes.Length];
            int idx = 0;
            for (int i=0;i<fileNameLengthBytes.Length;i++){
                messageExtended[idx] = fileNameLengthBytes[i];
                idx++;
            }
            for (int i=0;i<fileNameBytes.Length;i++){
                messageExtended[idx] = fileNameBytes[i];
                idx++;
            }
            for (int i=0;i<messageLengthBytes.Length;i++){
                messageExtended[idx] = messageLengthBytes[i];
                idx++;
            }
            for (int i=0;i<messageBytes.Length;i++){
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

        public static BitmapSource InsertionWithAlgorithmSwain(string imagePath, string messagePath, string key)
        {
            return null;
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
                for (int j=0;j<8;j++){
                    fileNameLengthBytes[i] = fileNameLengthBytes[i].SetBit(j, pixels[seq[idx]].GetBit(0));
                    idx++;
                }
            }
            Int32 filenameLength = BitConverter.ToInt16(fileNameLengthBytes, 0);
            byte[] fileNameBytes = new byte[filenameLength*2];
            for (int i = 0; i <filenameLength*2; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    fileNameBytes[i] = fileNameBytes[i].SetBit(j, pixels[seq[idx]].GetBit(0));
                    idx++;
                }
            }
            string fileName = "";
            for (int i=0;i<filenameLength*2;i+=2){
                fileName = fileName + BitConverter.ToChar(fileNameBytes,i);
            }
            byte[] messageLengthBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                for (int j=0;j<8;j++){
                    messageLengthBytes[i] = messageLengthBytes[i].SetBit(j, pixels[seq[idx]].GetBit(0));
                    idx++;
                }
            }
            Int32 messagelength = BitConverter.ToInt32(messageLengthBytes,0);            
            byte[] messageBytesBeforeDecrypt = new byte[messagelength];
            for (int i = 0; i < messagelength; i++)
            {
                for (int j=0;j<8;j++){
                    messageBytesBeforeDecrypt[i] = messageBytesBeforeDecrypt[i].SetBit(j, pixels[seq[idx]].GetBit(0));
                    idx++;
                }
            }

            byte[] messageBytes = Viginere.Decrypt(messageBytesBeforeDecrypt, key);

            FileTemp file = new FileTemp(fileName);
            file.Data = messageBytes;

            return file;
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
    }

    
}
