using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.DiscreteAlgorithms
{
    /// <summary>
    /// dCDA implements the deterministic Cell Dendritic Algorithm
    /// Usage:
    /// 
    /// </summary>
    public class dCDA  : IClassifier<DDataRecord>
    {
        protected int mCellNum;
        protected double mP_normal = 0.95;
        protected double mP_anormal = 0.7;
        protected string mNormalClassLabel = "Normal";
        protected string mAnormalClassLabel = "Anormal";
        protected IEnumerable<DDataRecord> mDomain;
        protected double[] mThreshold;
        protected int mNumIterations = 100;

        protected List<dCDACell> mMigratedCells;

        public double P_normal
        {
            get { return mP_normal; }
            set { mP_normal = value; }
        }

        public double P_anormal
        {
            get { return mP_anormal; }
            set { mP_anormal = value; }
        }

        public int NumIterations
        {
            get { return mNumIterations; }
            set { mNumIterations = value; }
        }

        public dCDA(int cell_num, string normal_class_label, string anormal_class_label, double[] threshold = null)
        {
            mCellNum = cell_num;
            mNormalClassLabel = normal_class_label;
            mAnormalClassLabel = anormal_class_label;

            mThreshold = threshold;
            if (mThreshold == null)
            {
                mThreshold = new double[] { 5, 15 };
            }
        }

        public void Train(IEnumerable<DDataRecord> domain)
        {
            mDomain = domain;
            
            List<dCDACell> immature_cells = new List<dCDACell>();

            for (int i = 0; i < mCellNum; ++i)
            {
                dCDACell cell = InitializeCell(mThreshold);
                immature_cells.Add(cell);
            }

            mMigratedCells = new List<dCDACell>();

            for (int iteration = 0; iteration < mNumIterations; ++iteration)
            {
                DDataRecord pattern = GeneratePattern(mDomain, mP_anormal, mP_normal);
                List<dCDACell> migrants = ExposeAllCells(immature_cells, pattern, mThreshold);
                foreach (dCDACell cell in migrants)
                {
                    immature_cells.Remove(cell);
                    immature_cells.Add(InitializeCell(mThreshold));
                    mMigratedCells.Add(cell);
                }
            }
        }

        public string Predict(DDataRecord pattern)
        {
            string[] feature_names = pattern.FindFeatures();
            int num_cells = 0;
            int num_antigens = 0;
            int migrated_cell_count = mMigratedCells.Count;
            foreach (string feature_name in feature_names)
            {
                for (int i = 0; i < migrated_cell_count; ++i)
                {
                    dCDACell cell = mMigratedCells[i];
                    if (cell.ClassLabel == mAnormalClassLabel && cell.Antigen.HasInput(feature_name, pattern[feature_name]))
                    {
                        num_cells++;
                        num_antigens += cell.Antigen[feature_name, pattern[feature_name]];
                    }
                }
            }

            double mcav = (double)num_cells / num_antigens;
            return mcav > 0.5 ? mAnormalClassLabel : mNormalClassLabel;
        }

        protected DDataRecord GeneratePattern(IEnumerable<DDataRecord> domain, double p_anomal, double p_normal, double prob_create_anorm=0.5)
        {
            double r = RandomEngine.NextDouble();
            if (r < prob_create_anorm)
            {
                return ConstructPattern(mAnormalClassLabel, domain, 1 - p_normal, p_anomal); //create anomal pattern
            }
            else
            {
                return ConstructPattern(mNormalClassLabel, domain, p_normal, 1 - p_anomal); //create normal pattern
            }
        }

        public DDataRecord ConstructPattern(string class_label, IEnumerable<DDataRecord> domain, double p_safe, double p_danger)
        {
            List<DDataRecord> set = new List<DDataRecord>();
            foreach (DDataRecord rec in domain)
            {
                if (rec.Label == class_label)
                {
                    set.Add(rec);
                }
            }

            DDataRecord pattern = set[RandomEngine.NextInt(set.Count)].Clone();

            dCDAPatternInfo pattern_info=new dCDAPatternInfo();
            pattern_info.Safe = p_safe * RandomEngine.NextDouble() * 100;
            pattern_info.Danger = p_danger * RandomEngine.NextDouble() * 100;

            pattern.Tag = pattern_info;

            return pattern;
        }

        public void ExposeCell(dCDACell cell, double cms, double k, DDataRecord pattern, double[] threshold)
        {
            cell.cms += cms;
            cell.k += k;
            cell.LifeSpan -= cms;

            StoreAntigen(cell.Antigen, pattern);

            if (cell.LifeSpan <= 0)
            {
                InitializeCell(cell, threshold);
            }
        }

        protected void StoreAntigen(dCDAAntigen antigen, DDataRecord pattern)
        {
            string[] feature_names = pattern.FindFeatures();
            foreach (string feature_name in feature_names)
            {
                if (antigen.HasInput(feature_name, pattern[feature_name]))
                {
                    antigen[feature_name, pattern[feature_name]]++;
                }
                else
                {
                    antigen[feature_name, pattern[feature_name]] = 1;
                }
            }
        }

        public List<dCDACell> ExposeAllCells(List<dCDACell> cells, DDataRecord pattern, double[] threshold)
        {
            List<dCDACell> migrate = new List<dCDACell>();
            dCDAPatternInfo pattern_info=pattern.Tag as dCDAPatternInfo;
            double cms = pattern_info.Danger + pattern_info.Safe;
            double k = pattern_info.Danger - pattern_info.Safe * 2;

            for (int i = 0; i < cells.Count; ++i)
            {
                dCDACell cell = cells[i];
                ExposeCell(cell, cms, k, pattern, threshold);
                if (CanCellMigrate(cell))
                {
                    migrate.Add(cell);
                    cell.ClassLabel = cell.k > 0 ? mAnormalClassLabel : mNormalClassLabel; //mature cells (represent "anormal") if cell.k > 0; semimature cells if cell.k < 0
                }
            }

            return migrate;
        }

        protected bool CanCellMigrate(dCDACell cell_info)
        {
            return (cell_info.cms >= cell_info.MigrationThreshold && !cell_info.Antigen.IsEmpty);
        }

        protected dCDACell InitializeCell(double[] threshold)
        {
            dCDACell cell = new dCDACell();
            InitializeCell(cell, threshold);

            return cell;
        }

        protected void InitializeCell(dCDACell cell, double[] threshold)
        {
            cell.cms = 0.0;
            cell.k = 0.0;
            cell.LifeSpan = 1000.0;
            cell.MigrationThreshold = threshold[0] + RandomEngine.NextDouble() * (threshold[1] - threshold[0]);
            cell.Antigen.Reset();
        }

        

    }
}
