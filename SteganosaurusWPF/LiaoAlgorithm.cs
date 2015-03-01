using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SteganosaurusWPF
{
    class LiaoAlgorithm
    {
        public int recentk = 0;

        public static byte ConvertToByte(BitArray bits)
        {
            if (bits.Count != 8)
            {
                throw new ArgumentException("bits");
            }
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }

        public byte[] liaoEncrypt(byte[] pixels, int T, int Kl, int Kh, BitArray bitmsg) {
            float D;
            int k;            
            //Step1
            int[] convd = Array.ConvertAll(pixels, c => (int)c);
            D = calculateDiff(convd);
            //Step2
            k = thresholding(D, T, Kl, Kh);
            recentk = k;
            //Step3
            if (!checkErrorBlock(convd,D,T)) {
                return null;
            }
            //Step4
            int msgcounter = 0;
            byte[] result = new byte[4];
            for (int x = 0; x < 4; x++)
            {
                BitArray b = new BitArray(new byte[] { pixels[x] });
                BitArray msgtemp = new BitArray(k);
                for (int i = k - 1; i >= 0; i--)
                {
                    if (bitmsg.Length > msgcounter+1) {
                        msgtemp[i] = bitmsg[msgcounter];
                        msgcounter++;
                    }
                }
                for (int i = 0; i < k; i++)
                {
                    b[i] = msgtemp[i];
                }
                result[x] = ConvertToByte(b);
            }
            //Step5
            for (int x = 0; x < 4; x++)
            {
                result[x] = modLSBsubs(result[x], pixels[x], k);           
            }
            //Step6
            int tempsum = int.MaxValue;
            byte[] tempres = new byte[4];
            int[] readj = new int[4];
            int multiplier = (int)Math.Pow(2, k);
            for (int p = -1; p < 2; p++) {
                for (int q= -1; q < 2; q++)
                {
                    for (int r = -1; r < 2; r++)
                    {
                        for (int s = -1; s < 2; s++)
                        {
                            readj[0] = result[0] + p * multiplier;
                            readj[1] = result[1] + q * multiplier;
                            readj[2] = result[2] + r * multiplier;
                            readj[3] = result[3] + s * multiplier;
                            //6a
                            float readjf = calculateDiff(readj);
                            if (k == thresholding(readjf, T, Kl, Kh))
                            {
                                //6b
                                if (checkErrorBlock(readj, readjf, T))
                                {
                                    //6c
                                    if ((pixelDifference(readj, convd)) < tempsum) {
                                        tempsum = pixelDifference(readj, convd);
                                        for (int x = 0; x < 4; x++) {
                                            tempres[x] = (byte)readj[x];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return tempres;
        }

        public BitArray liaoDecrypt(byte[] pixels, int T, int Kl, int Kh)
        {
            float D;
            int k;
            //Step1
            int[] convd = Array.ConvertAll(pixels, c => (int)c);
            D = calculateDiff(convd);
            //Step2
            k = thresholding(D, T, Kl, Kh);
            BitArray result = new BitArray(k * 4);
            recentk = k;
            //Step3
            if (!checkErrorBlock(convd, D, T))
            {
                return null;
            }
            else
            {
                int msgcounter = 0;
                byte[] res = new byte[4];
                for (int x = 0; x < 4; x++)
                {
                    BitArray b = new BitArray(new byte[] { pixels[x] });
                    BitArray msgtemp = new BitArray(k);
                    for (int i = k - 1; i >= 0; i--)
                    {
                        result[msgcounter] = b[i];
                        msgcounter++;
                    }
                }
                return result;
            }
        }

        private int thresholding(float D, int T, int Kl, int Kh)
        {
            if (D <= T) return Kl;
            else return Kh;           
        }

        private int pixelDifference(int[] pix1, int[] pix2)
        {
            double res = 0;
            for (int i = 0; i < 4; i++)
            {
                res += Math.Pow(pix1[i] - pix2[i], 2);
            }
            return (int)res;
        }

        private float calculateDiff(int[] pixels)
        {
            int ymin = (int)Math.Min(Math.Min(pixels[0], pixels[1]), Math.Min(pixels[2], pixels[3]));
            int sum = 0;
            for (int i = 0; i < 4; i++)
            {
                sum += (int)pixels[i] - ymin;
            }
            float result = (float)sum / 3;           
            return result;
        }

        private bool checkErrorBlock(int[] pixels, float D, int T)
        {
            int ymax = (int)Math.Max(Math.Max(pixels[0], pixels[1]), Math.Max(pixels[2], pixels[3]));
            int ymin = (int)Math.Min(Math.Min(pixels[0], pixels[1]), Math.Min(pixels[2], pixels[3]));
            if ((D <= T) && (ymax - ymin > 2 * T + 2)) return false; //error block
            else return true;
        }

        private int getIntFromBitArray(BitArray bitArray)
        {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }

        public static BitmapSource ConvertBitmap(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }

        byte modLSBsubs(byte msg, byte ori, int k)
        {
            BitArray bmsg = new BitArray(new byte[] { msg });
            BitArray bori = new BitArray(new byte[] { ori});
            int intori = getIntFromBitArray(bori);
            int intmsg = getIntFromBitArray(bmsg);
            int intmod = intmsg + (int)Math.Pow(2, k);
            int intmod2 = intmsg - (int)Math.Pow(2, k);
            
            int msgdiff = Math.Abs(intmsg-intori);
            int moddiff = Math.Abs(intmod-intori);
            int mod2diff = Math.Abs(intmod2-intori);

            int result = Math.Min(msgdiff, Math.Min(moddiff, mod2diff));
            if (result == msgdiff) return (byte)intmsg;
            else if (result == moddiff) return (byte)intmod;
            else return (byte)intmod2;
        }        
    }
}
