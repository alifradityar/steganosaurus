using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteganosaurusWPF
{
    class Viginere
    {
        public static byte[] Encrypt(byte[] message, string key)
        {
            byte[] cipher = new byte[message.Length];
            for (int i = 0; i < cipher.Length; i++)
            {
                int tmpT = 0;
                int tmpM = message[i] + byte.MinValue;
                int tmpK = key[i % key.Length];
                tmpT = (tmpM + tmpK) % (byte.MaxValue - byte.MinValue + 1);
                tmpT -= byte.MinValue;
                cipher[i] = (byte)tmpT;
            }
            return cipher;
        }

        public static byte[] Decrypt(byte[] message, string key)
        {
            byte[] cipher = new byte[message.Length];
            for (int i = 0; i < cipher.Length; i++)
            {
                int tmpT = 0;
                int tmpM = message[i] + byte.MinValue;
                int tmpK = key[i % key.Length];
                tmpT = ((byte.MaxValue - byte.MinValue + 1) + tmpM - tmpK) % (byte.MaxValue - byte.MinValue + 1);
                tmpT -= byte.MinValue;
                cipher[i] = (byte)tmpT;
            }
            return cipher;
        }
    }
}
