using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThinkGeo.Core;

namespace MBTilesGenerator
{
    public static class VectorTileGenerator
    {
        /// <summary>
        /// Process given shapefile and generate 
        /// </summary>
        /// <param name="shapeFileName">full path to the input shapefile to process</param>        
        /// <param name="includedAttributes">List of attributes to export. If null all attributes will be output</param>
        public async static Task Process(string shapeFileName, string targetMbTiles, CancellationToken cancellationToken, int minZoom, int maxZoom, int tileSize, List<string> includedAttributes = null)
        {
            if (File.Exists(targetMbTiles))
                File.Delete(targetMbTiles);

            ShapeFileFeatureLayer shapeFile = new ShapeFileFeatureLayer(shapeFileName);
            shapeFile.Open();
            shapeFile.Name = Path.GetFileNameWithoutExtension(shapeFile.ShapePathFilename);

            if (shapeFile.Projection != null)
            {
                shapeFile.FeatureSource.ProjectionConverter = new ProjectionConverter(shapeFile.Projection.ProjString, 3857);
                shapeFile.FeatureSource.ProjectionConverter.Open();
            }

            await Process(shapeFile, targetMbTiles, cancellationToken, minZoom, maxZoom, tileSize, includedAttributes);
        }

        private async static Task Process(ShapeFileFeatureLayer shapeFile, string targetMbtiles, CancellationToken cancellationToken, int minZoom, int maxZoom, int tileSize, List<string> includedAttributes = null)
        {
            Console.Out.WriteLine("Processing tiles. StartZoom:{0}, EndZoom:{1}", minZoom, maxZoom);

            RectangleShape shapeFileBounds = shapeFile.GetBoundingBox();
            shapeFile.Close();

            ThinkGeoMBTilesLayer.CreateDatabase(targetMbtiles);
            var targetDBConnection = new SqliteConnection($"Data Source={targetMbtiles}");
            targetDBConnection.Open();

            // Meta Table
            var targetMetadata = new MetadataTable(targetDBConnection);

            PointShape centerPoint = shapeFileBounds.GetCenterPoint();
            string center = $"{centerPoint.X},{centerPoint.Y},{maxZoom}";
            string bounds = $"{shapeFileBounds.UpperLeftPoint.X},{shapeFileBounds.UpperLeftPoint.Y},{shapeFileBounds.LowerRightPoint.X},{shapeFileBounds.LowerRightPoint.Y}";

            List<MetadataEntry> Entries = new List<MetadataEntry>();
            Entries.Add(new MetadataEntry() { Name = "name", Value = "ThinkGeo World Streets" });
            Entries.Add(new MetadataEntry() { Name = "format", Value = "pbf" });
            Entries.Add(new MetadataEntry() { Name = "bounds", Value = bounds }); //"-96.85310250357627,33.10809235525063,-96.85260897712004,33.107616047247156"
            Entries.Add(new MetadataEntry() { Name = "center", Value = center }); // "-96.85285574034816,33.1078542012489,14"
            Entries.Add(new MetadataEntry() { Name = "minzoom", Value = $"{minZoom}" });
            Entries.Add(new MetadataEntry() { Name = "maxzoom", Value = $"{maxZoom}" });
            Entries.Add(new MetadataEntry() { Name = "attribution", Value = "Copyright @2020 ThinkGeo LLC.All rights reserved." });
            Entries.Add(new MetadataEntry() { Name = "description", Value = "ThinkGeo World Street Vector Tile Data in EPSG:3857" });
            Entries.Add(new MetadataEntry() { Name = "version", Value = "2.0" });
            Entries.Add(new MetadataEntry() { Name = "json", Value = "" });
            targetMetadata.Insert(Entries);

            // Tile Table
            var targetMap = new TilesTable(targetDBConnection);
            List<TilesEntry> entries = new List<TilesEntry>();

            SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
            double currentScale = GetZoomLevelIndex(zoomLevelSet, minZoom);
            var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, tileSize, tileSize, GeographyUnit.Meter);
            var tileRange = tileMatrix.GetIntersectingRowColumnRange(shapeFileBounds);
            List<Task> tasks = new List<Task>();
            for (long tileY = tileRange.MinRowIndex; tileY <= tileRange.MaxRowIndex && !cancellationToken.IsCancellationRequested; ++tileY)
            {
                for (long tileX = tileRange.MinColumnIndex; tileX <= tileRange.MaxColumnIndex && !cancellationToken.IsCancellationRequested; ++tileX)
                {
                    Task task = ProcessTileRecursive(shapeFile, (int)tileY, (int)tileX, minZoom, maxZoom, cancellationToken, entries, targetMap, includedAttributes);
                    tasks.Add(task);
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }

            targetMap.Insert(entries);

            //if (tileSpeedCount >= 1000)
            //{
            //    DateTime tick = DateTime.Now;
            //    double elapsedSeconds = tick.Subtract(tileSpeedStartTime).TotalSeconds;
            //    //OnStatusMessage(new StatusMessageEventArgs(string.Format("total tiles processed:{0}, total data tiles:{1}, speed={2:0.00} tiles/second", processTileCount, totalDataTileCount, tileSpeedCount / elapsedSeconds)));
            //}
        }

