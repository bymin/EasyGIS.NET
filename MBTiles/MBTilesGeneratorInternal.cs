using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkGeo.Core;

namespace MBTiles
{
    partial class MBTilesGenerator
    {
        private static void ClipPolyline(PointInt[] input, RectangleInt tileScreenBoundingBox, List<int> clippedPoints, List<int> parts)
        {
            bool inside = false;
            for (int n = 0; n < input.Length - 1; ++n)
            {
                double x0 = input[n].X;
                double y0 = input[n].Y;
                double x1 = input[n + 1].X;
                double y1 = input[n + 1].Y;

                ClipState clipState;
                bool insideBounds = CohenSutherlandLineClip(ref x0, ref y0, ref x1, ref y1, ref tileScreenBoundingBox, out clipState);
                if (insideBounds)
                {
                    //new part
                    if (!inside || (clipState & ClipState.Start) == ClipState.Start)
                    {
                        parts.Add(clippedPoints.Count);
                        clippedPoints.Add((int)Math.Round(x0));
                        clippedPoints.Add((int)Math.Round(y0));
                    }
                    clippedPoints.Add((int)Math.Round(x1));
                    clippedPoints.Add((int)Math.Round(y1));
                }
                inside = insideBounds;
            }
        }

        private static List<PointInt> ClipRingShape(PointInt[] inputPoints, RectangleInt tileScreenBoundingBox)
        {
            List<PointInt> outputList = new List<PointInt>();
            List<PointInt> inputList = new List<PointInt>(inputPoints.Length);
            bool previousInside;
            for (int n = 0; n < inputPoints.Length; ++n)
            {
                inputList.Add(inputPoints[n]);
            }
            bool inputPolygonIsHole = IsPolygonHole(inputPoints, inputPoints.Length);

            //test left
            previousInside = inputList[inputList.Count - 1].X >= tileScreenBoundingBox.XMin;
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointInt currentPoint = inputList[n];
                bool currentInside = currentPoint.X >= tileScreenBoundingBox.XMin;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointInt prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    int x = tileScreenBoundingBox.XMin;
                    int y = prevPoint.Y + (currentPoint.Y - prevPoint.Y) * (x - prevPoint.X) / (currentPoint.X - prevPoint.X);
                    outputList.Add(new PointInt(x, y));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return outputList;

            //test top
            inputList = outputList.ToList();
            previousInside = inputList[inputList.Count - 1].Y <= tileScreenBoundingBox.YMax; ;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointInt currentPoint = inputList[n];
                bool currentInside = currentPoint.Y <= tileScreenBoundingBox.YMax;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointInt prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    int y = tileScreenBoundingBox.YMax;
                    int x = prevPoint.X + (currentPoint.X - prevPoint.X) * (y - prevPoint.Y) / (currentPoint.Y - prevPoint.Y);
                    outputList.Add(new PointInt(x, y));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return outputList;

            //test right
            inputList = outputList.ToList();
            previousInside = inputList[inputList.Count - 1].X <= tileScreenBoundingBox.XMax;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointInt currentPoint = inputList[n];
                bool currentInside = currentPoint.X <= tileScreenBoundingBox.XMax;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointInt prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    int x = tileScreenBoundingBox.XMax;
                    int y = prevPoint.Y + (currentPoint.Y - prevPoint.Y) * (x - prevPoint.X) / (currentPoint.X - prevPoint.X);
                    outputList.Add(new PointInt(x, y));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return outputList;

            //test bottom
            inputList = outputList.ToList();
            previousInside = inputList[inputList.Count - 1].Y >= tileScreenBoundingBox.YMin;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointInt currentPoint = inputList[n];
                bool currentInside = currentPoint.Y >= tileScreenBoundingBox.YMin;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointInt prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    int y = tileScreenBoundingBox.YMin;
                    int x = prevPoint.X + (currentPoint.X - prevPoint.X) * (y - prevPoint.Y) / (currentPoint.Y - prevPoint.Y);
                    outputList.Add(new PointInt(x, y));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return outputList;

            bool clippedPolygonIsHole = IsPolygonHole(outputList, outputList.Count);
            if (clippedPolygonIsHole == inputPolygonIsHole) outputList.Reverse();

            if (outputList.Count > 3 && outputList[0] != outputList[outputList.Count - 1])
            {
                outputList.Insert(0, outputList[0]);
            }
            return outputList;
        }
        
