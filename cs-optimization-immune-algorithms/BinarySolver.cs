using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA
{
    public abstract class BinarySolver : BaseSolver<int>
    {
        public BinarySolver()
        {

        }

        public delegate double CostEvaluationMethod(int[] x, object constraints);


    }
}
