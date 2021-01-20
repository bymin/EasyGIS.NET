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
    public class VectorTileGenerator
    {

        /// <summary>
        /// delegate to return whether a feature should be output at a given zoom level and tile coordinates
        /// </summary>
        /// <param name="shapeFile"></param>
        /// <param name="recordIndex"></param>
        /// <param name="tileZ"></param>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <returns></returns>
        public delegate bool OutputTileFeatureDelegate(ShapeFileFeatureLayer shapeFile, int recordIndex, int tileZ, int tileX, int tileY);

        /// <summary>
        /// VectorTileGenerator constructor 
        /// </summary>
        public VectorTileGenerator()
        {
            TileSize = 512;
            SimplificationPixelThreshold = 1;
            OutputMeasureValues = false;
            MeasuresAttributeName = "_MValues";
        }

        /// <summary>
        /// The size of the vector tiles
        /// </summary>
        public int TileSize
        {
            get;
            set;
        }

        /// <summary>
        /// Simplification Threshold. Default is 1
        /// </summary>
        /// <remarks>
        /// This property will simplify geometry points when the vector data is generated at lower tile
        /// zoom levels. In general this property should not be changed from the default value of 1
        /// </remarks>
        public int SimplificationPixelThreshold
        {
            get;
            set;
        }

        /// <summary>
        /// whether to output PolylineM Measures.
        /// </summary>
        public bool OutputMeasureValues
        {
            get;
            set;
        }

        /// <summary>
        /// Output Measures Attribute name. Default is "_MValues"
        /// </summary>
        public string MeasuresAttributeName
        {
            get;
            set;
        }



        /// <summary>
        /// Generates a Vector Tile from ShapeFile layers
        /// </summary>
        /// <param name="tileX">Tile X coordinate</param>
        /// <param name="tileY">Tile Y coordinate</param>
        /// <param name="zoomLevel">Tile zoom level</param>
        /// <param name="layers">List of ShapeFile layers</param>
        /// <param name="outputTileFeature">optional OutputTileFeatureDelegate which will be called with each record feature that will be added to the tile. This delegate is useful to exclude feaures at tile zoom levels</param>
        /// <returns></returns>
        public virtual List<VectorTileLayer> Generate(int tileX, int tileY, int zoomLevel, List<ShapeFileFeatureLayer> layers, OutputTileFeatureDelegate outputTileFeature = null)
        {

            List<VectorTileLayer> tileLayers = new List<VectorTileLayer>();

            //  lock (EGIS.ShapeFileLib.ShapeFile.Sync)
            {
                foreach (ShapeFileFeatureLayer shapeFile in layers)
                {
                    var shapeFileType = shapeFile.GetShapeFileType();

                    if (shapeFileType == ShapeFileType.Polyline || shapeFileType == ShapeFileType.PolylineM)
                    {
                        var layer = ProcessLineStringTile(shapeFile, tileX, tileY, zoomLevel, outputTileFeature);
                        if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                        {
                            tileLayers.Add(layer);
                        }
                    }
                    else if (shapeFileType == ShapeFileType.Polygon || shapeFileType == ShapeFileType.PolygonZ)
                    {
                        var layer = ProcessPolygonTile(shapeFile, tileX, tileY, zoomLevel, outputTileFeature);
                        if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                        {
                            tileLayers.Add(layer);
                        }
                    }
                    else if (shapeFileType == ShapeFileType.Point || shapeFileType == ShapeFileType.Multipoint ||
                             shapeFileType == ShapeFileType.PointZ || shapeFileType == ShapeFileType.PointM)
                    {
                        var layer = ProcessPointTile(shapeFile, tileX, tileY, zoomLevel, outputTileFeature);
                        if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                        {
                            tileLayers.Add(layer);
                        }
                    }
                    else throw new NotImplementedException("Shape Type " + shapeFileType + " not implemented yet");
                }
            }

            return tileLayers;
        }

        [Flags]
        enum OutCode { Inside = 0, Left = 1, Right = 2, Bottom = 4, Top = 8 };

        // Compute the bit code for a point (x, y) using the clip rectangle
        // bounded diagonally by (xmin, ymin), and (xmax, ymax)

        // ASSUME THAT xmax, xmin, ymax and ymin are global constants.

        private static OutCode ComputeOutCode(double x, double y, ref ClipBounds clipBounds)
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
        static bool CohenSutherlandLineClip(ref double x0, ref double y0, ref double x1, ref double y1, ref ClipBounds clipBounds, out ClipState clipState)
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



        public struct ClipBounds
        {
            public double XMin;
            public double XMax;
            public double YMin;
            public double YMax;

            public override string ToString()
            {
                return string.Format("ClipBounds XMin:{0}, XMax:{1}, YMin:{2}, YMax:{3}", XMin, XMax, YMin, YMax);
            }
        }
        [Flags]
        enum ClipState { None = 0, Start = 1, End = 2 };

        public static void PolyLineClip(System.Drawing.Point[] input, int inputCount, ClipBounds clipBounds, List<int> clippedPoints, List<int> parts, double[] measures, List<double> clippedMeasures)
        {
            bool inside = false;
            for (int n = 0; n < inputCount - 1; ++n)
            {
                double x0 = input[n].X, y0 = input[n].Y, x1 = input[n + 1].X, y1 = input[n + 1].Y;

                ClipState clipState;
                bool insideBounds = CohenSutherlandLineClip(ref x0, ref y0, ref x1, ref y1, ref clipBounds, out clipState);
                if (insideBounds)
                {
                    int dx = input[n + 1].X - input[n].X;
                    int dy = input[n + 1].Y - input[n].Y;


                    //new part
                    if (!inside || (clipState & ClipState.Start) == ClipState.Start)
                    {
                        parts.Add(clippedPoints.Count);
                        clippedPoints.Add((int)Math.Round(x0));
                        clippedPoints.Add((int)Math.Round(y0));

                        if (dx != 0)
                        {
                            double r = ((double)(x0 - input[n].X)) / dx;
                            clippedMeasures.Add(measures[n] + r * (measures[n + 1] - measures[n]));
                        }
                        else if (dy != 0)
                        {
                            double r = ((double)(y0 - input[n].Y)) / dy;
                            clippedMeasures.Add(measures[n] + r * (measures[n + 1] - measures[n]));
                        }
                        else
                        {
                            //point
                            clippedMeasures.Add(measures[n]);
                        }

                    }
                    clippedPoints.Add((int)Math.Round(x1));
                    clippedPoints.Add((int)Math.Round(y1));

                    if (dx != 0)
                    {
                        double r = ((double)(x1 - input[n].X)) / dx;
                        clippedMeasures.Add(measures[n] + r * (measures[n + 1] - measures[n]));
                    }
                    else if (dy != 0)
                    {
                        double r = ((double)(y1 - input[n].Y)) / dy;
                        clippedMeasures.Add(measures[n] + r * (measures[n + 1] - measures[n]));
                    }
                    else
                    {
                        //point
                        clippedMeasures.Add(measures[n + 1]);
                    }

                }
                inside = insideBounds;
            }

        }


        public static void PolyLineClip(System.Drawing.Point[] input, int inputCount, ClipBounds clipBounds, List<int> clippedPoints, List<int> parts)
        {
            bool inside = false;
            for (int n = 0; n < inputCount - 1; ++n)
            {
                double x0 = input[n].X, y0 = input[n].Y, x1 = input[n + 1].X, y1 = input[n + 1].Y;
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

        private const long l = 1;
        static Dictionary<string, RectangleShape> dictionaryCache = new Dictionary<string, RectangleShape>();

        public static void LLToPixel2(Vertex sphericalMercator, int zoomLevel, int tileX, int tileY, out long x, out long y, int tileSize = 256)
        {
            string key = $"{zoomLevel}-{tileX}-{tileY}";
            if (!dictionaryCache.ContainsKey(key))
            {
                SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
                double currentScale = GetZoomLevelIndex(zoomLevelSet, zoomLevel);
                var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, 512, 512, GeographyUnit.Meter);
                dictionaryCache.Add(key, tileMatrix.GetCell(tileX, tileY).BoundingBox);
            }

            RectangleShape bbox = dictionaryCache[key];



            double scale = ((double)tileSize / 40075016.4629396) * (l << zoomLevel);
            x = (long)Math.Round((sphericalMercator.X - bbox.LowerLeftPoint.X) * scale);

            // 1
            //y = (long)Math.Round((sphericalMercator.Y - bbox.LowerLeftPoint.Y ) * scale);

            // 2
            //y = (long)Math.Round((bbox.UpperLeftPoint.Y - sphericalMercator.Y) * scale);
            //y = tileSize - y;

            // 3
            y = (long)Math.Round((bbox.UpperLeftPoint.Y - sphericalMercator.Y) * scale);

        }


        #region private members

        private VectorTileLayer ProcessLineStringTile(ShapeFileFeatureLayer shapeFile, int tileX, int tileY, int zoom, OutputTileFeatureDelegate outputTileFeature)
        {
            int tileSize = TileSize;
            RectangleShape tileBounds = GetTileSphericalMercatorBounds(tileX, tileY, zoom, tileSize);
            //create a buffer around the tileBounds 
            //tileBounds.Inflate(tileBounds.Width * 0.05, tileBounds.Height * 0.05);
            tileBounds.ScaleUp(5);

            int simplificationFactor = Math.Min(10, Math.Max(1, SimplificationPixelThreshold));

            System.Drawing.Point tilePixelOffset = new System.Drawing.Point((tileX * tileSize), (tileY * tileSize));

            List<int> indicies = new List<int>();
            //shapeFile.GetShapeIndiciesIntersectingRect(indicies, tileBounds);
            Collection<ThinkGeo.Core.Feature> features = shapeFile.QueryTools.GetFeaturesInsideBoundingBox(tileBounds, ReturningColumnsType.AllColumns);
            ClipBounds clipBounds = new ClipBounds()
            {
                XMin = -20,
                YMin = -20,
                XMax = tileSize + 20,
                YMax = tileSize + 20
            };
            bool outputMeasureValues = this.OutputMeasureValues && (shapeFile.GetShapeFileType() == ShapeFileType.PolylineM || shapeFile.GetShapeFileType() == ShapeFileType.PolylineZ);
            System.Drawing.Point[] pixelPoints = new System.Drawing.Point[1024];
            System.Drawing.Point[] simplifiedPixelPoints = new System.Drawing.Point[1024];
            double[] simplifiedMeasures = new double[1024];


            VectorTileLayer tileLayer = new VectorTileLayer();
            tileLayer.Extent = (uint)tileSize;
            tileLayer.Version = 2;
            tileLayer.Name = !string.IsNullOrEmpty(shapeFile.Name) ? shapeFile.Name : System.IO.Path.GetFileNameWithoutExtension(shapeFile.ShapePathFilename);

            if (features.Count > 0)
            {
                
                foreach (ThinkGeo.Core.Feature index in features)
                {

                    //  if (outputTileFeature != null && !outputTileFeature(shapeFile, index, zoom, tileX, tileY)) continue;

                    VectorTileFeature feature = new VectorTileFeature()
                    {
                        Id = index.Id,
                        Geometry = new List<List<Coordinate>>(),
                        Attributes = new List<AttributeKeyValue>()
                    };

                    //get the point data
                    //var recordPoints = shapeFile.GetShapeDataD(index);
                    MultilineShape multiLineShape = new MultilineShape(index.GetWellKnownBinary());

                    System.Collections.ObjectModel.ReadOnlyCollection<double[]> recordMeasures = null;

                    //if (outputMeasureValues) recordMeasures = shapeFile.GetShapeMDataD(index);

                    List<double> outputMeasures = new List<double>();
                    int partIndex = 0;
                    //foreach (PointD[] points in recordPoints)
                    foreach (LineShape line in multiLineShape.Lines)
                    {
                        double[] measures = recordMeasures != null ? recordMeasures[partIndex] : null;
                        //convert to pixel coordinates;
                        if (pixelPoints.Length < line.Vertices.Count)
                        {
                            pixelPoints = new System.Drawing.Point[line.Vertices.Count + 10];
                            simplifiedPixelPoints = new System.Drawing.Point[line.Vertices.Count + 10];
                            simplifiedMeasures = new double[line.Vertices.Count + 10];
                        }

                        for (int n = 0; n < line.Vertices.Count; ++n)
                        {
                            Int64 x, y;
                            LLToPixel2(line.Vertices[n], zoom, tileX, tileY, out x, out y, tileSize);
                            //pixelPoints[n].X = (int)(x - tilePixelOffset.X);
                            //pixelPoints[n].Y = (int)(y - tilePixelOffset.Y);
                            pixelPoints[n].X = (int)x;
                            pixelPoints[n].Y = (int)y;
                        }

                        int outputCount = 0;
                        SimplifyPointData(pixelPoints, measures, line.Vertices.Count, simplificationFactor, simplifiedPixelPoints, simplifiedMeasures, ref pointsBuffer, ref outputCount);

                        //output count may be zero for short records at low zoom levels as 
                        //the pixel coordinates wil be a single point after simplification
                        if (outputCount > 0)
                        {
                            List<int> clippedPoints = new List<int>();
                            List<int> parts = new List<int>();

                            if (outputMeasureValues)
                            {
                                List<double> clippedMeasures = new List<double>();
                                PolyLineClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPoints, parts, simplifiedMeasures, clippedMeasures);
                                outputMeasures.AddRange(clippedMeasures);
                            }
                            else
                            {
                                PolyLineClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPoints, parts);
                            }
                            if (parts.Count > 0)
                            {
                                //output the clipped polyline
                                for (int n = 0; n < parts.Count; ++n)
                                {
                                    int index1 = parts[n];
                                    int index2 = n < parts.Count - 1 ? parts[n + 1] : clippedPoints.Count;

                                    List<Coordinate> lineString = new List<Coordinate>();
                                    feature.GeometryType = EGIS.Mapbox.Vector.Tile.Tile.GeomType.LineString;
                                    feature.Geometry.Add(lineString);
                                    //clipped points store separate x/y pairs so there will be two values per measure
                                    for (int i = index1; i < index2; i += 2)
                                    {
                                        lineString.Add(new Coordinate(clippedPoints[i], clippedPoints[i + 1]));
                                    }
                                }
                            }
                        }
                        ++partIndex;
                    }
                    
                    //add the record attributes
                    //string[] fieldNames = shapeFile.GetAttributeFieldNames();
                    //string[] values = shapeFile.GetAttributeFieldValues(index);
                    List<string> fields1 = new List<string>();
                    List<string> values1 = new List<string>();
                    foreach (var attributes in index.ColumnValues)
                    {
                        fields1.Add(attributes.Key);
                        values1.Add(attributes.Value);
                    }
                    string[] fieldNames = fields1.ToArray();
                    string[] values = values1.ToArray();

                    for (int n = 0; n < values.Length; ++n)
                    {
                        feature.Attributes.Add(new AttributeKeyValue(fieldNames[n], values[n].Trim()));
                    }
                    if (outputMeasureValues)
                    {
                        string s = Newtonsoft.Json.JsonConvert.SerializeObject(outputMeasures, new DoubleFormatConverter(4));
                        feature.Attributes.Add(new AttributeKeyValue(this.MeasuresAttributeName, s));
                    }

                    if (feature.Geometry.Count > 0)
                    {
                        tileLayer.VectorTileFeatures.Add(feature);
                    }

                }
            }
            return tileLayer;
        }

        PointShape[] pointsBuffer = new PointShape[1024];
        System.Drawing.Point[] pixelPoints = new System.Drawing.Point[1024];
        System.Drawing.Point[] simplifiedPixelPoints = new System.Drawing.Point[1024];


        public static double GetZoomLevelIndex(ZoomLevelSet zoomLevelSet, int zoomLevel)
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

        public static RectangleShape GetTileSphericalMercatorBounds(long tileX, long tileY, int zoomLevel, int tileSize = 256)
        {
            SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
            //double currentScale = zoomLevelSet.CustomZoomLevels[zoomLevel].Scale;
            double currentScale = GetZoomLevelIndex(zoomLevelSet, zoomLevel);
            var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, 512, 512, GeographyUnit.Meter);
            RectangleShape bbox = tileMatrix.GetCell(tileX, tileY).BoundingBox;
            return new RectangleShape(bbox.UpperLeftPoint.X, bbox.UpperLeftPoint.Y, bbox.LowerRightPoint.X, bbox.LowerRightPoint.Y);
            //            return RectangleD.FromLTRB(bbox.UpperLeftPoint.X, bbox.LowerRightPoint.Y, bbox.LowerRightPoint.X, bbox.UpperLeftPoint.Y);
        }

        private VectorTileLayer ProcessPolygonTile(ShapeFileFeatureLayer shapeFile, int tileX, int tileY, int zoom, OutputTileFeatureDelegate outputTileFeature)
        {
            //int tileSize = TileSize;
            //RectangleShape tileBounds = GetTileSphericalMercatorBounds(tileX, tileY, zoom, tileSize);

            ////create a buffer around the tileBounds 
            ////tileBounds.Inflate(tileBounds.Width * 0.05, tileBounds.Height * 0.05);
            //tileBounds.ScaleUp(5);

            //int simplificationFactor = Math.Min(10, Math.Max(1, SimplificationPixelThreshold));

            //System.Drawing.Point tilePixelOffset = new System.Drawing.Point((tileX * tileSize), (tileY * tileSize));

            //// List<int> indicies = new List<int>();
            //Collection<ThinkGeo.Core.Feature> features = shapeFile.QueryTools.GetFeaturesInsideBoundingBox(tileBounds, ReturningColumnsType.NoColumns);
            ////            shapeFile.GetShapeIndiciesIntersectingRect(indicies, tileBounds);
            ////GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
            ////{
            ////    XMin = -20,
            ////    YMin = -20,
            ////    XMax = tileSize + 20,
            ////    YMax = tileSize + 20
            ////};

            //List<System.Drawing.Point> clippedPolygon = new List<System.Drawing.Point>();


            //VectorTileLayer tileLayer = new VectorTileLayer();
            //tileLayer.Extent = (uint)tileSize;
            //tileLayer.Version = 2;
            //tileLayer.Name = !string.IsNullOrEmpty(shapeFile.Name) ? shapeFile.Name : System.IO.Path.GetFileNameWithoutExtension(shapeFile.FilePath);

            //// if (indicies.Count > 0)
            //if (features.Count > 0)
            //{
            //    foreach (ThinkGeo.Core.Feature index in features)
            //    {
            //        if (outputTileFeature != null && !outputTileFeature(shapeFile, index, zoom, tileX, tileY)) continue;

            //        VectorTileFeature feature = new VectorTileFeature()
            //        {
            //            //Id = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            //            Id = index.Id,
            //            Geometry = new List<List<Coordinate>>(),
            //            Attributes = new List<AttributeKeyValue>(),
            //            GeometryType = EGIS.Mapbox.Vector.Tile.Tile.GeomType.Polygon
            //        };

            //        //get the point data
            //        //  var recordPoints = shapeFile.GetShapeDataD(index);
            //        PolygonShape polygonShape = (PolygonShape)BaseShape.CreateShapeFromWellKnownData(index.GetWellKnownBinary());

            //        int partIndex = 0;
            //        //foreach (PointD[] points in recordPoints)
            //        foreach (PolygonShape a in polygonShape.r)
            //        {
            //            //convert to pixel coordinates;
            //            if (pixelPoints.Length < points.Length)
            //            {
            //                pixelPoints = new System.Drawing.Point[points.Length + 10];
            //                simplifiedPixelPoints = new System.Drawing.Point[points.Length + 10];
            //            }
            //            int pointCount = 0;
            //            for (int n = 0; n < points.Length; ++n)
            //            {
            //                Int64 x, y;
            //                TileUtil.LLToPixel2(points[n], zoom, tileX, tileY, out x, out y, tileSize);
            //                pixelPoints[pointCount].X = (int)(x);
            //                pixelPoints[pointCount++].Y = (int)(y);

            //            }
            //            ////check for duplicates points at end after they have been converted to pixel coordinates
            //            ////polygons need at least 3 points so don't reduce less than this
            //            //while(pointCount > 3 && (pixelPoints[pointCount-1] == pixelPoints[pointCount - 2]))
            //            //{
            //            //    --pointCount;
            //            //}

            //            int outputCount = 0;
            //            SimplifyPointData(pixelPoints, null, pointCount, simplificationFactor, simplifiedPixelPoints, null, ref pointsBuffer, ref outputCount);
            //            //simplifiedPixelPoints[outputCount++] = pixelPoints[pointCount-1];

            //            if (outputCount > 1)
            //            {
            //                GeometryAlgorithms.PolygonClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPolygon);

            //                if (clippedPolygon.Count > 0)
            //                {
            //                    //output the clipped polygon                                                                                             
            //                    List<Coordinate> lineString = new List<Coordinate>();
            //                    feature.Geometry.Add(lineString);
            //                    for (int i = clippedPolygon.Count - 1; i >= 0; --i)
            //                    {
            //                        lineString.Add(new Coordinate(clippedPolygon[i].X, clippedPolygon[i].Y));
            //                    }
            //                }
            //            }
            //            ++partIndex;
            //        }

            //        //add the record attributes
            //        string[] fieldNames = shapeFile.GetAttributeFieldNames();
            //        string[] values = shapeFile.GetAttributeFieldValues(index);
            //        for (int n = 0; n < values.Length; ++n)
            //        {
            //            feature.Attributes.Add(new AttributeKeyValue(fieldNames[n], values[n].Trim()));
            //        }

            //        if (feature.Geometry.Count > 0)
            //        {
            //            tileLayer.VectorTileFeatures.Add(feature);
            //        }
            //    }
            //}

            //return tileLayer;
            return null;
        }

        private VectorTileLayer ProcessPointTile(ShapeFileFeatureLayer shapeFile, int tileX, int tileY, int zoom, OutputTileFeatureDelegate outputTileFeature)
        {
            //int tileSize = TileSize;
            ////      RectangleD tileBounds = TileUtil.GetTileLatLonBounds(tileX, tileY, zoom, tileSize);
            //RectangleD tileBounds = TileUtil.GetTileSphericalMercatorBounds(tileX, tileY, zoom, tileSize);
            ////create a buffer around the tileBounds 
            //tileBounds.Inflate(tileBounds.Width * 0.05, tileBounds.Height * 0.05);

            //int simplificationFactor = Math.Min(10, Math.Max(1, SimplificationPixelThreshold));

            //System.Drawing.Point tilePixelOffset = new System.Drawing.Point((tileX * tileSize), (tileY * tileSize));

            //List<int> indicies = new List<int>();
            //shapeFile.GetShapeIndiciesIntersectingRect(indicies, tileBounds);
            //GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
            //{
            //    XMin = -20,
            //    YMin = -20,
            //    XMax = tileSize + 20,
            //    YMax = tileSize + 20
            //};


            //VectorTileLayer tileLayer = new VectorTileLayer();
            //tileLayer.Extent = (uint)tileSize;
            //tileLayer.Version = 2;
            //tileLayer.Name = !string.IsNullOrEmpty(shapeFile.Name) ? shapeFile.Name : System.IO.Path.GetFileNameWithoutExtension(shapeFile.FilePath);

            //if (indicies.Count > 0)
            //{
            //    foreach (int index in indicies)
            //    {
            //        if (outputTileFeature != null && !outputTileFeature(shapeFile, index, zoom, tileX, tileY)) continue;

            //        VectorTileFeature feature = new VectorTileFeature()
            //        {
            //            Id = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            //            Geometry = new List<List<Coordinate>>(),
            //            Attributes = new List<AttributeKeyValue>(),
            //            GeometryType = Tile.GeomType.Point
            //        };

            //        //output the pixel coordinates                                                                                             
            //        List<Coordinate> coordinates = new List<Coordinate>();
            //        //get the point data
            //        var recordPoints = shapeFile.GetShapeDataD(index);
            //        foreach (PointD[] points in recordPoints)
            //        {
            //            for (int n = 0; n < points.Length; ++n)
            //            {
            //                Int64 x, y;
            //                TileUtil.LLToPixel2(points[n], zoom, tileX, tileY, out x, out y, tileSize);
            //                coordinates.Add(new Coordinate((int)(x), (int)(y)));
            //            }
            //        }
            //        if (coordinates.Count > 0)
            //        {
            //            feature.Geometry.Add(coordinates);
            //        }

            //        //add the record attributes
            //        string[] fieldNames = shapeFile.GetAttributeFieldNames();
            //        string[] values = shapeFile.GetAttributeFieldValues(index);
            //        for (int n = 0; n < values.Length; ++n)
            //        {
            //            feature.Attributes.Add(new AttributeKeyValue(fieldNames[n], values[n].Trim()));
            //        }

            //        if (feature.Geometry.Count > 0)
            //        {
            //            tileLayer.VectorTileFeatures.Add(feature);
            //        }
            //    }
            //}

            // return tileLayer;
            return null;
        }


        private static void DouglasPeuckerReduction(PointShape[] points, Int32 firstPoint, Int32 lastPoint, Double tolerance,
            List<Int32> pointIndexsToKeep)
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

        public static double Dot(ref PointShape a, ref PointShape b, ref PointShape c)
        {
            PointShape ab = new PointShape(b.X - a.X, b.Y - a.Y);
            PointShape bc = new PointShape(c.X - b.X, c.Y - b.Y);
            return (ab.X * bc.X) + (ab.Y * bc.Y);
        }

        /// <summary>
        /// Computes the cross product AB x AC 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static double Cross(ref PointShape a, ref PointShape b, ref PointShape c)
        {
            PointShape ab = new PointShape(b.X - a.X, b.Y - a.Y);
            PointShape ac = new PointShape(c.X - a.X, c.Y - a.Y);
            return (ab.X * ac.Y) - (ab.Y * ac.X);
        }

        /// <summary>
        /// returns the Euclidean distance between two points
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Distance(ref PointShape a, ref PointShape b)
        {
            double d1 = a.X - b.X;
            double d2 = a.Y - b.Y;
            return Math.Sqrt((d1 * d1) + (d2 * d2));
        }


        public static double LineSegPointDist(ref PointShape a, ref PointShape b, ref PointShape c)
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

        public static int SimplifyDouglasPeucker(PointShape[] inputPoints, List<int> reducedPointIndicies, int inputPointCount, double tolerance)
        {
            reducedPointIndicies.Clear();
            if (inputPointCount < 3)
            {
                for (int i = 0; i < inputPointCount; ++i)
                {
                    reducedPointIndicies.Add(i);
                }
                return inputPointCount;
            }


            Int32 firstPoint = 0;
            Int32 lastPoint = inputPointCount - 1;

            //Add the first and last index to the keepers
            reducedPointIndicies.Add(firstPoint);
            reducedPointIndicies.Add(lastPoint);

            //ensure first and last point not the same
            while (lastPoint >= 0 && inputPoints[firstPoint].Equals(inputPoints[lastPoint]))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(inputPoints, firstPoint, lastPoint,
            tolerance, reducedPointIndicies);

            reducedPointIndicies.Sort();


            //if only two points check if both points the same
            if (reducedPointIndicies.Count == 2)
            {
                if (Math.Abs(inputPoints[reducedPointIndicies[0]].X - inputPoints[reducedPointIndicies[1]].X) < double.Epsilon && Math.Abs(inputPoints[reducedPointIndicies[0]].Y - inputPoints[reducedPointIndicies[1]].Y) < double.Epsilon) return 0;
            }


            return reducedPointIndicies.Count;
        }

        private void SimplifyPointData(System.Drawing.Point[] points, double[] measures, int pointCount, int simplificationFactor, System.Drawing.Point[] reducedPoints, double[] reducedMeasures, ref PointShape[] pointsBuffer, ref int reducedPointCount)
        {
            System.Drawing.Point endPoint = points[pointCount - 1];
            double endMeasure = measures != null ? measures[pointCount - 1] : 0;
            bool addEndpoint = endPoint == points[0];
            //check for duplicates points at end after they have been converted to pixel coordinates
            //polygons need at least 3 points so don't reduce less than this
            while (pointCount > 2 && (points[pointCount - 1] == points[0]))
            {
                --pointCount;
            }

            if (pointCount <= 2)
            {
                reducedPoints[0] = points[0];
                if (measures != null) reducedMeasures[0] = measures[0];
                if (pointCount == 2)
                {
                    reducedPoints[1] = points[1];
                    if (measures != null) reducedMeasures[1] = measures[1];
                }
                reducedPointCount = pointCount;
                if (addEndpoint)
                {
                    reducedPoints[reducedPointCount] = endPoint;
                    if (measures != null) reducedMeasures[reducedPointCount] = endMeasure;
                    ++reducedPointCount;
                }
                return;
            }
            if (pointsBuffer.Length < pointCount)
            {
                pointsBuffer = new ThinkGeo.Core.PointShape[pointCount];
            }

            for (int n = 0; n < pointCount; ++n)
            {
                pointsBuffer[n] = new PointShape();
                pointsBuffer[n].X = points[n].X;
                pointsBuffer[n].Y = points[n].Y;
            }
            List<int> reducedIndices = new List<int>();
            reducedPointCount = SimplifyDouglasPeucker(pointsBuffer, reducedIndices, pointCount, simplificationFactor);
            for (int n = 0; n < reducedPointCount; ++n)
            {
                reducedPoints[n] = points[reducedIndices[n]];
                if (measures != null) reducedMeasures[n] = measures[reducedIndices[n]];
            }
            if (addEndpoint)
            {
                reducedPoints[reducedPointCount] = endPoint;
                if (measures != null) reducedMeasures[reducedPointCount] = endMeasure;
                ++reducedPointCount;
            }
        }

        #endregion
    }

    class DoubleFormatConverter : JsonConverter
    {
        private string formatString = "{0:F2}";

        public DoubleFormatConverter(int decimalPlaces)
            : base()
        {
            decimalPlaces = Math.Max(0, decimalPlaces);
            formatString = "F" + decimalPlaces;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(double);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // writer.WriteRawValue($"{value:0.00}");
            //double doubleValue = (double)value;
            writer.WriteRawValue(((double)value).ToString(formatString, System.Globalization.CultureInfo.InvariantCulture));
        }

        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
