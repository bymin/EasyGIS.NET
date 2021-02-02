#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2020 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.ShapeFileLib class library of Easy GIS .NET.
** 
** Easy GIS .NET is free software: you can redistribute it and/or modify
** it under the terms of the GNU Lesser General Public License version 3 as
** published by the Free Software Foundation and appearing in the file
** lgpl-license.txt included in the packaging of this file.
**
** Easy GIS .NET is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License and
** GNU Lesser General Public License along with Easy GIS .NET.
** If not, see <http://www.gnu.org/licenses/>.
**
****************************************************************************/

#endregion

using EGIS.Mapbox.Vector.Tile;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using ThinkGeo.Core;

namespace EGIS.Web.Controls
{
    /// <summary>
    /// Utility class to generate Vector Tile data from ShapeFile layers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class can be combined with EGIS.Mapbox.Vector.Tile.VectorTileParser to create Mapbox vector tiles.
    /// </para>
    /// </remarks>
    /// <example> Sample code to create a Mapbox Vector Tile from a shapefile. 
    /// <code>        
    ///public void CreateMapboxTile(List&lt;ShapeFile&gt; mapLayers, string vectorTileFileName)
    ///{
    ///    //create a VectorTileGenerator
    ///    VectorTileGenerator tileGenerator = new VectorTileGenerator();
    ///    List&lt;VectorTileLayer&gt; tileLayers = tileGenerator.Generate(tileX, tileY, zoomLevel, mapLayers);
    ///    //encode the vector tile in Mapbox vector tile format
    ///    using (System.IO.FileStream fs = new System.IO.FileStream(vectorTileFileName, System.IO.FileMode.Create))
    ///    {
    ///        EGIS.Mapbox.Vector.Tile.VectorTileParser.Encode(tileLayers, fs);
    ///    }
    ///}
    /// </code>                                                   
    /// </example>
    public static class VectorTileGenerator
    {
        /// <summary>
        /// Generates a Vector Tile from ShapeFile layers
        /// </summary>
        /// <param name="tileX">Tile X coordinate</param>
        /// <param name="tileY">Tile Y coordinate</param>
        /// <param name="zoomLevel">Tile zoom level</param>
        /// <param name="layers">List of ShapeFile layers</param>
        /// <param name="outputTileFeature">optional OutputTileFeatureDelegate which will be called with each record feature that will be added to the tile. This delegate is useful to exclude feaures at tile zoom levels</param>
        /// <returns></returns>
        public static EGIS.Mapbox.Vector.Tile.Tile Generate(int tileX, int tileY, int zoomLevel, List<FeatureLayer> layers, IEnumerable<string> columnNames, int tileSize, int simplificationFactor)
        {
            EGIS.Mapbox.Vector.Tile.Tile tile = new Mapbox.Vector.Tile.Tile();
            //List<TileLayer> tileLayers = new List<TileLayer>();

            foreach (FeatureLayer featureLayer in layers)
            {
                var layer = ProcessTile(featureLayer, tileX, tileY, zoomLevel, columnNames, simplificationFactor, tileSize);
                if (layer.Features != null && layer.Features.Count > 0)
                {
                    tile.Layers.Add(layer);
                }
            }

            return tile;
        }

        [Flags]
        enum OutCode { Inside = 0, Left = 1, Right = 2, Bottom = 4, Top = 8 };

        [Flags]
        enum ClipState { None = 0, Start = 1, End = 2 };

        #region private members

        private static Dictionary<string, RectangleShape> dictionaryCache = new Dictionary<string, RectangleShape>();

