using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class Runway : TargetNode
    {
        private string _number;

        public Runway OppositeEnd;

        public string Number
        {
            get { return _number; }

            set
            {
                _number = value;
                FileNameSafeName = value;
            }
        }

        public Runway() 
            : base()
        {
            OppositeEnd = null;
        }

        public override string ToString()
        {
            return Number;
        }
    }
}
