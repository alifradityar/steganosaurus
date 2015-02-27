using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteganosaurusWPF
{
    public static class BitUtil
    {
        public static bool GetBit(this byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        public static byte SetBit(this byte b, int pos, bool value)
        {
            if (value)
            {
                //left-shift 1, then bitwise OR
                b = (byte)(b | (1 << pos));
            }
            else
            {
                //left-shift 1, then take complement, then bitwise AND
                b = (byte)(b & ~(1 << pos));
            }
            return b;
        }
    }
}