        private static TileFeature GetVectorTileFeature(Feature feature, int tileX, int tileY, int zoom, int tileSize, RectangleInt clipBounds, int simplificationFactor)
        {
            TileFeature tileFeature = new TileFeature();
            switch (feature.GetWellKnownType())
            {
                case WellKnownType.Line:
                case WellKnownType.Multiline:
                    tileFeature.Type = Mapbox.Vector.Tile.GeometryType.LineString;
                    MultilineShape multiLineShape = new MultilineShape(feature.GetWellKnownBinary());
                    ProcessLineShape(tileX, tileY, zoom, tileSize, clipBounds, tileFeature, multiLineShape, simplificationFactor);
                    break;
                case WellKnownType.Polygon:
                case WellKnownType.Multipolygon:
                    tileFeature.Type = Mapbox.Vector.Tile.GeometryType.Polygon;
                    MultipolygonShape multiPolygonShape = new MultipolygonShape(feature.GetWellKnownBinary());
                    foreach (PolygonShape polygonShape in multiPolygonShape.Polygons)
                    {
                        ProcessRingShape(tileX, tileY, zoom, tileSize, clipBounds, tileFeature, polygonShape.OuterRing, simplificationFactor);
                        foreach (RingShape ringShape in polygonShape.InnerRings)
                        {
                            ProcessRingShape(tileX, tileY, zoom, tileSize, clipBounds, tileFeature, ringShape, simplificationFactor);
                        }
                    }
                    break;
                case WellKnownType.Point:
                case WellKnownType.Multipoint:
                    tileFeature.Type = Mapbox.Vector.Tile.GeometryType.Point;
                    List<PointInt> coordinates = new List<PointInt>();

                    MultipointShape multiPointShape = new MultipointShape();

                    if (feature.GetWellKnownType() == WellKnownType.Point)
                    {
                        PointShape pointShape = new PointShape(feature.GetWellKnownBinary());
                        multiPointShape.Points.Add(pointShape);
                    }
                    else if (feature.GetWellKnownType() == WellKnownType.Multipoint)
                    {
                        multiPointShape = new MultipointShape(feature.GetWellKnownBinary());
                    }

                    foreach (PointShape point in multiPointShape.Points)
                    {
                        PointInt pointI = WorldPointToTilePoint(point.X, point.Y, zoom, tileX, tileY, tileSize);
                        coordinates.Add(new PointInt(pointI.X, pointI.Y));
                    }
                    if (coordinates.Count > 0)
                    {
                        tileFeature.Geometry.Add(coordinates);
                    }
                    break;
                default:
                    tileFeature.Type = Mapbox.Vector.Tile.GeometryType.Unknown;
                    break;
            }

            //add the record attributes
            foreach (var attributes in feature.ColumnValues)
            {
                tileFeature.Attributes.Add(new Value(attributes.Key, attributes.Value));
            }

            return tileFeature;
        }

        private static TileLayer ProcessTile(FeatureLayer featureLayer, int tileX, int tileY, int zoom, IEnumerable<string> columnNames, int simplificationFactor, int tileSize)
        {
            RectangleShape tileBounds = GetTileSphericalMercatorBounds(tileX, tileY, zoom, tileSize);
            tileBounds.ScaleUp(5);
            featureLayer.Open();

            Collection<string> allFeatureIds = featureLayer.FeatureSource.GetFeatureIdsInsideBoundingBox(tileBounds);
            RectangleInt clipBounds = new RectangleInt()
            {
                XMin = -20,
                YMin = -20,
                XMax = tileSize + 20,
                YMax = tileSize + 20
            };


            TileLayer tileLayer = new TileLayer();
            tileLayer.Extent = (uint)tileSize;
            tileLayer.Version = 2;
            tileLayer.Name = featureLayer.Name;

            int IdsCountToExecute = allFeatureIds.Count;
            int startIndex = 0;

            while (IdsCountToExecute > 0)
            {
                string[] featureIds = GetSubString(allFeatureIds, startIndex, Math.Min(1000, IdsCountToExecute));
                Collection<Feature> features = featureLayer.FeatureSource.GetFeaturesByIds(featureIds, columnNames);
                foreach (Feature feature in features)
                {
                    TileFeature tileFeature = GetVectorTileFeature(feature, tileX, tileY, zoom, tileSize, clipBounds, simplificationFactor);
                    if (tileFeature.Geometry.Count > 0)
                    {
                        tileLayer.Features.Add(tileFeature);
                    }
                }
                IdsCountToExecute = IdsCountToExecute - featureIds.Length;
                startIndex = startIndex + featureIds.Length;
            }

            return tileLayer;
        }
        private static string[] GetSubString(Collection<string> sourceString, int startIndex, int endIndex)
        {
            string[] result = new string[endIndex];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = sourceString[startIndex + i];
            }
            return result;
        }


