using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.Output
{
    public class TxtWriter : RouteWriter
    {
        public TxtWriter(string path, string allSizes, int cargo, int military, string designator, string parkingCenter)
            : base(path + ".txt")
        {
            WriteLine($"STARTAIRCRAFTTYPE\n{allSizes}\nENDAIRCRAFTTYPE\n");

            if (cargo != -1)
                WriteLine($"STARTCARGO\n{cargo}\nENDCARGO\n");
            if (military != -1)
                WriteLine($"STARTMILITARY\n{military}\nENDMILITARY\n");
            if (!string.IsNullOrEmpty(designator))
                WriteLine($"STARTRUNWAY\n{designator}\nENDRUNWAY\n");
            if (!string.IsNullOrEmpty(parkingCenter))
                WriteLine($"START_PARKING_CENTER\n{parkingCenter}\nEND_PARKING_CENTER\n");
    
            WriteLine("STARTSTEERPOINTS");
        }

        public override void Write(SteerPoint steerPoint)
        {
            steerPoint.Write(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WriteLine("ENDSTEERPOINTS");
            }
            base.Dispose(disposing);
        }
    }
}
