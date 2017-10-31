using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.DiscreteAlgorithms
{
    public class dCDAPatternInfo
    {
        protected double mSafe;
        protected double mDanger;

        public double Safe
        {
            get { return mSafe; }
            set
            {
                mSafe = value;
            }
        }

        public double Danger
        {
            get { return mDanger; }
            set { mDanger = value; }
        }
    }
}
