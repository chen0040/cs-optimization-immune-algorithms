using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.DiscreteAlgorithms
{
    public class dCDACell : ImmuneDiscreteSolution
    {
        protected double m_cms;
        protected double m_k;
        protected double m_lifespan;
        protected double m_migration_threshold;
        protected dCDAAntigen mAntigen = new dCDAAntigen();
        protected string mClassLabel;

        public string ClassLabel
        {
            get { return mClassLabel; }
            set { mClassLabel = value; }
        }

        public dCDACell()
            : base(null, double.MaxValue)
        {

        }

        public dCDAAntigen Antigen
        {
            get { return mAntigen; }
        }

        public double MigrationThreshold
        {
            get { return m_migration_threshold; }
            set { m_migration_threshold = value; }
        }

        public double cms
        {
            get { return m_cms; }
            set { m_cms = value; }
        }

        public double k
        {
            get { return m_k; }
            set { m_k = value; }
        }

        public double LifeSpan
        {
            get { return m_lifespan; }
            set { m_lifespan = value; }
        }
    }
}
