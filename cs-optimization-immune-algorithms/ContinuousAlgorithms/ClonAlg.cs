using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.ContinuousAlgorithms
{
    /// <summary>
    /// Clonal Selection Algorithm
    /// </summary>
    public class ClonAlg : MultiTrajectoryContinuousSolver
    {
        protected int mPopSize = 100;
        protected double mCloneFactor = 0.1;
        protected int mDimension;
        protected int mNumRandomSolutionInserted = 2;
        protected double mMutationStdDev = 3;

        public delegate double[] CreateSolutionMethod(object constraints);
        protected CreateSolutionMethod mSolutionGenerator;

        public ClonAlg(int pop_size, int dimension)
        {
            mDimension = dimension;
            mPopSize = pop_size;
        }

        public int RandomInsertedSolutionNum
        {
            get { return mNumRandomSolutionInserted; }
            set { mNumRandomSolutionInserted = value; }
        }

        public double MutationStdDev
        {
            get { return mMutationStdDev; }
            set { mMutationStdDev = value; }
        }

        public double CloneFactor
        {
            get { return mCloneFactor; }
            set { mCloneFactor = value; }
        }

        public void CalcAffinity(ContinuousSolution[] population, double[] population_affinity, ContinuousSolution best_solution, ContinuousSolution worst_solution)
        {
            double range = worst_solution.Cost - best_solution.Cost;

            if (range == 0)
            {
                for (int i = 0; i < population.Length; ++i)
                {
                    population_affinity[i] = 1.0;
                }
            }
            else
            {
                for (int i = 0; i < population.Length; ++i)
                {
                    population_affinity[i] = 1.0 - population[i].Cost / range;
                }
            }
        }

        protected double CalcMutationRate(double affinity, double mutate_factor=-2.5)
        {
            return System.Math.Exp(mutate_factor * affinity);
        }

        protected List<ContinuousSolution> CloneAndHypermutate(ContinuousSolution[] population, double[] population_affinity, double clone_factor)
        {
            int num_clones = (int)System.Math.Floor(mPopSize * mCloneFactor);
            List<ContinuousSolution> clones = new List<ContinuousSolution>();

            for (int i = 0; i < population.Length; ++i)
            {
                double mutation_rate = CalcMutationRate(population_affinity[i]);
                for (int j = 0; j < num_clones; ++j)
                {
                    ContinuousSolution clone = population[i].Clone() as ContinuousSolution;
                    NormalMutate(clone, mutation_rate);
                    clones.Add(clone);
                }
            }

            return clones;
        }

        protected void NormalMutate(ContinuousSolution solution, double mutation_rate)
        {
            for (int i = 0; i < solution.Length; ++i)
            {
                if (RandomEngine.NextDouble() < mutation_rate)
                {
                    solution.MutateNormal(i, mMutationStdDev);
                }
            }
        }

        public override ContinuousSolution Minimize(CostEvaluationMethod evaluate, GradientEvaluationMethod calc_gradient, TerminationEvaluationMethod should_terminate, object constraints = null)
        {
            double? improvement = null;
            int iteration = 0;

            ContinuousSolution[] population = new ContinuousSolution[mPopSize];
            double[] population_affinity = new double[mPopSize];

            double min_fx_0 = double.MaxValue;
            double[] best_x_0 = null;
            for (int i = 0; i < mPopSize; ++i)
            {
                double[] x_0 = mSolutionGenerator(constraints);
                double fx_0 = evaluate(x_0, mLowerBounds, mUpperBounds, constraints);
                ContinuousSolution solution_0 = new ContinuousSolution(x_0, fx_0);
                population[i] = solution_0;

                if (fx_0 < min_fx_0)
                {
                    min_fx_0 = fx_0;
                    best_x_0 = x_0;
                }
            }

            ContinuousSolution best_solution = new ContinuousSolution(best_x_0, min_fx_0);

            while (!should_terminate(improvement, iteration))
            {
                //clone and hypermutate
                population = population.OrderBy(s => s.Cost).ToArray();
                CalcAffinity(population, population_affinity, population[0], population[mPopSize-1]);
                List<ContinuousSolution> clones = CloneAndHypermutate(population, population_affinity, mCloneFactor);

                foreach (ContinuousSolution clone in clones)
                {
                    clone.Cost = evaluate(clone.Values, mLowerBounds, mUpperBounds, constraints);
                }

                foreach (ContinuousSolution solution in population)
                {
                    clones.Add(solution);
                }

                //insert random solutions
                for (int i = 0; i < mNumRandomSolutionInserted; ++i)
                {
                    double[] x = mSolutionGenerator(constraints);
                    double fx = evaluate(x, mLowerBounds, mUpperBounds, constraints);
                    clones.Add(new ContinuousSolution(x, fx));
                }

                clones = clones.OrderBy(s => s.Cost).ToList();

                for (int i = 0; i < mPopSize; ++i)
                {
                    population[i] = clones[i];
                }

                if (best_solution.TryUpdateSolution(population[0].Values, population[0].Cost, out improvement))
                {
                    OnSolutionUpdated(best_solution, iteration);
                }

                OnStepped(best_solution, iteration);
                iteration++;
            }

            return best_solution;
        }
    }
}
