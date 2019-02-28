﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.LogSupport;

namespace GroundRouteFinder.AptDat
{
    public class Parking : LocationObject
    {
        private static char[] _invalidChars = Path.GetInvalidFileNameChars();

        private string _fileNameSafeName;
        public string FileNameSafeName
        {
            get { return _fileNameSafeName; }
            protected set
            {
                _fileNameSafeName = new string(value.Select(c => _invalidChars.Contains(c) ? '_' : c).ToArray());
            }
        }

        public TaxiNode NearestNode;

        public StartUpLocationType LocationType;
        public OperationType Operation;
        public IEnumerable<string> Operators;

        public XPlaneAircraftCategory MaxSize;
        public IEnumerable<XPlaneAircraftType> XpTypes;
        public IEnumerable<WorldTrafficAircraftType> PossibleWtTypes = null;

        private string _name;
        public string Name
        {
            get { return _name; }

            set
            {
                _name = value;
                FileNameSafeName = value;
            }
        }

        public double Bearing;

        public double PushBackLatitude;
        public double PushBackLongitude;
        public TaxiNode AlternateAfterPushBack;

        protected Airport _airport;

        public Parking(Airport airport) 
            : base()
        {
            _airport = airport;
            AlternateAfterPushBack = null;
            PushBackLatitude = 0;
            PushBackLongitude = 0;
        }

