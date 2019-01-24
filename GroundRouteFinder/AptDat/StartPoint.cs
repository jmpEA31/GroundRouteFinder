using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string Type;
        public string Jets;
        public string Operation;
        public int MaxSize;

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

        public Parking() 
            : base()
        {
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
            double adjustedBearing = (Type == "gate") ? Bearing : (Bearing + Math.PI);
            if (adjustedBearing > Math.PI)
                adjustedBearing -= (VortexMath.PI2);

            // Compute the distance (arbitrary units) from each taxi node to the start location
            foreach (TaxiNode node in taxiNodes)
            {
                node.TemporaryDistance = VortexMath.DistancePyth(node, this);
            }

            // Select the 25 nearest, then from those select only the ones that are in the 180 degree arc of the direction
            // we intend to move in from the startpoint
            // todo: make both 25 and 180 parameters
            IEnumerable<TaxiNode> selectedNodes = taxiNodes.OrderBy(v => v.TemporaryDistance).Take(25);
            fallback = selectedNodes.First();
            selectedNodes = selectedNodes.Where(v => Math.Abs(adjustedBearing - VortexMath.BearingRadians(v, this)) < VortexMath.PI05);

            // For each qualifying node
            foreach (TaxiNode v in selectedNodes)
            {
                // Look at each link coming into it from other nodes
                foreach (MeasuredNode incoming in v.IncomingNodes)
                {
                    double pushBackLatitude = 0;
                    double pushBackLongitude = 0;

                    // Now find where the 'start point outgoing line' intersects with the taxi link we are currently checking
                    if (!VortexMath.Intersection(Latitude, Longitude, adjustedBearing,
                                                incoming.SourceNode.Latitude, incoming.SourceNode.Longitude, incoming.Bearing,
                                                ref pushBackLatitude, ref pushBackLongitude))
                    {
                        // If computation fails, try again but now with the link in the other direction.
                        // Ignoring one way links here, I just want a push back target for now that's close to A link.
                        if (!VortexMath.Intersection(Latitude, Longitude, adjustedBearing,
                                                     incoming.SourceNode.Latitude, incoming.SourceNode.Longitude, incoming.Bearing + Math.PI,
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
                        pushBackLongitude += Math.PI;
                    }

                    // To find the best spot we must know if the found intersection is actually
                    // on the link or if it is somewhere outside the actual link. These are 
                    // still usefull in some cases
                    bool foundTargetIsOutsideSegment = false;

                    // Todo: check might fail for airports on the -180/+180 longitude line
                    if (pushBackLatitude - incoming.SourceNode.Latitude > 0)
                    {
                        if (v.Latitude - pushBackLatitude <= 0)
                            foundTargetIsOutsideSegment = true;
                    }
                    else if (v.Latitude - pushBackLatitude > 0)
                        foundTargetIsOutsideSegment = true;

                    if (pushBackLongitude - incoming.SourceNode.Longitude > 0)
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
                    double distanceSource = VortexMath.DistancePyth(incoming.SourceNode.Latitude, incoming.SourceNode.Longitude, pushBackLatitude, pushBackLongitude);
                    double distanceDest = VortexMath.DistancePyth(v.Latitude, v.Longitude, pushBackLatitude, pushBackLongitude);

                    // If the found point is outside the link, add the distance to the nearest node of
                    // the link time 2 as a penalty to the actual distance. This prevents pushback point
                    // candidates that sneak up on the start because of a slight angle in remote link
                    // from being accepted as best.
                    TaxiNode nearestVertexIfPushBackOutsideSegment = null;
                    if (foundTargetIsOutsideSegment)
                    {
                        if (distanceSource < distanceDest)
                        {
                            pushDistance = distanceSource * 2.0;
                            nearestVertexIfPushBackOutsideSegment = incoming.SourceNode;
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
                                firstAfterPush = incoming.SourceNode;
                                alternateAfterPush = v;
                            }
                            else
                            {
                                firstAfterPush = v;
                                alternateAfterPush = incoming.SourceNode;
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



        public override string ToString()
        {
            return Name;
        }

        internal void SetLimits(int maxSize, string operation)
        {
            MaxSize = maxSize;
            Operation = operation;
        }
    }
}
