using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Managed.Tools
{
    class ParallelExtensions
    {
        private static IEnumerable<bool> IterateUntilFalse(Func<bool> condition)
        {
            while (condition()) yield return true;
        }
        public static void While(
            Func<bool> condition,
            Action<ParallelLoopState> body)
        {
            Parallel.ForEach(IterateUntilFalse(condition),
                (ignored, loopState) => body(loopState));
        }
    }
}