        private async static Task ProcessTileRecursive(FeatureLayer shapeFile, int tileX, int tileY, int zoom, int maxZoomLevel, CancellationToken cancellationToken, List<TilesEntry> entries, TilesTable targetMap, List<string> includedAttributes = null)
        {
            Console.WriteLine($"Tile: {zoom}-{tileX}-{tileY}");
            if (cancellationToken.IsCancellationRequested) return;
            bool result = await ProcessTile(shapeFile, tileX, tileY, zoom, includedAttributes, entries, targetMap);

            if (result && zoom < maxZoomLevel)
            {
                List<Task> tasks = new List<Task>();

                tasks.Add(ProcessTileRecursive(shapeFile, tileX << 1, tileY << 1, zoom + 1, maxZoomLevel, cancellationToken, entries, targetMap, includedAttributes));
                tasks.Add(ProcessTileRecursive(shapeFile, (tileX << 1) + 1, tileY << 1, zoom + 1, maxZoomLevel, cancellationToken, entries, targetMap, includedAttributes));
                tasks.Add(ProcessTileRecursive(shapeFile, tileX << 1, (tileY << 1) + 1, zoom + 1, maxZoomLevel, cancellationToken, entries, targetMap, includedAttributes));
                tasks.Add(ProcessTileRecursive(shapeFile, (tileX << 1) + 1, (tileY << 1) + 1, zoom + 1, maxZoomLevel, cancellationToken, entries, targetMap, includedAttributes));
                foreach (var task in tasks)
                {
                    await task;
                }
            }
        }