        private static void ProcessLineShape(int tileX, int tileY, int zoom, int tileSize, RectangleInt clipBounds, EGIS.Mapbox.Vector.Tile.TileFeature tileFeature, MultilineShape multiLineShape, int simplificationFactor)
        {
            foreach (LineShape line in multiLineShape.Lines)
            {
                PointInt[] pixelPoints = new PointInt[line.Vertices.Count];
                for (int n = 0; n < line.Vertices.Count; ++n)
                {
                    pixelPoints[n] = WorldPointToTilePoint(line.Vertices[n].X, line.Vertices[n].Y, zoom, tileX, tileY, tileSize);
                }

                PointInt[] simplifiedPixelPoints = SimplifyPointData(pixelPoints, simplificationFactor);

                //output count may be zero for short records at low zoom levels as 
                //the pixel coordinates wil be a single point after simplification
                if (simplifiedPixelPoints.Length > 0)
                {
                    List<int> clippedPoints = new List<int>();
                    List<int> parts = new List<int>();
                    ClipPolyline(simplifiedPixelPoints, clipBounds, clippedPoints, parts);
                    if (parts.Count > 0)
                    {
                        //output the clipped polyline
                        for (int n = 0; n < parts.Count; ++n)
                        {
                            int index1 = parts[n];
                            int index2 = n < parts.Count - 1 ? parts[n + 1] : clippedPoints.Count;

                            List<PointInt> lineString = new List<PointInt>();
                            tileFeature.Geometry.Add(lineString);
                            //clipped points store separate x/y pairs so there will be two values per measure
                            for (int i = index1; i < index2; i += 2)
                            {
                                lineString.Add(new PointInt(clippedPoints[i], clippedPoints[i + 1]));
                            }
                        }
                    }
                }
            }
        }

        private static void ProcessRingShape(int tileX, int tileY, int zoom, int tileSize, RectangleInt clipBounds, EGIS.Mapbox.Vector.Tile.TileFeature tileFeature, RingShape ringShape, int simplificationFactor)
        {
            PointInt[] tilePoints = new PointInt[ringShape.Vertices.Count];
            for (int n = 0; n < ringShape.Vertices.Count; ++n)
            {
                tilePoints[n] = WorldPointToTilePoint(ringShape.Vertices[n].X, ringShape.Vertices[n].Y, zoom, tileX, tileY, tileSize);
            }
            PointInt[] simplifiedTilePoints = SimplifyPointData(tilePoints, simplificationFactor);

            //output count may be zero for short records at low zoom levels as 
            //the pixel coordinates wil be a single point after simplification
            if (simplifiedTilePoints.Length > 3)
            {
                if (simplifiedTilePoints[0] != simplifiedTilePoints[simplifiedTilePoints.Length - 1])
                {
                    Console.WriteLine("Not Closed");
                }
                List<PointInt> clippedRing = ClipRingShape(simplifiedTilePoints, clipBounds);
                if (clippedRing.Count > 3)
                {
                    tileFeature.Geometry.Add(clippedRing);
                }
            }
        }

        private static PointInt[] SimplifyPointData(PointInt[] points, int simplificationFactor)
        {
            //check for duplicates points at end after they have been converted to pixel coordinates
            //polygons need at least 3 points so don't reduce less than this
            int pointCount = points.Length;

            while (pointCount > 2 && (points[pointCount - 1] == points[0]))
            {
                --pointCount;
            }

            PointInt[] resultPoints;
            if (pointCount <= 2)
            {
                resultPoints = new PointInt[pointCount];
                for (int i = 0; i < resultPoints.Length; i++)
                {
                    resultPoints[i] = points[i];
                }

                return resultPoints;
            }

            return SimplifyDouglasPeucker(points, simplificationFactor);
        }