        public void DetermineTaxiOutLocation(IEnumerable<TaxiNode> taxiNodes)
        {
            double shortestDistance = double.MaxValue;
            double bestPushBackLatitude = 0;
            double bestPushBackLongitude = 0;
            TaxiNode firstAfterPush = null;
            TaxiNode alternateAfterPush = null;
            TaxiNode fallback = null;

            // For gates use the indicated bearings (push back), for others add 180 degrees for straight out
            // Then convert to -180...180 range
            double adjustedBearing = (LocationType == StartUpLocationType.Gate) ? Bearing : (Bearing + Math.PI);
            if (adjustedBearing > Math.PI)
                adjustedBearing -= (VortexMath.PI2);

            // Compute the distance (arbitrary units) from each taxi node to the start location
            foreach (TaxiNode node in taxiNodes)
            {
                node.TemporaryDistance = VortexMath.DistanceKM(node, this);
            }

            // Select the 25 nearest, then from those select only the ones that are in the 180 degree arc of the direction
            // we intend to move in from the startpoint
            // todo: make both 25 and 180 parameters
            IEnumerable<TaxiNode> selectedNodes = taxiNodes.OrderBy(v => v.TemporaryDistance).Take(25);
            fallback = selectedNodes.First();

            if (fallback.TemporaryDistance < 0.0025)
            {
                // There is a atc taxi node really close to the parking, try to build pushback path from there
                if (fallback.IncomingEdges.Count == 1)
                {
                    TaxiEdge theEdge = fallback.IncomingEdges.FirstOrDefault();
                    if (theEdge != null)
                    {
                        fallback = theEdge.StartNode;
                        while (fallback.TemporaryDistance < 0.150 && fallback.IncomingEdges.Count <= 2)
                        {
                            TaxiEdge nextEdge = fallback.IncomingEdges.FirstOrDefault(e => e.StartNode != theEdge.EndNode);
                            if (nextEdge == null)
                                break;

                            // This catches the cases at the end of an apron where the only
                            // link is the actual taxipath already
                            if (VortexMath.AbsTurnAngle(theEdge.Bearing, nextEdge.Bearing) > VortexMath.Deg060Rad)
                                break;

                            // todo: each node should be added to the parking as 'push back trajectory'
                            fallback = nextEdge.StartNode;
                            theEdge = nextEdge;
                        }

                        NearestNode = fallback;
                        AlternateAfterPushBack = null;
                        PushBackLatitude = fallback.Latitude;
                        PushBackLongitude = fallback.Longitude;
                        return;
                    }
                }
            }


            selectedNodes = selectedNodes.Where(v => Math.Abs(adjustedBearing - VortexMath.BearingRadians(v, this)) < VortexMath.PI05);

            // For each qualifying node
            // Todo: check this part for tie downs
            foreach (TaxiNode v in selectedNodes)
            {
                // Look at each link coming into it from other nodes
                foreach (TaxiEdge incoming in v.IncomingEdges)
                {
                    double pushBackLatitude = 0;
                    double pushBackLongitude = 0;

                    // Now find where the 'start point outgoing line' intersects with the taxi link we are currently checking
                    if (!VortexMath.Intersection(Latitude, Longitude, adjustedBearing,
                                                incoming.StartNode.Latitude, incoming.StartNode.Longitude, incoming.Bearing,
                                                ref pushBackLatitude, ref pushBackLongitude))
                    {
                        // If computation fails, try again but now with the link in the other direction.
                        // Ignoring one way links here, I just want a push back target for now that's close to A link.
                        if (!VortexMath.Intersection(Latitude, Longitude, adjustedBearing,
                                                     incoming.StartNode.Latitude, incoming.StartNode.Longitude, incoming.Bearing + Math.PI,
                                                     ref pushBackLatitude, ref pushBackLongitude))
                        {
                            // Lines might be parallel, can't find intersection, skip
                            continue;
                        }
                    }

                    // Great Circles cross twice, if we found the one on the back of the earth, convert it to the
                    // one on the airport
                    // Todo: check might fail for airports on the -180/+180 longitude line
                    if (Math.Abs(pushBackLongitude - Longitude) > 0.25 * Math.PI)
                    {
                        pushBackLatitude = -pushBackLatitude;
                        pushBackLongitude += VortexMath.PI;
                        if (pushBackLongitude > VortexMath.PI)
                            pushBackLongitude -= VortexMath.PI2;
                    }

                    // To find the best spot we must know if the found intersection is actually
                    // on the link or if it is somewhere outside the actual link. These are 
                    // still usefull in some cases
                    bool foundTargetIsOutsideSegment = false;

                    // Todo: check might fail for airports on the -180/+180 longitude line
                    if (pushBackLatitude - incoming.StartNode.Latitude > 0)
                    {
                        if (v.Latitude - pushBackLatitude <= 0)
                            foundTargetIsOutsideSegment = true;
                    }
                    else if (v.Latitude - pushBackLatitude > 0)
                        foundTargetIsOutsideSegment = true;

                    if (pushBackLongitude - incoming.StartNode.Longitude > 0)
                    {
                        if (v.Longitude - pushBackLongitude <= 0)
                            foundTargetIsOutsideSegment = true;
                    }
                    else if (v.Longitude - pushBackLongitude > 0)
                        foundTargetIsOutsideSegment = true;

                    // Ignore links where the taxiout line intercepts at too sharp of an angle if it is 
                    // also outside the actual link.
                    // todo: Maybe ignore these links right away, saves a lot of calculations
                    double interceptAngleSharpness = Math.Abs(VortexMath.PI05 - Math.Abs((adjustedBearing - incoming.Bearing) % Math.PI)) / Math.PI;
                    if (foundTargetIsOutsideSegment && interceptAngleSharpness > 0.4)
                    {
                        continue;
                    }

                    // for the found location keep track of the distance to it from the start point
                    // also keep track of the distances to both nodes of the link we are inspecting now
                    double pushDistance = 0.0;
                    double distanceSource = VortexMath.DistancePyth(incoming.StartNode.Latitude, incoming.StartNode.Longitude, pushBackLatitude, pushBackLongitude);
                    double distanceDest = VortexMath.DistancePyth(v.Latitude, v.Longitude, pushBackLatitude, pushBackLongitude);

                    // If the found point is outside the link, add the distance to the nearest node of
                    // the link times 2 as a penalty to the actual distance. This prevents pushback point
                    // candidates that sneak up on the start because of a slight angle in remote link
                    // from being accepted as best.
                    TaxiNode nearestVertexIfPushBackOutsideSegment = null;
                    if (foundTargetIsOutsideSegment)
                    {
                        if (distanceSource < distanceDest)
                        {
                            pushDistance = distanceSource * 2.0;
                            nearestVertexIfPushBackOutsideSegment = incoming.StartNode;
                        }
                        else
                        {
                            pushDistance = distanceDest * 2.0;
                            nearestVertexIfPushBackOutsideSegment = v;
                        }
                    }

                    // How far is the candidate from the start point?
                    pushDistance += VortexMath.DistancePyth(Latitude, Longitude, pushBackLatitude, pushBackLongitude);

                    // See if it is a better candidate
                    if (pushDistance < shortestDistance)
                    {
                        bestPushBackLatitude = pushBackLatitude;
                        bestPushBackLongitude = pushBackLongitude;
                        shortestDistance = pushDistance;

                        // Setting things up for the path calculation that will follow later
                        if (foundTargetIsOutsideSegment)
                        {
                            // The taxi out route will start with a push to the best candidate
                            // Then move to the 'firstAfterPush' node and from there follow
                            // the 'shortest' path to the runway
                            firstAfterPush = nearestVertexIfPushBackOutsideSegment;
                            alternateAfterPush = null;
                        }
                        else
                        {
                            // The taxi out route will start with a push to the best candidate
                            // Then, if the second node in the find 'shortest' path is the alternate
                            // the first point will be skipped. If the second point is not the alternate,
                            // the 'firstAfterPush' will be the first indeed and after that the found
                            // route will be followed.
                            if (distanceSource < distanceDest)
                            {
                                firstAfterPush = incoming.StartNode;
                                alternateAfterPush = v;
                            }
                            else
                            {
                                firstAfterPush = v;
                                alternateAfterPush = incoming.StartNode;
                            }
                        }
                    }
                }
            }

            // All candiates have been considered, post processing the winner:
            if (shortestDistance < double.MaxValue)
            {
                // If there is one, check if it is not too far away from the start. This catches cases where
                // a gate at the end of an apron with heading parallel to the apron entry would get a best
                // target on the taxiway outside the apron.
                double actualDistance = VortexMath.DistanceKM(Latitude, Longitude, bestPushBackLatitude, bestPushBackLongitude);
                if (actualDistance > 0.25)
                {
                    // Fix this by pushing to the end point of the entry link
                    // (If that is actually the nearest node to the parking, but alas...
                    //  this is the default WT3 behaviour anyway)
                    NearestNode = selectedNodes.First();
                    AlternateAfterPushBack = null;
                    PushBackLatitude = NearestNode.Latitude;
                    PushBackLongitude = NearestNode.Longitude;
                }
                else
                {
                    // Store the results in the startpoint
                    PushBackLatitude = bestPushBackLatitude;
                    PushBackLongitude = bestPushBackLongitude;
                    NearestNode = firstAfterPush;
                    AlternateAfterPushBack = alternateAfterPush;
                }
            }
            else
            {
                // Crude fallback to defautl WT behavoit if nothing was found.
                NearestNode = fallback;
                AlternateAfterPushBack = null;
                PushBackLatitude = NearestNode.Latitude;
                PushBackLongitude = NearestNode.Longitude;
            }
        }

