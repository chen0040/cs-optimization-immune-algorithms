using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.DiscreteAlgorithms
{
    public class ImmuneDiscreteSolution : BaseSolution<int>
    {
        public ImmuneDiscreteSolution(int[] values, double cost)
            : base(values, cost)
        {
        }

        public override BaseSolution<int> Clone()
        {
            ImmuneDiscreteSolution clone = new ImmuneDiscreteSolution(mValues, mCost);
            return clone;
        }

        
    }
}
