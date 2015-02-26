using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteganosaurusWPF
{
    class FileTemp
    {
        public string Name;
        public byte[] Data;

        public FileTemp(string name)
        {
            this.Name = name;
        }
    }
}
