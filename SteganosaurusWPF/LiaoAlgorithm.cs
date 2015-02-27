using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SteganosaurusWPF
{
    class LiaoAlgorithm
    {
        byte ConvertToByte(BitArray bits)
        {
            if (bits.Count != 8)
            {
                throw new ArgumentException("bits");
            }
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }

        public byte[] liaoEncrypt(byte[] pixels, int T, int Kl, int Kh, byte[] msg) {
            float D;
            int k;
            BitArray bitmsg = new BitArray(msg);
            foreach (Object obj in bitmsg)
            {
                Debug.Write(obj + " ");
            }
            //Step1
            int[] convd = Array.ConvertAll(pixels, c => (int)c);
            D = calculateDiff(convd);
            //Step2
            k = thresholding(D, T, Kl, Kh);
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
                    msgtemp[i] = bitmsg[msgcounter];
                    msgcounter++;
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
           // Debug.WriteLine("Result diff : " + result);            
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

            Debug.WriteLine("modlsbs : " + intori + "," + intmsg + "," + intmod + "," + intmod2);
            int result = Math.Min(msgdiff, Math.Min(moddiff, mod2diff));
            if (result == msgdiff) return (byte)intmsg;
            else if (result == moddiff) return (byte)intmod;
            else return (byte)intmod2;
        }

        static void Main(string[] args)
        {
            /*
            Bitmap bmp = (Bitmap)Image.FromFile(@"D:/peppers.bmp", true);
            LockBitmap lockBitmap = new LockBitmap(bmp);
            lockBitmap.LockBits();
 
            Color compareClr = Color.FromArgb(0,0,0,0);
            byte newclr;
    
            for (int y = 0; y < lockBitmap.Height; y++)
            {
                for (int x = 0; x < lockBitmap.Width; x++)
                {
                    compareClr = lockBitmap.GetPixel(x, y);
                    newclr = compareClr.R;
                    if (compareClr.R < 200) {
                        newclr++;
                    } else {
                        newclr--;
                    }
                    lockBitmap.SetPixel(x, y, Color.FromArgb(newclr, newclr, newclr));
                }
            }
            lockBitmap.UnlockBits();
            bmp.Save("newpeppers.bmp");
            */
            LiaoAlgorithm it = new LiaoAlgorithm();
            byte[] newbtest = {139,146,137,142};
            bool[] inputarray1 = { false, false, false, true, true, true, true, true };
            bool[] inputarray2 = { true, true, false, true, false, false, false, false };
            BitArray inp1 = new BitArray(inputarray1);
            BitArray inp2 = new BitArray(inputarray2);
            byte[] inputt = {it.ConvertToByte(inp1), it.ConvertToByte(inp2) };
            
            newbtest = it.liaoEncrypt(newbtest, 5, 2, 3, inputt);
            Debug.WriteLine("Final Result :");
            for (int i = 0; i < 4; i++)
            {
                Debug.WriteLine(newbtest[i]);
            }   
        }
    }
}
