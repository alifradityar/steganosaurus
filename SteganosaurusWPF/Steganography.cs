using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteganosaurusWPF
{
    class Steganography
    {
        public static Byte[] InsertionWithAlgorithmStandard(Byte[] imageData, Byte[] messageDate, string key)
        {
            return null;
        }

        public static Byte[] InsertionWithAlgorithmLiao(Byte[] imageData, Byte[] messageDate, string key)
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

        public static Byte z(Byte y, Byte x, int n)
        {
            int z;
            if (y >= x + s2(n - 1) + 1)
            {
                z = y - s2(n);
            }
            else if (y <= x - s2(n - 1) + 1)
            {
                z = y + s2(n);
            }
            else
            {
                z = y;
            }
            // if z < 0 or z > 255
            while (z < 0 || z > 255)
            {
                if (z < 0)
                {
                    z = z + s2(n);
                }
                else if (z > 255)
                {
                    z = z - s2(n);
                }
            }
            return (byte)z;
        }

        public static Byte[] InsertionWithAlgorithmSwain(Byte[] imageData, Byte[] messageDate, string key)
        {
            Byte[] newImageData = imageData;
            for (int i = 0; i < imageData.Length; i += 3)
            {
                int[] x = new int[9];
                int[] y = new int[9];
                int d = 0;
                int xmin = x[0];
                for (int j = 1; j < 9; j++)
                {
                    Math.Max(xmin, x[j]);
                }

                for (int j = 0; j < 9; j++)
                {
                    d = d + Math.Abs(x[j] - xmin);
                }

                int n = 1;
                if (d < 8) // 2-bit 
                {
                    n = 2;
                }
                else if (d < 16) // 3-bit 
                {
                    n = 3;

                }
                else if (d < 32) // 4-bit 
                {
                    n = 4;

                }
                else // 5-bit 
                {
                    n = 5;

                }
                for (int j = 0; j < 9; j++)
                {
                    newImageData[j] = z((byte) y[j], (byte) x[j], n);
                }
            }
            return newImageData;
        }
    }
}
