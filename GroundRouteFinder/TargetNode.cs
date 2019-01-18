using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class TargetNode
    {
        private static char[] _invalidChars = Path.GetInvalidFileNameChars();

        public double Latitude;
        public double Longitude;

        public TaxiNode NearestVertex;

        private string _fileNameSafeName;
        public string FileNameSafeName
        {
            get { return _fileNameSafeName;  }
            protected set
            {
                _fileNameSafeName = new string(value.Select(c => _invalidChars.Contains(c) ? '_' : c).ToArray());
            }
        }

        public TargetNode()
        {
            NearestVertex = null;
        }
    }
}