        internal void FindNearestLine(List<LineElement> _lines)
        {
            double minDist = double.MaxValue;
            LineElement leBEst = null;

            foreach (LineElement line in _lines)
            {
                LineElement from = line;
                foreach (LineElement seg in line.Segments)
                {
                    LineElement to = seg;

                    double dStart = VortexMath.DistanceKM(this, from);
                    if (dStart < minDist)
                    {
                        double lineBearing = VortexMath.BearingRadians(from, to);
                        double turn = VortexMath.AbsTurnAngle(lineBearing, Bearing);

                        //if (turn < VortexMath.Deg005Rad || turn > VortexMath.PI - VortexMath.Deg005Rad)
                        {
                            leBEst = line;
                            minDist = dStart;
                        }
                    }

                    from = to;
                }
            }

            if (leBEst != null)
            {
                Logger.Log($"{Name} has a line node at {minDist * 1000:0.000} meters: {leBEst.Latitude * VortexMath.Rad2Deg} {leBEst.Longitude * VortexMath.Rad2Deg}");
            }
            else
            {
                Logger.Log($"{Name} has no nearby line node");
            }
        }

        internal void DetermineWtTypes()
        {
            PossibleWtTypes = AircraftTypeConverter.WTTypesFromXPlaneLimits(XPlaneAircraftCategory.A, MaxSize, Operation);
        }

        public override string ToString()
        {
            return Name;
        }

        internal void SetMetaData(XPlaneAircraftCategory maxSize, string operation, IEnumerable<string> operators)
        {
            MaxSize = maxSize;
            Operation = OperationTypeConverter.FromString(operation);
            Operators = operators;
        }

        public void WriteParkingDef()
        {
            string filename = $"{Settings.WorldTrafficParkingDefs}\\{_airport.ICAO}\\{Name}.txt";
            using (StreamWriter sw = File.CreateText(filename))
            {
                int military = (Operation == OperationType.Military) ? 1 : 0;
                int cargo = (Operation == OperationType.Cargo) ? 1 : 0;
                sw.WriteLine($"Auto Generated by GRG \n<{_airport.ICAO}.{Name}> Ops: <{Operation}> Cat: <{MaxSize}> XpTypes: <{string.Join(" ", XpTypes)}>\n");
                sw.WriteLine("START");
                sw.WriteLine($"Name              {Name}");
                sw.WriteLine($"Types             {string.Join(" ", PossibleWtTypes.Select(w => (int)w).OrderBy(w => w))}");
                sw.WriteLine($"Operators         {string.Join(" ", Operators).ToUpper()}");
                sw.WriteLine($"AcName            ");
                sw.WriteLine($"Tailnum           ");
                sw.WriteLine($"Cargo             {cargo}");
                sw.WriteLine($"Military          {military}");
                sw.WriteLine($"Lat               {Latitude * VortexMath.Rad2Deg}");
                sw.WriteLine($"Lon               {Longitude * VortexMath.Rad2Deg}");
                sw.WriteLine($"AdjLat            {Latitude * VortexMath.Rad2Deg}");
                sw.WriteLine($"AdjLon            {Longitude * VortexMath.Rad2Deg}");
                sw.WriteLine($"HdgDegT           {((Bearing * VortexMath.Rad2Deg)+360) % 360:0}");
                sw.WriteLine($"ParkCenter        {Settings.ParkingReference}");
                sw.WriteLine($"PushBackDist1_ft  0"); // Push is part of route
                sw.WriteLine($"PushBackTurnHdg   0");
                sw.WriteLine($"PushBackDist2_ft  0");
                sw.WriteLine($"Has1300_data      1");
                sw.WriteLine($"Enabled           1");
                sw.WriteLine("END");
            }
        }
    }
}