        private async static Task<bool> ProcessTile(FeatureLayer shapeFile, int tileX, int tileY, int zoom, IEnumerable<string> columnNames, List<TilesEntry> entries, TilesTable targetMap)
        {
            List<FeatureLayer> layers = new List<FeatureLayer>();
            FeatureLayer layer = (FeatureLayer)shapeFile.CloneDeep();
            layers.Add(layer);
            Tile vectorTile = new Tile();
            await Task.Run(() =>
            {
                vectorTile = Generate(tileX, tileY, zoom, layers, columnNames, 512, 1);
            });
            if (vectorTile != null && vectorTile.Layers.Count > 0)
            {
                await Task.Run(() =>
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        vectorTile.Serialize(ms);
                        byte[] content = ms.ToArray();
                        byte[] gzippedContent = GZipData(content);

                        TilesEntry newEntry = new TilesEntry();
                        newEntry.ZoomLevel = zoom;
                        newEntry.TileRow = (long)Math.Pow(2, zoom) - tileX - 1;
                        newEntry.TileColumn = tileY;
                        newEntry.TileData = gzippedContent;

                        entries.Add(newEntry);

                        if (entries.Count > 100)
                        {
                            targetMap.Insert(entries);
                            entries.Clear();
                        }
                    }
                });
                return true;
            }
            return false;
        }

        private static byte[] GZipData(byte[] bytes)
        {
            byte[] zippedBytes;
            using (MemoryStream zippedMemoryStream = new MemoryStream())
            {
                using (GZipStream zippedStream = new GZipStream(zippedMemoryStream, CompressionMode.Compress))
                {
                    zippedStream.Write(bytes, 0, bytes.Length);
                    zippedStream.Close();
                    zippedBytes = zippedMemoryStream.ToArray();

                }
            }
            return zippedBytes;
        }

        /// <summary>
        /// Generates a Vector Tile from ShapeFile layers
        /// </summary>
        /// <param name="tileX">Tile X coordinate</param>
        /// <param name="tileY">Tile Y coordinate</param>
        /// <param name="zoomLevel">Tile zoom level</param>
        /// <param name="featureLayers">List of Feature layers</param>
        /// <param name="columnNames">The column Names will be included in the result mbtile.</param>
        /// <param name="simplificationFactor"></param>
        /// <param name="tileSize">Tile Size</param>
        /// <returns></returns>
        private static Tile Generate(int tileX, int tileY, int zoomLevel, List<FeatureLayer> featureLayers, IEnumerable<string> columnNames, int tileSize, int simplificationFactor)
        {
            Tile tile = new Tile();

            foreach (FeatureLayer featureLayer in featureLayers)
            {
                var layer = ProcessTile(featureLayer, tileX, tileY, zoomLevel, columnNames, simplificationFactor, tileSize);
                if (layer.Features != null && layer.Features.Count > 0)
                {
                    tile.Layers.Add(layer);
                }
            }

            return tile;
        }

        private static double GetZoomLevelIndex(ZoomLevelSet zoomLevelSet, int zoomLevel)
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

        #region private members

        private static TileFeature GetVectorTileFeature(Feature feature, int zoom, int tileSize, RectangleInt clipBounds, int simplificationFactor, RectangleShape tileBoundingBox)
        {
            TileFeature tileFeature = new TileFeature();
            switch (feature.GetWellKnownType())
            {
                case WellKnownType.Line:
                case WellKnownType.Multiline:
                    tileFeature.Type = GeometryType.LineString;
                    MultilineShape multiLineShape = new MultilineShape(feature.GetWellKnownBinary());
                    ProcessLineShape(zoom, tileSize, clipBounds, tileFeature, multiLineShape, simplificationFactor, tileBoundingBox);
                    break;
                case WellKnownType.Polygon:
                case WellKnownType.Multipolygon:
                    tileFeature.Type = GeometryType.Polygon;
                    MultipolygonShape multiPolygonShape = new MultipolygonShape(feature.GetWellKnownBinary());
                    foreach (PolygonShape polygonShape in multiPolygonShape.Polygons)
                    {
                        ProcessRingShape(zoom, tileSize, clipBounds, tileFeature, polygonShape.OuterRing, simplificationFactor, tileBoundingBox);
                        foreach (RingShape ringShape in polygonShape.InnerRings)
                        {
                            ProcessRingShape(zoom, tileSize, clipBounds, tileFeature, ringShape, simplificationFactor, tileBoundingBox);
                        }
                    }
                    break;
                case WellKnownType.Point:
                case WellKnownType.Multipoint:
                    tileFeature.Type = GeometryType.Point;
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
                        PointInt pointI = WorldPointToTilePoint(point.X, point.Y, zoom, tileSize, tileBoundingBox);
                        coordinates.Add(new PointInt(pointI.X, pointI.Y));
                    }
                    if (coordinates.Count > 0)
                    {
                        tileFeature.Geometry.Add(coordinates);
                    }
                    break;
                default:
                    tileFeature.Type = GeometryType.Unknown;
                    break;
            }

            //add the record attributes
            foreach (var attributes in feature.ColumnValues)
            {
                tileFeature.Attributes.Add(new TileAttribute(attributes.Key, attributes.Value));
            }

            return tileFeature;
        }

        private static TileLayer ProcessTile(FeatureLayer featureLayer, int tileX, int tileY, int zoom, IEnumerable<string> columnNames, int simplificationFactor, int tileSize)
        {
            RectangleShape tileBoundingBox = GetTileSphericalMercatorBounds(tileX, tileY, zoom, tileSize);
            RectangleShape scaledUpBoudingBox = new RectangleShape(tileBoundingBox.GetWellKnownBinary());
            scaledUpBoudingBox.ScaleUp(5);
            featureLayer.Open();

            Collection<string> allFeatureIds = featureLayer.FeatureSource.GetFeatureIdsInsideBoundingBox(scaledUpBoudingBox);
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
                    TileFeature tileFeature = GetVectorTileFeature(feature, zoom, tileSize, clipBounds, simplificationFactor, tileBoundingBox);
                    if (tileFeature.Geometry.Count > 0)
                    {
                        tileLayer.Features.Add(tileFeature);
                    }
                }
                IdsCountToExecute = IdsCountToExecute - featureIds.Length;
                startIndex = startIndex + featureIds.Length;
            }
            tileLayer.FillInTheInternalProperties();
            return tileLayer;
        }

        private static void ProcessLineShape(int zoom, int tileSize, RectangleInt clipBounds, TileFeature tileFeature, MultilineShape multiLineShape, int simplificationFactor, RectangleShape tileBoundingBox)
        {
            foreach (LineShape line in multiLineShape.Lines)
            {
                PointInt[] pixelPoints = new PointInt[line.Vertices.Count];
                for (int n = 0; n < line.Vertices.Count; ++n)
                {
                    pixelPoints[n] = WorldPointToTilePoint(line.Vertices[n].X, line.Vertices[n].Y, zoom, tileSize, tileBoundingBox);
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

        private static void ProcessRingShape(int zoom, int tileSize, RectangleInt clipBounds, TileFeature tileFeature, RingShape ringShape, int simplificationFactor, RectangleShape tileBoundingBox)
        {
            PointInt[] tilePoints = new PointInt[ringShape.Vertices.Count];
            for (int n = 0; n < ringShape.Vertices.Count; ++n)
            {
                tilePoints[n] = WorldPointToTilePoint(ringShape.Vertices[n].X, ringShape.Vertices[n].Y, zoom, tileSize, tileBoundingBox);
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

        private static PointInt WorldPointToTilePoint(double pointX, double pointY, int zoomLevel, int tileSize, RectangleShape tileBoundingBox)
        {
            double scale = ((double)tileSize / MaxExtents.SphericalMercator.Width) * (1 << zoomLevel);
            PointInt result = new PointInt()
            {
                X = (int)Math.Round((pointX - tileBoundingBox.LowerLeftPoint.X) * scale),
                Y = (int)Math.Round((tileBoundingBox.UpperLeftPoint.Y - pointY) * scale)
            };

            return result;
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

        [Flags]
        enum OutCode { Inside = 0, Left = 1, Right = 2, Bottom = 4, Top = 8 };

        [Flags]
        enum ClipState { None = 0, Start = 1, End = 2 };
        #endregion
    }
}
