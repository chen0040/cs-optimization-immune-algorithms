using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.BinaryAlgorithms
{
    /// <summary>
    /// Opt-aiNet which implements the Optimization Artificial Immune Network
    /// 
    /// Application Domain:
    /// aiNet is designed for unsupervised clustering, where as the opt-aiNet extension 
    /// was designed for pattern recognition and optimization, specificially multi-modal 
    /// function optimization.
    /// 
    /// 
    /// </summary>
    public class OptAINet : MultiTrajectoryBinarySolver
    {
        protected int mInitPopSize = 20;
        protected int mNumClones = 10;
        protected double mBeta = 100.0;
        protected int mDimension;
        protected int mNumRandomSolutionInserted = 2;
        protected double mAffThreshold;

        public delegate int[] CreateSolutionMethod(int dimension, object constraints);
        protected CreateSolutionMethod mSolutionGenerator;

        public OptAINet(int pop_size, int dimension, double value_upper_bound, double value_lower_bound, CreateSolutionMethod solution_generator)
        {
            mDimension = dimension;
            mInitPopSize = pop_size;
            mAffThreshold = (value_upper_bound - value_lower_bound) * 0.05;

            mSolutionGenerator = solution_generator;
            if (mSolutionGenerator == null)
            {
                throw new NullReferenceException();
            }
        }

        public OptAINet(int pop_size, int dimension, double aff_threshold, CreateSolutionMethod solution_generator)
        {
            mDimension = dimension;
            mInitPopSize = pop_size;
            mAffThreshold = aff_threshold;

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

        public int NumClones
        {
            get { return mNumClones; }
            set { mNumClones = value; }
        }

        public List<double> CalcNormalizedCost(List<BinarySolution> population, BinarySolution best_solution, BinarySolution worst_solution)
        {
            List<double> normalized_costs = new List<double>();

            double range = worst_solution.Cost - best_solution.Cost;

            if (range == 0)
            {
                for (int i = 0; i < population.Count; ++i)
                {
                    double normalized_cost = 1.0;
                    normalized_costs.Add(normalized_cost);
                }
            }
            else
            {
                for (int i = 0; i < population.Count; ++i)
                {
                    double normalized_cost = 1.0 - population[i].Cost / range;
                    normalized_costs.Add(normalized_cost);
                }
            }

            return normalized_costs;
        }

        protected double CalcMutationRate(double beta, double normalized_cost)
        {
            return (1.0 / beta) * System.Math.Exp( - normalized_cost);
        }

        protected List<BinarySolution> CreateClones(BinarySolution solution, double beta, double normalized_cost, int num_clones)
        {
            List<BinarySolution> clones = new List<BinarySolution>();

            double mutation_rate = CalcMutationRate(beta, normalized_cost);
            for (int j = 0; j < num_clones; ++j)
            {
                BinarySolution clone = solution.Clone() as BinarySolution;
                PointMutate(clone, mutation_rate);
                clones.Add(clone);
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

        public virtual double CalcDistance(BinarySolution s1, BinarySolution s2)
        {
            double distance = 0;
            for (int i = 0; i < s1.Length; ++i)
            {
                distance += (s1[i] != s2[i] ? 1 : 0);
            }
            return distance;
        }

        public virtual List<BinarySolution> GetNeighbors(IEnumerable<BinarySolution> population, BinarySolution cell, double aff_threshold)
        {
            List<BinarySolution> neighbors = new List<BinarySolution>();
            double distance;
            foreach (BinarySolution p in population)
            {
                distance = CalcDistance(p, cell);
                if (distance < aff_threshold)
                {
                    neighbors.Add(p);
                }
            }

            return neighbors;
        }

        public virtual List<BinarySolution> AffinitySuppress(IEnumerable<BinarySolution> population, double aff_threshold)
        {
            List<BinarySolution> pop = new List<BinarySolution>();
            foreach (BinarySolution p in population)
            {
                List<BinarySolution> neighbors = GetNeighbors(population, p, aff_threshold);
                if (neighbors.Count == 0)
                {
                    pop.Add(p);
                }
                else
                {
                    neighbors = neighbors.OrderBy(s => s.Cost).ToList();
                    if (neighbors[0] == p)
                    {
                        pop.Add(p);
                    }
                }
            }

            return pop;
        }

        public override BinarySolution Minimize(CostEvaluationMethod evaluate, TerminationEvaluationMethod should_terminate, object constraints = null)
        {
            double? improvement = null;
            int iteration = 0;

            List<BinarySolution> population = new List<BinarySolution>();
            
            double min_fx_0 = double.MaxValue;
            int[] best_x_0 = null;
            for (int i = 0; i < mInitPopSize; ++i)
            {
                int[] x_0 = mSolutionGenerator(mDimension, constraints);
                double fx_0 = evaluate(x_0, constraints);
                BinarySolution solution_0 = new BinarySolution(x_0, fx_0);
                population.Add(solution_0);

                if (fx_0 < min_fx_0)
                {
                    min_fx_0 = fx_0;
                    best_x_0 = x_0;
                }
            }

            BinarySolution best_solution = new BinarySolution(best_x_0, min_fx_0);

            population = population.OrderBy(s => s.Cost).ToList();

            while (!should_terminate(improvement, iteration))
            {
                double avg_cost = population.Average(s => s.Cost);

                List<double> normalized_costs = CalcNormalizedCost(population, population[0], population[population.Count-1]);
                List<BinarySolution> progeny = new List<BinarySolution>();
                do{
                    for(int i=0; i < population.Count; ++i)
                    {
                        List<BinarySolution> clones = CreateClones(population[i], mBeta, normalized_costs[i], mNumClones);
                        foreach (BinarySolution clone in clones)
                        {
                            clone.Cost = evaluate(clone.Values, constraints);
                        }
                        progeny.AddRange(clones);
                    }
                }while(progeny.Average(s=>s.Cost) > avg_cost);

                population = AffinitySuppress(progeny, mAffThreshold);

                //insert random solutions
                for (int i = 0; i < mNumRandomSolutionInserted; ++i)
                {
                    int[] x = mSolutionGenerator(mDimension, constraints);
                    double fx = evaluate(x, constraints);
                    population.Add(new BinarySolution(x, fx));
                }

                population = population.OrderBy(s => s.Cost).ToList();

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
