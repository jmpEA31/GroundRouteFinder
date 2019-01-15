using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class StartPoint : TargetNode
    {
        public string Type;
        public string Jets;
        public string Name;

        public StartPoint() 
            : base()
        {
            NearestVertex = null;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
