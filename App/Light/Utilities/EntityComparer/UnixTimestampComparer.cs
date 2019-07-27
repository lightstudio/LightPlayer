using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Utilities.EntityComparer
{
    public class UnixTimestampComparer : IComparer<string>
    {
        bool _ascending;

        public UnixTimestampComparer(bool ascending)
        {
            _ascending = ascending;
        }

        public int Compare(string x, string y)
        {
            long xTime = 0, yTime = 0;
            long.TryParse(x, out xTime);
            long.TryParse(y, out yTime);
            return _ascending ? xTime.CompareTo(yTime) : yTime.CompareTo(xTime);
        }
    }
}