        private static double GetZoomLevelSetScale(ZoomLevelSet zoomLevelSet, int zoomLevel)
        {
            switch (zoomLevel)
            {
                case 0: return zoomLevelSet.ZoomLevel01.Scale;
                case 1: return zoomLevelSet.ZoomLevel02.Scale;
                case 2: return zoomLevelSet.ZoomLevel03.Scale;
                case 3: return zoomLevelSet.ZoomLevel04.Scale;
                case 4: return zoomLevelSet.ZoomLevel05.Scale;
                case 5: return zoomLevelSet.ZoomLevel06.Scale;
                case 6: return zoomLevelSet.ZoomLevel07.Scale;
                case 7: return zoomLevelSet.ZoomLevel08.Scale;
                case 8: return zoomLevelSet.ZoomLevel09.Scale;
                case 9: return zoomLevelSet.ZoomLevel10.Scale;
                case 10: return zoomLevelSet.ZoomLevel11.Scale;
                case 11: return zoomLevelSet.ZoomLevel12.Scale;
                case 12: return zoomLevelSet.ZoomLevel13.Scale;
                case 13: return zoomLevelSet.ZoomLevel14.Scale;
                case 14: return zoomLevelSet.ZoomLevel15.Scale;
                case 15: return zoomLevelSet.ZoomLevel16.Scale;
                case 16: return zoomLevelSet.ZoomLevel17.Scale;
                case 17: return zoomLevelSet.ZoomLevel18.Scale;
                case 18: return zoomLevelSet.ZoomLevel19.Scale;
                case 19: return zoomLevelSet.ZoomLevel20.Scale;
                default:
                    return -1;
            }
        }

