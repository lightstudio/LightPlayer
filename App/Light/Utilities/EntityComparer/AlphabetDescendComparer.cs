using System;
using System.Collections.Generic;

namespace Light.Utilities.EntityComparer
{
    public class AlphabetDescendComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return -string.Compare(x, y, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
