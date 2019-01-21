using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class Runway : TargetNode
    {
        private string _number;

        private double _displacement;
        public double Displacement
        {
            get { return _displacement; }
            set
            {
                _displacement = value;
                update();
            }
        }
        public double DisplacedLatitude;
        public double DisplacedLongitude;
        public double Length;

        public TaxiNode DisplacedNode;

        public List<RunwayTakeOffSpot> TakeOffSpots;

        private Runway _oppositeEnd = null;
        public Runway OppositeEnd
        {
            get { return _oppositeEnd;  }
            set
            {
                _oppositeEnd = value;
                update();
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

        private void update()
        {
            if (OppositeEnd != null)
            {
                Bearing = VortexMath.BearingRadians(Latitude, Longitude, _oppositeEnd.Latitude, _oppositeEnd.Longitude);
                VortexMath.PointFrom(Latitude, Longitude, Bearing, Displacement, ref DisplacedLatitude, ref DisplacedLongitude);                
            }
        }

        public override string ToString()
        {
            return Number;
        }
    }
}
