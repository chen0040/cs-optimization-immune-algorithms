using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.BinaryAlgorithms
{
    /// <summary>
    /// Clonal Selection Algorithm
    /// </summary>
    public class ClonAlg : MultiTrajectoryBinarySolver
    {
        protected int mPopSize = 100;
        protected double mCloneFactor = 0.1;
        protected int mDimension;
        protected int mNumRandomSolutionInserted = 2;

        public delegate int[] CreateSolutionMethod(int dimenson, object constraints);
        protected CreateSolutionMethod mSolutionGenerator;

        public ClonAlg(int pop_size, int dimension, CreateSolutionMethod solution_generator)
        {
            mDimension = dimension;
            mPopSize = pop_size;
            mSolutionGenerator = solution_generator;
            if (mSolutionGenerator == null)
            {
                throw new NullReferenceException();
            }
        }

        public int RandomInsertedSolutionNum
        {
            get { return mNumRandomSolutionInserted; }
            set { mNumRandomSolutionInserted = value; }
        }

        public double CloneFactor
        {
            get { return mCloneFactor; }
            set { mCloneFactor = value; }
        }

        public void CalcAffinity(BinarySolution[] population, double[] population_affinity, BinarySolution best_solution, BinarySolution worst_solution)
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

        protected List<BinarySolution> CloneAndHypermutate(BinarySolution[] population, double[] population_affinity, double clone_factor)
        {
            int num_clones = (int)System.Math.Floor(mPopSize * mCloneFactor);
            List<BinarySolution> clones = new List<BinarySolution>();

            for (int i = 0; i < population.Length; ++i)
            {
                double mutation_rate = CalcMutationRate(population_affinity[i]);
                for (int j = 0; j < num_clones; ++j)
                {
                    BinarySolution clone = population[i].Clone() as BinarySolution;
                    PointMutate(clone, mutation_rate);
                    clones.Add(clone);
                }
            }

            return clones;
        }

        protected void PointMutate(BinarySolution solution, double mutation_rate)
        {
            for (int i = 0; i < solution.Length; ++i)
            {
                if (RandomEngine.NextDouble() < mutation_rate)
                {
                    solution.FlipBit(i);
                }
            }
        }

        public override BinarySolution Minimize(CostEvaluationMethod evaluate, TerminationEvaluationMethod should_terminate, object constraints = null)
        {
            double? improvement = null;
            int iteration = 0;

            BinarySolution[] population = new BinarySolution[mPopSize];
            double[] population_affinity = new double[mPopSize];

            double min_fx_0 = double.MaxValue;
            int[] best_x_0 = null;
            for (int i = 0; i < mPopSize; ++i)
            {
                int[] x_0 = mSolutionGenerator(mDimension, constraints);
                double fx_0 = evaluate(x_0, constraints);
                BinarySolution solution_0 = new BinarySolution(x_0, fx_0);
                population[i] = solution_0;

                if (fx_0 < min_fx_0)
                {
                    min_fx_0 = fx_0;
                    best_x_0 = x_0;
                }
            }

            BinarySolution best_solution = new BinarySolution(best_x_0, min_fx_0);

            while (!should_terminate(improvement, iteration))
            {
                //clone and hypermutate
                population = population.OrderBy(s => s.Cost).ToArray();
                CalcAffinity(population, population_affinity, population[0], population[mPopSize-1]);
                List<BinarySolution> clones = CloneAndHypermutate(population, population_affinity, mCloneFactor);

                foreach (BinarySolution clone in clones)
                {
                    clone.Cost = evaluate(clone.Values, constraints);
                }

                foreach (BinarySolution solution in population)
                {
                    clones.Add(solution);
                }

                //insert random solutions
                for (int i = 0; i < mNumRandomSolutionInserted; ++i)
                {
                    int[] x = mSolutionGenerator(mDimension, constraints);
                    double fx = evaluate(x, constraints);
                    clones.Add(new BinarySolution(x, fx));
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