        private static void DouglasPeuckerReduction(PointInt[] points, int firstPoint, int lastPoint, Double tolerance, List<int> pointIndexsToKeep)
        {
            double maxDistance = 0;
            int indexMax = 0;

            for (int index = firstPoint; index < lastPoint; ++index)
            {
                double distance = LineSegPointDist(ref points[firstPoint], ref points[lastPoint], ref points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexMax = index;
                }
            }

            if (maxDistance > tolerance && indexMax != 0)
            {
                //Add the largest point that exceeds the tolerance
                pointIndexsToKeep.Add(indexMax);

                DouglasPeuckerReduction(points, firstPoint, indexMax, tolerance, pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexMax, lastPoint, tolerance, pointIndexsToKeep);
            }
        }

        private static double Dot(ref PointInt a, ref PointInt b, ref PointInt c)
        {
            Vertex ab = new Vertex(b.X - a.X, b.Y - a.Y);
            Vertex bc = new Vertex(c.X - b.X, c.Y - b.Y);
            return (ab.X * bc.X) + (ab.Y * bc.Y);
        }

        private static double Cross(ref PointInt a, ref PointInt b, ref PointInt c)
        {
            PointShape ab = new PointShape(b.X - a.X, b.Y - a.Y);
            PointShape ac = new PointShape(c.X - a.X, c.Y - a.Y);
            return (ab.X * ac.Y) - (ab.Y * ac.X);
        }

        private static double Distance(ref PointInt a, ref PointInt b)
        {
            double d1 = a.X - b.X;
            double d2 = a.Y - b.Y;
            return Math.Sqrt((d1 * d1) + (d2 * d2));
        }

        private static double LineSegPointDist(ref PointInt a, ref PointInt b, ref PointInt c)
        {
            //float dist = cross(a,b,c) / distance(a,b);

            if (Dot(ref a, ref b, ref c) > 0)
            {
                return Distance(ref b, ref c);
            }
            if (Dot(ref b, ref a, ref c) > 0)
            {
                return Distance(ref a, ref c);
            }
            return Math.Abs(Cross(ref a, ref b, ref c) / Distance(ref a, ref b));
        }

        private static PointInt[] SimplifyDouglasPeucker(PointInt[] inputPoints, double tolerance)
        {
            PointInt[] resultPoints = new PointInt[inputPoints.Length];
            if (inputPoints.Length < 3)
            {
                for (int i = 0; i < inputPoints.Length; ++i)
                {
                    resultPoints[i] = inputPoints[i];
                }
                return resultPoints;
            }

            Int32 firstPoint = 0;
            Int32 lastPoint = inputPoints.Length - 1;

            //Add the first and last index to the keepers
            var reducedPointIndicies = new List<int>();
            reducedPointIndicies.Add(firstPoint);
            reducedPointIndicies.Add(lastPoint);

            //ensure first and last point not the same
            while (lastPoint >= 0 && inputPoints[firstPoint].Equals(inputPoints[lastPoint]))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(inputPoints, firstPoint, lastPoint, tolerance, reducedPointIndicies);

            reducedPointIndicies.Sort();


            //if only two points check if both points the same
            if (reducedPointIndicies.Count == 2)
            {
                if (inputPoints[reducedPointIndicies[0]] == inputPoints[reducedPointIndicies[1]]) return new PointInt[0];
            }

            PointInt endPoint = inputPoints[inputPoints.Length - 1];
            bool addEndpoint = endPoint == inputPoints[0];

            if (addEndpoint)
            {
                resultPoints = new PointInt[reducedPointIndicies.Count + 1];
            }
            else
            {
                resultPoints = new PointInt[reducedPointIndicies.Count];
            }
            for (int n = 0; n < reducedPointIndicies.Count; ++n)
            {
                resultPoints[n] = inputPoints[reducedPointIndicies[n]];
            }
            if (addEndpoint)
            {
                resultPoints[reducedPointIndicies.Count] = endPoint;
            }

            return resultPoints;
        }
        
        private static bool IsPolygonHole(IList<PointInt> points, int numPoints)
        {
            //if we are detecting holes then we need to calculate the area
            double area = 0;
            int j = numPoints - 1;
            for (int i = 0; i < numPoints; ++i)
            {
                area += (points[j].X * points[i].Y - points[i].X * points[j].Y);
                j = i;
            }

            return area > 0;
        }

        // Compute the bit code for a point (x, y) using the clip rectangle
        // bounded diagonally by (xmin, ymin), and (xmax, ymax)

