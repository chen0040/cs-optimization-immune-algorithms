using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA.AlgorithmModels.Survival
{
    using System.Xml;
    using AIA.ComponentModels;
    using AIA.AlgorithmModels.Survival;
    using AIA;

    class SurvivalInstruction_Compete<P, S> : SurvivalInstruction<P, S>
        where S : ISolution
        where P : IPop
    {
        public SurvivalInstruction_Compete()
        {

        }

        public SurvivalInstruction_Compete(XmlElement xml_level1)
            : base(xml_level1)
        {

        }

        public override S Compete(P pop, S weak_program_in_current_pop, S child_program, Comparison<S> comparer)
        {
            if (comparer(child_program, weak_program_in_current_pop) > 0)
            {
                pop.Replace(weak_program_in_current_pop, child_program);
                return weak_program_in_current_pop;
            }
            return child_program;
        }

        public override SurvivalInstruction<P, S> Clone()
        {
            SurvivalInstruction_Compete<P, S> clone = new SurvivalInstruction_Compete<P, S>();
            return clone;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(">> Name: SurvivalInstruction_Compete");

            return sb.ToString();
        }
    }
}
