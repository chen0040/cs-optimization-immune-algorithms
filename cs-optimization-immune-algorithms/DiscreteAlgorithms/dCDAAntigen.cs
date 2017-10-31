using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.DiscreteAlgorithms
{
    public class dCDAAntigen
    {
        protected Dictionary<string, int> mInputs = new Dictionary<string, int>();

        public int InputCount
        {
            get { return mInputs.Count; }
        }

        public bool IsEmpty
        {
            get { return mInputs.Count == 0; }
        }

        public int this[string feature_name, string feature_value]
        {
            get { return mInputs[string.Format("{0}={1}", feature_name, feature_value)]; }
            set { mInputs[string.Format("{0}={1}", feature_name, feature_value)] = value; }
        }

        public bool HasInput(string feature_name, string feature_value)
        {
            return mInputs.ContainsKey(string.Format("{0}={1}", feature_name, feature_value));
        }

        public void Reset()
        {
            mInputs.Clear();
        }
    }
}
