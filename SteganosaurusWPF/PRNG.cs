﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteganosaurusWPF
{
    class PRNG
    {
        public static List<int> GenerateSequence(int seed_, int max_)
        {
            // Manually input the Seed, or you can make it random like my code above.
            int seed = Convert.ToInt32(seed_);

            String display = "";
            int min = 1;
            // Max value is manually input, for how many number will be generated.
            // i need to plus by 1 for the max value because i state the min value is 1.
            int max = Convert.ToInt32(max_) + 1;

            Random rand = new Random(seed);

            int number;
            // this dictionary is for saving the number generated by random, if exist, 
            //do random again.
            Dictionary<int, int> num = new Dictionary<int, int>();

            List<int> sequence = new List<int>();
            for (int i = 1; i < max; i++)
            {
                number = rand.Next(min, max);
                if (num.ContainsKey(number))
                {
                    while (true)
                    {
                        number = rand.Next(min, max);
                        if (num.ContainsKey(number))
                        {
                            // if exist do nothing and then random again while true  
                        }
                        else
                        {
                            num.Add(number, 1);
                            break;
                        }

                    }
                }
                else
                {
                    num.Add(number, 1);
                }
                sequence.Add(number);
            }
            return sequence;
        }

        public static List<int> GenerateSequence(string str, int max_)
        {
            int seed = 0;
            for (int i = 0; i < str.Length; i++)
            {
                seed += (int)str[i];
            }
            return GenerateSequence(seed, max_);
        }
    }
}