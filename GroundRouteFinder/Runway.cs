using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class Runway : TargetNode
    {
        public string Number;

        public Runway() : base()
        {
        }

        public override string ToString()
        {
            return Number;
        }
    }
}
