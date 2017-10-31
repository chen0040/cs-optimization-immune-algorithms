using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.ContinuousAlgorithms
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
    public class OptAINet : MultiTrajectoryContinuousSolver
    {
        protected int mInitPopSize = 20;
        protected int mNumClones = 10;
        protected double mBeta = 100.0;
        protected int mDimension;
        protected int mNumRandomSolutionInserted = 2;
        protected double mAffThreshold;

        public delegate double[] CreateSolutionMethod(object constraints);
        protected CreateSolutionMethod mSolutionGenerator;

        public OptAINet(int pop_size, int dimension, double value_upper_bound, double value_lower_bound)
        {
            mDimension = dimension;
            mInitPopSize = pop_size;
            mAffThreshold = (value_upper_bound - value_lower_bound) * 0.05;
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

        public List<double> CalcNormalizedCost(List<ContinuousSolution> population, ContinuousSolution best_solution, ContinuousSolution worst_solution)
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

        protected List<ContinuousSolution> CreateClones(ContinuousSolution solution, double beta, double normalized_cost, int num_clones)
        {
            List<ContinuousSolution> clones = new List<ContinuousSolution>();

            double alpha = CalcMutationRate(beta, normalized_cost);
            for (int j = 0; j < num_clones; ++j)
            {
                ContinuousSolution clone = solution.Clone() as ContinuousSolution;
                NormalMutate(clone, alpha);
                clones.Add(clone);
            }

            return clones;
        }

        protected void NormalMutate(ContinuousSolution solution, double alpha)
        {
            for (int i = 0; i < solution.Length; ++i)
            {
                if (RandomEngine.NextDouble() < alpha)
                {
                    solution.MutateNormal(i, alpha);
                }
            }
        }

        public virtual double CalcDistance(ContinuousSolution s1, ContinuousSolution s2)
        {
            double distance = 0;
            for (int i = 0; i < s1.Length; ++i)
            {
                distance += (s1[i] != s2[i] ? 1 : 0);
            }
            return distance;
        }

        public virtual List<ContinuousSolution> GetNeighbors(IEnumerable<ContinuousSolution> population, ContinuousSolution cell, double aff_threshold)
        {
            List<ContinuousSolution> neighbors = new List<ContinuousSolution>();
            double distance;
            foreach (ContinuousSolution p in population)
            {
                distance = CalcDistance(p, cell);
                if (distance < aff_threshold)
                {
                    neighbors.Add(p);
                }
            }

            return neighbors;
        }

        public virtual List<ContinuousSolution> AffinitySuppress(IEnumerable<ContinuousSolution> population, double aff_threshold)
        {
            List<ContinuousSolution> pop = new List<ContinuousSolution>();
            foreach (ContinuousSolution p in population)
            {
                List<ContinuousSolution> neighbors = GetNeighbors(population, p, aff_threshold);
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

        public override ContinuousSolution Minimize(CostEvaluationMethod evaluate, GradientEvaluationMethod calc_gradient, TerminationEvaluationMethod should_terminate, object constraints = null)
        {
            double? improvement = null;
            int iteration = 0;

            List<ContinuousSolution> population = new List<ContinuousSolution>();
            
            double min_fx_0 = double.MaxValue;
            double[] best_x_0 = null;
            for (int i = 0; i < mInitPopSize; ++i)
            {
                double[] x_0 = mSolutionGenerator(constraints);
                double fx_0 = evaluate(x_0, mLowerBounds, mUpperBounds, constraints);
                ContinuousSolution solution_0 = new ContinuousSolution(x_0, fx_0);
                population.Add(solution_0);

                if (fx_0 < min_fx_0)
                {
                    min_fx_0 = fx_0;
                    best_x_0 = x_0;
                }
            }

            ContinuousSolution best_solution = new ContinuousSolution(best_x_0, min_fx_0);

            population = population.OrderBy(s => s.Cost).ToList();

            while (!should_terminate(improvement, iteration))
            {
                double avg_cost = population.Average(s => s.Cost);

                List<double> normalized_costs = CalcNormalizedCost(population, population[0], population[population.Count-1]);
                List<ContinuousSolution> progeny = new List<ContinuousSolution>();
                do{
                    for(int i=0; i < population.Count; ++i)
                    {
                        List<ContinuousSolution> clones = CreateClones(population[i], mBeta, normalized_costs[i], mNumClones);
                        foreach (ContinuousSolution clone in clones)
                        {
                            clone.Cost = evaluate(clone.Values, mLowerBounds, mUpperBounds, constraints);
                        }
                        progeny.AddRange(clones);
                    }
                }while(progeny.Average(s=>s.Cost) > avg_cost);

                population = AffinitySuppress(progeny, mAffThreshold);

                //insert random solutions
                for (int i = 0; i < mNumRandomSolutionInserted; ++i)
                {
                    double[] x = mSolutionGenerator(constraints);
                    double fx = evaluate(x, mLowerBounds, mUpperBounds, constraints);
                    population.Add(new ContinuousSolution(x, fx));
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
