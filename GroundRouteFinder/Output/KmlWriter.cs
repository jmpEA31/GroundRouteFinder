using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.Output
{
    public class KmlWriter : RouteWriter
    {
        private StringBuilder _coords = new StringBuilder();

        public KmlWriter(string path)
            : base(path + ".kml", Encoding.UTF8)
        {
            _coords = new StringBuilder();

            WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
            WriteLine(" <Document>");
            WriteLine("  <Style id=\"Parking\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/shapes/parking_lot.png</href></Icon></IconStyle></Style>");
            WriteLine("  <Style id=\"HoldShort\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/shapes/hospitals.png</href></Icon></IconStyle></Style>");
            WriteLine("  <Style id=\"Runway\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pal4/icon49.png</href></Icon></IconStyle></Style>");
            WriteLine("  <Style id=\"Pushback\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/grn-stars-lv.png</href></Icon></IconStyle></Style>");
            WriteLine("  <Style id=\"TaxiNode\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/grn-blank-lv.png</href></Icon></IconStyle></Style>");
            WriteLine("  <Style id=\"TaxiLine\"><LineStyle><color>ff0000ff</color><width>3</width></LineStyle></Style>");
        }

        public override void Write(SteerPoint steerPoint)
        {
            steerPoint.WriteKML(this);
            _coords.Append($"  {steerPoint.Longitude * VortexMath.Rad2Deg},{steerPoint.Latitude * VortexMath.Rad2Deg},0\n");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WriteLine($"  <Placemark><styleUrl>#TaxiLine</styleUrl><LineString>\n<coordinates>\n{_coords.ToString()}</coordinates>\n</LineString></Placemark>\n");
                WriteLine(" </Document>");
                WriteLine("</kml>");
            }
            base.Dispose(disposing);
        }

    }
}
