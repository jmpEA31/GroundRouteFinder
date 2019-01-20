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

        public double Displacement;
        public double DisplacedLatitude;
        public double DisplacedLongitude;
        public TaxiNode DisplacedNode;

        public List<RunwayTakeOffSpot> TakeOffSpots;

        private Runway _oppositeEnd = null;
        public Runway OppositeEnd
        {
            get { return _oppositeEnd;  }
            set
            {
                _oppositeEnd = value;
                if (value != null)
                {
                    Bearing = VortexMath.BearingRadians(Latitude, Longitude, _oppositeEnd.Latitude, _oppositeEnd.Longitude);
                    VortexMath.PointFrom(Latitude, Longitude, Bearing, Displacement, ref DisplacedLatitude, ref DisplacedLongitude);
                }
            }
        }
        public double Bearing;

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
            DisplacedNode = null;
            TakeOffSpots = new List<RunwayTakeOffSpot>();
        }

        public override string ToString()
        {
            return Number;
        }
    }
}
