using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibOrbisPkg.Kimie
{
    public static class Count
    {
        private static int count = new int();
        private static int MaxCount = new int();

        public static int Count1 { get => count; set => count = value; }
        public static int MaxCount1 { get => MaxCount; set => MaxCount = value; }
    }
}