        private static RectangleShape GetTileSphericalMercatorBounds(long tileX, long tileY, int zoomLevel, int tileSize = 256)
        {
            SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
            double currentScale = GetZoomLevelSetScale(zoomLevelSet, zoomLevel);
            var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, 512, 512, GeographyUnit.Meter);
            RectangleShape bbox = tileMatrix.GetCell(tileX, tileY).BoundingBox;
            return new RectangleShape(bbox.UpperLeftPoint.X, bbox.UpperLeftPoint.Y, bbox.LowerRightPoint.X, bbox.LowerRightPoint.Y);
        }

        private static PointInt WorldPointToTilePoint(double pointX, double pointY, int zoomLevel, int tileX, int tileY, int tileSize)
        {
            string key = $"{zoomLevel}-{tileX}-{tileY}";
            if (!dictionaryCache.ContainsKey(key))
            {
                SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
                double currentScale = GetZoomLevelSetScale(zoomLevelSet, zoomLevel);
                var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, 512, 512, GeographyUnit.Meter);
                dictionaryCache.Add(key, tileMatrix.GetCell(tileX, tileY).BoundingBox);
            }

            RectangleShape bbox = dictionaryCache[key];
            double scale = ((double)tileSize / 40075016.4629396) * (1 << zoomLevel);
            PointInt result = new PointInt()
            {
                X = (int)Math.Round((pointX - bbox.LowerLeftPoint.X) * scale),
                Y = (int)Math.Round((bbox.UpperLeftPoint.Y - pointY) * scale)
            };

            return result;
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
            PointShape ab = new PointShape(b.X - a.X, b.Y - a.Y);
            PointShape bc = new PointShape(c.X - b.X, c.Y - b.Y);
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

        private static void ClipPolyline(PointInt[] input, RectangleInt clipBounds, List<int> clippedPoints, List<int> parts)
        {
            bool inside = false;
            for (int n = 0; n < input.Length - 1; ++n)
            {
                double x0 = input[n].X;
                double y0 = input[n].Y;
                double x1 = input[n + 1].X;
                double y1 = input[n + 1].Y;

                ClipState clipState;
                bool insideBounds = CohenSutherlandLineClip(ref x0, ref y0, ref x1, ref y1, ref clipBounds, out clipState);
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

        private static List<PointInt> ClipRingShape(PointInt[] inputPoints, RectangleInt clipBounds)
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
            previousInside = inputList[inputList.Count - 1].X >= clipBounds.XMin;
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointInt currentPoint = inputList[n];
                bool currentInside = currentPoint.X >= clipBounds.XMin;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointInt prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    int x = clipBounds.XMin;
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
            previousInside = inputList[inputList.Count - 1].Y <= clipBounds.YMax; ;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointInt currentPoint = inputList[n];
                bool currentInside = currentPoint.Y <= clipBounds.YMax;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointInt prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    int y = clipBounds.YMax;
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
            previousInside = inputList[inputList.Count - 1].X <= clipBounds.XMax;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointInt currentPoint = inputList[n];
                bool currentInside = currentPoint.X <= clipBounds.XMax;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointInt prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    int x = clipBounds.XMax;
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
            previousInside = inputList[inputList.Count - 1].Y >= clipBounds.YMin;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointInt currentPoint = inputList[n];
                bool currentInside = currentPoint.Y >= clipBounds.YMin;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointInt prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    int y = clipBounds.YMin;
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
        private static OutCode ComputeOutCode(double x, double y, ref RectangleInt clipBounds)
        {
            OutCode code = OutCode.Inside;

            if (x < clipBounds.XMin)           // to the left of clip window
                code |= OutCode.Left;
            else if (x > clipBounds.XMax)      // to the right of clip window
                code |= OutCode.Right;
            if (y < clipBounds.YMin)           // below the clip window
                code |= OutCode.Bottom;
            else if (y > clipBounds.YMax)      // above the clip window
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
        /// <param name="clipBounds"></param>
        /// <param name="clipState"> state of clipped line. ClipState.None if line is not clipped. If either end of the line 
        /// is clipped then the clipState flag are set (ClipState.Start or ClipState.End) </param>
        /// <returns>true if clipped line is inside given clip bounds</returns>
        private static bool CohenSutherlandLineClip(ref double x0, ref double y0, ref double x1, ref double y1, ref RectangleInt clipBounds, out ClipState clipState)
        {
            // compute outcodes for P0, P1, and whatever point lies outside the clip rectangle
            OutCode outcode0 = ComputeOutCode(x0, y0, ref clipBounds);
            OutCode outcode1 = ComputeOutCode(x1, y1, ref clipBounds);
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
                        x = x0 + (x1 - x0) * (clipBounds.YMax - y0) / (y1 - y0);
                        y = clipBounds.YMax;
                    }
                    else if ((outcodeOut & OutCode.Bottom) == OutCode.Bottom)
                    { // point is below the clip window
                        x = x0 + (x1 - x0) * (clipBounds.YMin - y0) / (y1 - y0);
                        y = clipBounds.YMin;// ymin;
                    }
                    else if ((outcodeOut & OutCode.Right) == OutCode.Right)
                    {  // point is to the right of clip window
                        y = y0 + (y1 - y0) * (clipBounds.XMax - x0) / (x1 - x0);
                        x = clipBounds.XMax;// xmax;
                    }
                    else if ((outcodeOut & OutCode.Left) == OutCode.Left)
                    {   // point is to the left of clip window
                        y = y0 + (y1 - y0) * (clipBounds.XMin - x0) / (x1 - x0);
                        x = clipBounds.XMin;// xmin;
                    }

                    // Now we move outside point to intersection point to clip
                    // and get ready for next pass.
                    if (outcodeOut == outcode0)
                    {
                        clipState |= ClipState.Start;
                        x0 = x;
                        y0 = y;
                        outcode0 = ComputeOutCode(x0, y0, ref clipBounds);
                    }
                    else
                    {
                        clipState |= ClipState.End;

                        x1 = x;
                        y1 = y;
                        outcode1 = ComputeOutCode(x1, y1, ref clipBounds);
                    }
                }
            }

            return accept;
        }

        #endregion
    }
}