        // ASSUME THAT xmax, xmin, ymax and ymin are global constants.
        private static OutCode ComputeOutCode(double x, double y, ref RectangleInt tileScreenBoundingBox)
        {
            OutCode code = OutCode.Inside;

            if (x < tileScreenBoundingBox.XMin)           // to the left of clip window
                code |= OutCode.Left;
            else if (x > tileScreenBoundingBox.XMax)      // to the right of clip window
                code |= OutCode.Right;
            if (y < tileScreenBoundingBox.YMin)           // below the clip window
                code |= OutCode.Bottom;
            else if (y > tileScreenBoundingBox.YMax)      // above the clip window
                code |= OutCode.Top;

            return code;
        }

        /// <summary>
        /// Cohen–Sutherland clipping algorithm clips a line from P0 = (x0, y0) to P1 = (x1, y1) against a clipBounds rectangle 
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="tileScreenBoundingBox"></param>
        /// <param name="clipState"> state of clipped line. ClipState.None if line is not clipped. If either end of the line 
        /// is clipped then the clipState flag are set (ClipState.Start or ClipState.End) </param>
        /// <returns>true if clipped line is inside given clip bounds</returns>
        private static bool CohenSutherlandLineClip(ref double x0, ref double y0, ref double x1, ref double y1, ref RectangleInt tileScreenBoundingBox, out ClipState clipState)
        {
            // compute outcodes for P0, P1, and whatever point lies outside the clip rectangle
            OutCode outcode0 = ComputeOutCode(x0, y0, ref tileScreenBoundingBox);
            OutCode outcode1 = ComputeOutCode(x1, y1, ref tileScreenBoundingBox);
            bool accept = false;

            clipState = ClipState.None;

            while (true)
            {
                if ((outcode0 | outcode1) == OutCode.Inside)
                {
                    // bitwise OR is 0: both points inside window; trivially accept and exit loop
                    accept = true;
                    break;
                }
                else if ((outcode0 & outcode1) != OutCode.Inside)
                {
                    // bitwise AND is not 0: both points share an outside zone (LEFT, RIGHT, TOP,
                    // or BOTTOM), so both must be outside window; exit loop (accept is false)
                    break;
                }
                else
                {
                    // failed both tests, so calculate the line segment to clip
                    // from an outside point to an intersection with clip edge
                    double x = 0, y = 0;

                    // At least one endpoint is outside the clip rectangle; pick it.
                    OutCode outcodeOut = outcode0 != OutCode.Inside ? outcode0 : outcode1;

                    // Now find the intersection point;
                    // use formulas:
                    //   slope = (y1 - y0) / (x1 - x0)
                    //   x = x0 + (1 / slope) * (ym - y0), where ym is ymin or ymax
                    //   y = y0 + slope * (xm - x0), where xm is xmin or xmax
                    // No need to worry about divide-by-zero because, in each case, the
                    // outcode bit being tested guarantees the denominator is non-zero
                    if ((outcodeOut & OutCode.Top) == OutCode.Top)
                    {           // point is above the clip window
                        x = x0 + (x1 - x0) * (tileScreenBoundingBox.YMax - y0) / (y1 - y0);
                        y = tileScreenBoundingBox.YMax;
                    }
                    else if ((outcodeOut & OutCode.Bottom) == OutCode.Bottom)
                    { // point is below the clip window
                        x = x0 + (x1 - x0) * (tileScreenBoundingBox.YMin - y0) / (y1 - y0);
                        y = tileScreenBoundingBox.YMin;// ymin;
                    }
                    else if ((outcodeOut & OutCode.Right) == OutCode.Right)
                    {  // point is to the right of clip window
                        y = y0 + (y1 - y0) * (tileScreenBoundingBox.XMax - x0) / (x1 - x0);
                        x = tileScreenBoundingBox.XMax;// xmax;
                    }
                    else if ((outcodeOut & OutCode.Left) == OutCode.Left)
                    {   // point is to the left of clip window
                        y = y0 + (y1 - y0) * (tileScreenBoundingBox.XMin - x0) / (x1 - x0);
                        x = tileScreenBoundingBox.XMin;// xmin;
                    }

                    // Now we move outside point to intersection point to clip
                    // and get ready for next pass.
                    if (outcodeOut == outcode0)
                    {
                        clipState |= ClipState.Start;
                        x0 = x;
                        y0 = y;
                        outcode0 = ComputeOutCode(x0, y0, ref tileScreenBoundingBox);
                    }
                    else
                    {
                        clipState |= ClipState.End;

                        x1 = x;
                        y1 = y;
                        outcode1 = ComputeOutCode(x1, y1, ref tileScreenBoundingBox);
                    }
                }
            }

            return accept;
        }

        [Flags]
        enum OutCode { Inside = 0, Left = 1, Right = 2, Bottom = 4, Top = 8 };

        [Flags]
        enum ClipState { None = 0, Start = 1, End = 2 };
    }
}
