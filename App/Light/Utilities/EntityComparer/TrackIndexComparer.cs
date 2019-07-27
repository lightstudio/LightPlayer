using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Utilities.EntityComparer
{
    class TrackIndexComparer : IComparer<string>
    {
        private static int Parse(string x)
        {
            int result;
            if (int.TryParse(x, out result))
            {
                return result;
            }
            else
            {
                return 0;
            }
        }
        public int Compare(string x, string y)
        {
            var xs = x.Split('.');
            var ys = y.Split('.');
            int xDiscNumber = Parse(xs[0]),
                xTrackNumber = Parse(xs[1]),
                yDiscNumber = Parse(ys[0]),
                yTrackNumber = Parse(ys[1]);
            var discNumberCompare = xDiscNumber.CompareTo(yDiscNumber);
            if (discNumberCompare == 0)
            {
                return xTrackNumber.CompareTo(yTrackNumber);
            }
            return discNumberCompare;
        }
    }
}
