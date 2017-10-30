using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA
{
    public abstract class MultiTrajectoryBinarySolver : BinarySolver
    {
        public abstract BinarySolution Minimize(CostEvaluationMethod evaluate, TerminationEvaluationMethod should_terminate, object constraints = null);
    }
}
