using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.ContinuousAlgorithms
{
    public class NegativeSelectionAlgorithm : IClassifier<MLDataPoint>
    {
        protected int mMaxDetectorNum = 300;
        protected double mMinDistance = 0.05;

        protected string mSelfLabel = "S"; // the label for the self data set
        protected string mNonSelfLabel = "N";  // the label for the non-self data set

        protected List<MLDataPoint> mDetectors;

        public delegate List<MLDataPoint> CreateSelfDataMethod(IEnumerable<MLDataPoint> domain);
        protected CreateSelfDataMethod mSelfDataGenerator;

        public delegate MLDataPoint CreateDetectorMethod();
        protected CreateDetectorMethod mDetectorGenerator;

        public NegativeSelectionAlgorithm(CreateDetectorMethod generator, string self_label, string non_self_label, CreateSelfDataMethod self_data_generator = null)
        {
            mDetectorGenerator = generator;
            mSelfLabel = self_label;
            mNonSelfLabel = non_self_label;
        }

        public virtual List<MLDataPoint> GenerateSelfDataSet(IEnumerable<MLDataPoint> domain)
        {
            if (mSelfDataGenerator != null)
            {
                return mSelfDataGenerator(domain);
            }
            else
            {
                List<MLDataPoint> self_dataset = new List<MLDataPoint>();
                foreach (MLDataPoint rec in domain)
                {
                    if (rec.Label == mSelfLabel)
                    {
                        self_dataset.Add(rec);
                    }
                }
                return self_dataset;
            }
        }

        

        public virtual void Train(IEnumerable<MLDataPoint> domain)
        {
            mDetectors = new List<MLDataPoint>();
            IEnumerable<MLDataPoint> self_data = GenerateSelfDataSet(domain);
            do
            {
                MLDataPoint detector = mDetectorGenerator();
                if (Match(detector, self_data, mMinDistance))
                {
                    mDetectors.Add(detector);
                }

            } while (mDetectors.Count < mMaxDetectorNum);
        }

        public bool Match(MLDataPoint detector, IEnumerable<MLDataPoint> domain, double min_distance)
        {
            foreach (MLDataPoint rec in domain)
            {
                double distance = rec.FindDistance2(detector);
                if (distance < min_distance)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual string Predict(MLDataPoint pattern)
        {
            if (Match(pattern, mDetectors, mMinDistance))
            {
                return mSelfLabel;
            }
            return mNonSelfLabel;
        }
    }
}
