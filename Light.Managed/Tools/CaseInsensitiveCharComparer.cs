using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Managed.Tools
{
    public class CaseInsensitiveCharComparer : IEqualityComparer<char>
    {
        public bool Equals(char x, char y)
        {
            return Char.ToUpper(x) == Char.ToUpper(y);
        }

        public int GetHashCode(char obj)
        {
            return Char.ToUpper(obj);
        }
    }
}
