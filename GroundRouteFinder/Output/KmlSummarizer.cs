using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.AptDat;

namespace GroundRouteFinder.Output
{
    public class KmlSummarizer
    {
        public static void Write(Airport airport)
        {
            using (StreamWriter sw = File.CreateText(Path.Combine(Settings.DataFolder, airport.ICAO + ".kml")))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sw.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                sw.WriteLine(" <Document>");
                sw.WriteLine("  <Style id=\"Runway\"><LineStyle><color>ff0000ff</color><width>3</width><gx:labelVisibility>0</gx:labelVisibility></LineStyle></Style>");
                sw.WriteLine("  <Style id=\"RunwayIcon\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pal2/icon48.png</href></Icon></IconStyle></Style>");
                sw.WriteLine("  <Style id=\"NearestRunway\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/red-diamond-lv.png</href></Icon></IconStyle></Style>");
                sw.WriteLine("  <Style id=\"TaxiNode\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pal4/icon57.png</href></Icon></IconStyle></Style>");
                WriteNodes(sw, airport.TaxiNodes);
                WriteRunways(sw, airport.Runways);
                sw.WriteLine(" </Document>");
                sw.WriteLine("</kml>");
            }
        }

        private static void WriteNodes(StreamWriter sw, IEnumerable<TaxiNode> nodes)
        {
            foreach (TaxiNode node in nodes)
            {
                sw.WriteLine($"  <Placemark><styleUrl>#TaxiNode</styleUrl><name>{node.Id}</name><Point><coordinates>{node.Longitude * VortexMath.Rad2Deg},{node.Latitude * VortexMath.Rad2Deg},0</coordinates></Point></Placemark>");
            }
        }

        private static void WriteRunways(StreamWriter sw, IEnumerable<Runway> runways)
        {
            foreach (Runway runway in runways)
            {
                sw.WriteLine($"  <Placemark><styleUrl>#RunwayIcon</styleUrl><name>{runway.Designator}</name><Point><coordinates>{runway.DisplacedLongitude * VortexMath.Rad2Deg},{runway.DisplacedLatitude * VortexMath.Rad2Deg},0</coordinates></Point></Placemark>");
                sw.WriteLine($"  <Placemark><styleUrl>#NearestRunway</styleUrl><name>{runway.Designator}</name><Point><coordinates>{runway.Longitude * VortexMath.Rad2Deg},{runway.Latitude * VortexMath.Rad2Deg},0</coordinates></Point></Placemark>");

                sw.WriteLine("  <Placemark>\n");
                sw.WriteLine($"   <name>{runway.Designator}</name>\n   <styleUrl>#Runway</styleUrl>\n");
                sw.WriteLine("   <LineString>\n    <coordinates>\n");
                foreach (TaxiNode node in runway.RunwayNodes)
                {
                    sw.WriteLine($"     {node.Longitude * VortexMath.Rad2Deg}, {node.Latitude * VortexMath.Rad2Deg}, 0.0\n");
                }
                sw.WriteLine("    </coordinates>\n   </LineString>\n  </Placemark>\n");
            }
        }
    }
}
