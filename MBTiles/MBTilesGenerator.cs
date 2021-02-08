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

namespace MBTiles
{
    public static partial class MBTilesGenerator
    {
        private static Dictionary<string, Feature> featureCache = new Dictionary<string, Feature>();
        private static object lockObject = new object();
        private static int cacheFeatureCount = 10000;

        public static Tile GetFirstTile(string mbtilesPath, int zoom)
        {
            var connection = new SqliteConnection($"Data Source={mbtilesPath}");
            connection.Open();

            string sqlStatement = $"SELECT * FROM tiles where zoom_level = {zoom} order by Id asc limit 1";
            SqliteCommand command = new SqliteCommand(sqlStatement, connection);
            SqliteDataReader dataReader = command.ExecuteReader();

            byte[] data = null;
            while (dataReader.Read())
            {
                data = (byte[])dataReader["tile_data"];
                break;
            }

            if (data == null)
                return null;

            byte[] unzippedData = UnzipGzippedData(data);
            MemoryStream ms = new MemoryStream(unzippedData);

            Tile tile = Tile.Deserialize(ms);
            return tile;
        }

        public static byte[] UnzipGzippedData(byte[] bytes)
        {
            byte[] unzippedBytes;

            using (MemoryStream gzippedStream = new MemoryStream(bytes))
            {
                using (GZipStream unzippedStream = new GZipStream(gzippedStream, CompressionMode.Decompress))
                {
                    using (MemoryStream returnedStream = new MemoryStream())
                    {
                        unzippedStream.CopyTo(returnedStream);
                        unzippedBytes = returnedStream.ToArray();
                    }
                }
            }

            return unzippedBytes;
        }

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

            string targetFolder = Path.Combine(Path.GetDirectoryName(targetMbtiles), Path.GetFileNameWithoutExtension(targetMbtiles), "-tmp");
            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
            double currentScale = GetZoomLevelIndex(zoomLevelSet, minZoom);
            var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, tileSize, tileSize, GeographyUnit.Meter);
            var tileRange = tileMatrix.GetIntersectingRowColumnRange(shapeFileBounds);
            List<Task> tasks = new List<Task>();
            for (long tileY = tileRange.MinRowIndex; tileY <= tileRange.MaxRowIndex && !cancellationToken.IsCancellationRequested; ++tileY)
            {
                for (long tileX = tileRange.MinColumnIndex; tileX <= tileRange.MaxColumnIndex && !cancellationToken.IsCancellationRequested; ++tileX)
                {
                    Task task = ProcessTileRecursive(shapeFile, (int)tileY, (int)tileX, minZoom, maxZoom, cancellationToken, targetFolder, includedAttributes);
                    tasks.Add(task);
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }

            await Task.Run(() =>
            {
                long index = 0;

                string[] files = Directory.GetFiles(targetFolder, "*.mvt");
                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string[] NameValues = fileName.Split('_');

                    byte[] bytes = File.ReadAllBytes(file);

                    TilesEntry newEntry = new TilesEntry();
                    int zoomLevel = int.Parse(NameValues[0]);
                    newEntry.ZoomLevel = zoomLevel;
                    long row = long.Parse(NameValues[1]);
                    row = (long)Math.Pow(2, zoomLevel) - row - 1;
                    newEntry.TileRow = row;
                    newEntry.TileColumn = long.Parse(NameValues[2]);
                    newEntry.TileId = index++;
                    newEntry.TileData = bytes;
                    File.Delete(file);

                    entries.Add(newEntry);

                    if (index % 1000 == 0)
                    {
                        targetMap.Insert(entries);
                        entries.Clear();
                        continue;
                    }
                }
                targetMap.Insert(entries);
            });

            targetDBConnection.Close();
        }

        private async static Task ProcessTileRecursive(FeatureLayer shapeFile, int tileX, int tileY, int zoom, int maxZoomLevel, CancellationToken cancellationToken, string targetFolder, List<string> includedAttributes = null)
        {
            Console.WriteLine($"Tile: {zoom}-{tileX}-{tileY}");
            if (cancellationToken.IsCancellationRequested) return;
            bool result = await ProcessTile(shapeFile, tileX, tileY, zoom, includedAttributes, targetFolder);

            if (result && zoom < maxZoomLevel)
            {
                List<Task> tasks = new List<Task>();

                tasks.Add(ProcessTileRecursive(shapeFile, tileX << 1, tileY << 1, zoom + 1, maxZoomLevel, cancellationToken, targetFolder, includedAttributes));
                tasks.Add(ProcessTileRecursive(shapeFile, (tileX << 1) + 1, tileY << 1, zoom + 1, maxZoomLevel, cancellationToken, targetFolder, includedAttributes));
                tasks.Add(ProcessTileRecursive(shapeFile, tileX << 1, (tileY << 1) + 1, zoom + 1, maxZoomLevel, cancellationToken, targetFolder, includedAttributes));
                tasks.Add(ProcessTileRecursive(shapeFile, (tileX << 1) + 1, (tileY << 1) + 1, zoom + 1, maxZoomLevel, cancellationToken, targetFolder, includedAttributes));
                foreach (var task in tasks)
                {
                    await task;
                }
            }
        }

        private async static Task<bool> ProcessTile(FeatureLayer shapeFile, int tileX, int tileY, int zoom, IEnumerable<string> columnNames, string targetFolder)
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
                        File.WriteAllBytes(Path.Combine(targetFolder, $"{zoom}_{tileX}_{tileY}.mvt"), gzippedContent);
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

        public static int GetZoom(ZoomLevelSet zoomLevelSet, double scale )
        {
            int zoom = -1;
            if (scale >= zoomLevelSet.ZoomLevel01.Scale)
                zoom = 0;
            else if (scale >= zoomLevelSet.ZoomLevel02.Scale)
                zoom = 1;
            else if (scale >= zoomLevelSet.ZoomLevel03.Scale)
                zoom = 2;
            else if (scale >= zoomLevelSet.ZoomLevel04.Scale)
                zoom = 3;
            else if (scale >= zoomLevelSet.ZoomLevel05.Scale)
                zoom = 4;
            else if (scale >= zoomLevelSet.ZoomLevel06.Scale)
                zoom = 5;
            else if (scale >= zoomLevelSet.ZoomLevel07.Scale)
                zoom = 6;
            else if (scale >= zoomLevelSet.ZoomLevel08.Scale)
                zoom = 7;
            else if (scale >= zoomLevelSet.ZoomLevel09.Scale)
                zoom = 8;
            else if (scale >= zoomLevelSet.ZoomLevel10.Scale)
                zoom = 9;
            else if (scale >= zoomLevelSet.ZoomLevel11.Scale)
                zoom = 10;
            else if (scale >= zoomLevelSet.ZoomLevel12.Scale)
                zoom = 11;
            else if (scale >= zoomLevelSet.ZoomLevel13.Scale)
                zoom = 12;
            else if (scale >= zoomLevelSet.ZoomLevel14.Scale)
                zoom = 13;
            else if (scale >= zoomLevelSet.ZoomLevel15.Scale)
                zoom = 14;
            else if (scale >= zoomLevelSet.ZoomLevel16.Scale)
                zoom = 15;
            else if (scale >= zoomLevelSet.ZoomLevel17.Scale)
                zoom = 16;
            else if (scale >= zoomLevelSet.ZoomLevel18.Scale)
                zoom = 17;
            else if (scale >= zoomLevelSet.ZoomLevel19.Scale)
                zoom = 18;
            else
                zoom = 19;
            return zoom;
        }

        private static TileFeature GetVectorTileFeature(Feature feature, int zoom, int tileSize, RectangleInt tileScreenBoundingBox, int simplificationFactor, RectangleShape tileBoundingBox)
        {
            TileFeature tileFeature = new TileFeature();
            switch (feature.GetWellKnownType())
            {
                case WellKnownType.Line:
                case WellKnownType.Multiline:
                    tileFeature.Type = GeometryType.LineString;
                    MultilineShape multiLineShape = new MultilineShape(feature.GetWellKnownBinary());
                    ProcessLineShape(zoom, tileSize, tileScreenBoundingBox, tileFeature, multiLineShape, simplificationFactor, tileBoundingBox);
                    break;
                case WellKnownType.Polygon:
                case WellKnownType.Multipolygon:
                    tileFeature.Type = GeometryType.Polygon;
                    MultipolygonShape multiPolygonShape = new MultipolygonShape(feature.GetWellKnownBinary());
                    foreach (PolygonShape polygonShape in multiPolygonShape.Polygons)
                    {
                        ProcessRingShape(zoom, tileSize, tileScreenBoundingBox, tileFeature, polygonShape.OuterRing, simplificationFactor, tileBoundingBox);
                        foreach (RingShape ringShape in polygonShape.InnerRings)
                        {
                            ProcessRingShape(zoom, tileSize, tileScreenBoundingBox, tileFeature, ringShape, simplificationFactor, tileBoundingBox);
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
            RectangleShape tileBoundingBox = GetTileSphericalMercatorBoundingBox(tileX, tileY, zoom, tileSize);
            RectangleShape scaledUpBoudingBox = new RectangleShape(tileBoundingBox.GetWellKnownBinary());
            scaledUpBoudingBox.ScaleUp(5);
            featureLayer.Open();

            Collection<string> allFeatureIds = featureLayer.FeatureSource.GetFeatureIdsInsideBoundingBox(scaledUpBoudingBox);
            RectangleInt tileScreenBoundingBox = new RectangleInt()
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
                List<string> featureIds = GetSubString(allFeatureIds, startIndex, Math.Min(1000, IdsCountToExecute));
                IdsCountToExecute = IdsCountToExecute - featureIds.Count;
                startIndex = startIndex + featureIds.Count;
                Collection<Feature> features = GetFeaturesByIds(featureLayer, featureIds, columnNames);

                foreach (Feature feature in features)
                {
                    TileFeature tileFeature = GetVectorTileFeature(feature, zoom, tileSize, tileScreenBoundingBox, simplificationFactor, tileBoundingBox);
                    if (tileFeature.Geometry.Count > 0)
                    {
                        tileLayer.Features.Add(tileFeature);
                    }
                }

            }
            tileLayer.FillInTheInternalProperties();
            return tileLayer;
        }

        private static Collection<Feature> GetFeaturesByIds(FeatureLayer featureLayer, List<string> featureIds, IEnumerable<string> columnNames)
        {
            List<Feature> result = new List<Feature>();
            for (int i = featureIds.Count - 1; i >= 0; i--)
            {
                if (featureCache.ContainsKey(featureIds[i]))
                {
                    result.Add(featureCache[featureIds[i]]);
                    featureIds.RemoveAt(i);
                }
            }

            Collection<Feature> features = featureLayer.FeatureSource.GetFeaturesByIds(featureIds, columnNames);

            if (featureCache.Count < cacheFeatureCount && features.Count > 0)
            {
                lock (lockObject)
                {
                    foreach (Feature feature in features)
                    {
                        if (!featureCache.ContainsKey(feature.Id))
                            featureCache.Add(feature.Id, feature);
                    }
                }
            }
            result.AddRange(features);
            return new Collection<Feature>(result);
        }

        private static void ProcessLineShape(int zoom, int tileSize, RectangleInt tileScreenBoundingBox, TileFeature tileFeature, MultilineShape multiLineShape, int simplificationFactor, RectangleShape tileBoundingBox)
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
                    ClipPolyline(simplifiedPixelPoints, tileScreenBoundingBox, clippedPoints, parts);
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

        private static void ProcessRingShape(int zoom, int tileSize, RectangleInt tileScreenBoundingBox, TileFeature tileFeature, RingShape ringShape, int simplificationFactor, RectangleShape tileBoundingBox)
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
                List<PointInt> clippedRing = ClipRingShape(simplifiedTilePoints, tileScreenBoundingBox);
                if (clippedRing.Count > 3)
                {
                    tileFeature.Geometry.Add(clippedRing);
                }
            }
        }

        private static PointInt[] SimplifyPointData(PointInt[] points, int simplificationFactor)
        {
            // Check for duplicates points at end after they have been converted to pixel coordinates(PointInt)
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

        private static RectangleShape GetTileSphericalMercatorBoundingBox(long tileX, long tileY, int zoomLevel, int tileSize = 256)
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

        private static List<string> GetSubString(Collection<string> sourceString, int startIndex, int count)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                result.Add(sourceString[startIndex + i]);
            }
            return result;
        }
    }
}
