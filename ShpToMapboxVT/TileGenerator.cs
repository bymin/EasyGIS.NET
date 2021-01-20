using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ThinkGeo.Core;
using static ShpToMapboxVT.MetadataTable;


/*
 * 
 * DISCLAIMER OF WARRANTY: THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING,
 * WITHOUT LIMITATION, WARRANTIES THAT THE SOFTWARE IS FREE OF DEFECTS, MERCHANTABLE, FIT FOR A PARTICULAR PURPOSE OR NON-INFRINGING.
 * THE ENTIRE RISK AS TO THE QUALITY AND PERFORMANCE OF THE SOFTWARE IS WITH YOU. SHOULD ANY COVERED CODE PROVE DEFECTIVE IN ANY RESPECT,
 * YOU (NOT CORPORATE EASY GIS .NET) ASSUME THE COST OF ANY NECESSARY SERVICING, REPAIR OR CORRECTION.  
 * 
 * LIABILITY: IN NO EVENT SHALL CORPORATE EASY GIS .NET BE LIABLE FOR ANY DAMAGES WHATSOEVER (INCLUDING, WITHOUT LIMITATION, 
 * DAMAGES FOR LOSS OF BUSINESS PROFITS, BUSINESS INTERRUPTION, LOSS OF INFORMATION OR ANY OTHER PECUNIARY LOSS)
 * ARISING OUT OF THE USE OF INABILITY TO USE THIS SOFTWARE, EVEN IF CORPORATE EASY GIS .NET HAS BEEN ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGES.
 * 
 * Copyright: Easy GIS .NET 2020
 *
 */
namespace ShpToMapboxVT
{
    /// <summary>
    /// Mapbox Vector Tile Generator
    /// </summary>
    /// <remarks>
    /// This class uses the EGIS.Mapbox.Vector.Tile library to generate Mapbox vector tiles from a shapefile
    /// </remarks>
    public class TileGenerator
    {

        private int startZoom, endZoom;

        public TileGenerator()
        {
            StartZoomLevel = 0;
            EndZoomLevel = 20;
            TileSize = 512;
            ExportAttributesToSeparateFile = true;
        }

        /// <summary>
        /// The start zoom level
        /// </summary>
        public int StartZoomLevel
        {
            get { return startZoom; }
            set
            {
                if (value < 0) throw new Exception("StartZoomLevel must be >= 0");
                if (value >= 50) throw new Exception("StartZoomLevel must be < 50");
                startZoom = value;
            }
        }

        /// <summary>
        /// the end zoom level (inclusive) to generate tiles
        /// </summary>
        public int EndZoomLevel
        {
            get { return endZoom; }
            set
            {
                if (value < 0) throw new Exception("EndZoomLevel must be >= 0");
                if (value >= 50) throw new Exception("EndZoomLevel must be < 50");
                endZoom = value;
            }
        }

        /// <summary>
        /// The size of the vector tiles. Default is 512
        /// </summary>
        /// <remarks>
        /// TileSize should be a power of 2 (256,512,1024 etc.)</remarks>
        public int TileSize
        {
            get;
            set;
        }

        /// <summary>
        /// Full path to the output directory where tiles will be saved
        /// </summary>
        public string BaseOutputDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// whether to export shapefile attributes to separate file or include in each vector feature
        /// </summary>
        public bool ExportAttributesToSeparateFile
        {
            get;
            set;
        }

    
        public event EventHandler<StatusMessageEventArgs> StatusMessage;

        protected void OnStatusMessage(StatusMessageEventArgs args)
        {
            if (StatusMessage != null)
            {
                StatusMessage(this, args);
            }
        }

        /// <summary>
        /// Process given shapefile and generate Mapbox mvt vector tiles
        /// </summary>
        /// <param name="shapeFileName">full path to the input shapefile to process</param>        
        /// <param name="includedAttributes">List of attributes to export. If null all attributes will be output</param>
        public void Process(string shapeFileName, System.Threading.CancellationToken cancellationToken, List<string> includedAttributes = null)
        {

            ShapeFileFeatureLayer shapeFile = new ShapeFileFeatureLayer(shapeFileName);
            {
                string metadataPath = System.IO.Path.Combine(BaseOutputDirectory,
                    System.IO.Path.GetFileNameWithoutExtension(shapeFileName) + "_metadata.json");
                
                OutputMetadata(shapeFile, metadataPath, includedAttributes, ExportAttributesToSeparateFile);

                OnStatusMessage(new StatusMessageEventArgs("Output metadata"));

                ////is the shapefile using geographic coordinates
                //var wgs84CRS = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84EpsgCode);

                //string wgs84ShapeFileName = null;
                //ShapeFile wgs84ShapeFile = null;
                //try
                //{
                    //if (!wgs84CRS.IsEquivalent(shapeFile.CoordinateReferenceSystem))
                    //{
                    //    OnStatusMessage(new StatusMessageEventArgs("Transforming shapefile CRS to Wgs84"));
                    //    //convert the shapefile to WGS84
                    //    wgs84ShapeFileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".shp");
                    //    TransformShapeFileCRS(shapeFileName, wgs84ShapeFileName, wgs84CRS);

                    //    OnStatusMessage(new StatusMessageEventArgs("Shapefile CRS succesfully coverted to Wgs84"));

                    //    wgs84ShapeFile = new ShapeFile(wgs84ShapeFileName);
                    //} 

                    EGIS.Web.Controls.VectorTileGenerator generator = new EGIS.Web.Controls.VectorTileGenerator()
                    {
                        TileSize = TileSize,
                        SimplificationPixelThreshold = 1
                    };

                    //Process(wgs84ShapeFile != null ? wgs84ShapeFile : shapeFile, generator, ExportAttributesToSeparateFile, cancellationToken, includedAttributes);
                    Process(shapeFile, generator, ExportAttributesToSeparateFile, cancellationToken, includedAttributes);
                //}
                //finally
                //{
                //    if (wgs84ShapeFile != null)
                //    {
                //        wgs84ShapeFile.Dispose();
                //        try
                //        {
                //            System.IO.File.Delete(wgs84ShapeFileName);
                //            System.IO.File.Delete(System.IO.Path.ChangeExtension(wgs84ShapeFileName, ".shx"));
                //            System.IO.File.Delete(System.IO.Path.ChangeExtension(wgs84ShapeFileName, ".dbf"));
                //            System.IO.File.Delete(System.IO.Path.ChangeExtension(wgs84ShapeFileName, ".prj"));
                //        }
                //        catch { }
                //    }
                //}


            }
        }

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

        private void Process(ShapeFileFeatureLayer shapeFile, EGIS.Web.Controls.VectorTileGenerator generator, bool exportAttributesToSeparateFile, System.Threading.CancellationToken cancellationToken,  List<string> includedAttributes = null)
        {
            int zoom = Math.Max(StartZoomLevel, 0);
            int endZoomLevel = Math.Min(Math.Max(zoom, EndZoomLevel), 49);
            int tileSize = TileSize;

            Console.Out.WriteLine("Processing tiles. StartZoom:{0}, EndZoom:{1}", zoom, endZoomLevel);

            if (!System.IO.Directory.Exists(BaseOutputDirectory))
            {
                System.IO.Directory.CreateDirectory(BaseOutputDirectory);
            }

            
            RectangleShape shapeFileBounds = shapeFile.GetBoundingBox();
            Console.Out.WriteLine(shapeFileBounds);

         
            // -------------------------------------------------------------------------------------
            SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
            //double currentScale = zoomLevelSet.CustomZoomLevels[zoomLevel].Scale;
            double currentScale = GetZoomLevelIndex(zoomLevelSet, zoom);

            var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, tileSize, tileSize, GeographyUnit.Meter);
          //  RectangleShape boundingBox = new RectangleShape(shapeFileBounds.Left, shapeFileBounds.Top, shapeFileBounds.Right, shapeFileBounds.Bottom);
            var tileRange = tileMatrix.GetIntersectingRowColumnRange(shapeFileBounds);

            processTileCount = totalDataTileCount = tileSpeedCount = 0;
            processingStartTime = DateTime.Now;
            tileSpeedStartTime = DateTime.Now;

            //OnStatusMessage(new StatusMessageEventArgs(string.Format("Top Left Tile:{0}, Bottom Right Tile:{1}", topLeftTile, bottomRightTile)));
            //OnStatusMessage(new StatusMessageEventArgs(string.Format("Max Tiles at zoom level {0} is {1}", EndZoomLevel, 1 << EndZoomLevel)));

            for (long tileY = tileRange.MinRowIndex; tileY <= tileRange.MaxRowIndex && !cancellationToken.IsCancellationRequested; ++tileY)
            {
                for (long tileX = tileRange.MinColumnIndex; tileX <= tileRange.MaxColumnIndex && !cancellationToken.IsCancellationRequested; ++tileX)
                {
                    ProcessTileRecursive(shapeFile, (int)tileY, (int)tileX, zoom, endZoomLevel, generator, exportAttributesToSeparateFile, cancellationToken, includedAttributes);
                }
            }
            // ----------------------------------------------------------------------

            //var topLeftTile = TileUtil.GetTileFromGisLocation(shapeFileBounds.Left, shapeFileBounds.Bottom, zoom, tileSize);
            //var bottomRightTile = TileUtil.GetTileFromGisLocation(shapeFileBounds.Right, shapeFileBounds.Top, zoom, tileSize);

            //processTileCount = totalDataTileCount = tileSpeedCount = 0;
            //processingStartTime = DateTime.Now;
            //tileSpeedStartTime = DateTime.Now;

            //OnStatusMessage(new StatusMessageEventArgs( string.Format("Top Left Tile:{0}, Bottom Right Tile:{1}", topLeftTile, bottomRightTile)));
            //OnStatusMessage(new StatusMessageEventArgs(string.Format("Max Tiles at zoom level {0} is {1}", EndZoomLevel, 1 << EndZoomLevel)));

            //for (int tileY = topLeftTile.Y; tileY <= bottomRightTile.Y && !cancellationToken.IsCancellationRequested ; ++tileY)
            //{
            //    for (int tileX = topLeftTile.X; tileX <= bottomRightTile.X && !cancellationToken.IsCancellationRequested;  ++tileX)
            //    {
            //        ProcessTileRecursive(shapeFile, tileX, tileY, zoom, endZoomLevel, generator, exportAttributesToSeparateFile, cancellationToken, includedAttributes);
            //    }
            //}

            if (tileSpeedCount >= 1000)
            {
                DateTime tick = DateTime.Now;
                double elapsedSeconds = tick.Subtract(tileSpeedStartTime).TotalSeconds;
                OnStatusMessage(new StatusMessageEventArgs(string.Format("total tiles processed:{0}, total data tiles:{1}, speed={2:0.00} tiles/second", processTileCount, totalDataTileCount, tileSpeedCount / elapsedSeconds)));                
            }

        }

        private int processTileCount = 0;
        private int totalDataTileCount = 0;
        private int tileSpeedCount = 0;
        private DateTime tileSpeedStartTime = DateTime.Now;
        private DateTime processingStartTime = DateTime.Now;

        private void ProcessTileRecursive(ShapeFileFeatureLayer shapeFile, int tileX, int tileY, int zoom, int maxZoomLevel, EGIS.Web.Controls.VectorTileGenerator generator, bool exportAttributesToSeparateFile, System.Threading.CancellationToken cancellationToken, List<string> includedAttributes = null)
        {
            if (cancellationToken.IsCancellationRequested) return;
            bool result = ProcessTile(shapeFile, tileX, tileY, zoom, generator, exportAttributesToSeparateFile, includedAttributes);


            ++tileSpeedCount;
            if (tileSpeedCount >= 1000)
            {
                DateTime tick = DateTime.Now;
                double elapsedSeconds = tick.Subtract(tileSpeedStartTime).TotalSeconds;

                OnStatusMessage(new StatusMessageEventArgs(string.Format("total tiles processed:{0}, total data tiles:{1}, speed={2:0.00} tiles/second", processTileCount, totalDataTileCount, tileSpeedCount / elapsedSeconds)));
                tileSpeedCount = 0;
                tileSpeedStartTime = tick;

            }


            if (result && zoom < maxZoomLevel)
            {
                ProcessTileRecursive(shapeFile, tileX << 1, tileY << 1, zoom + 1, maxZoomLevel, generator, exportAttributesToSeparateFile, cancellationToken, includedAttributes);
                ProcessTileRecursive(shapeFile, (tileX << 1) + 1, tileY << 1, zoom + 1, maxZoomLevel, generator, exportAttributesToSeparateFile, cancellationToken, includedAttributes);
                ProcessTileRecursive(shapeFile, tileX << 1, (tileY << 1) + 1, zoom + 1, maxZoomLevel, generator, exportAttributesToSeparateFile, cancellationToken,includedAttributes);
                ProcessTileRecursive(shapeFile, (tileX << 1) + 1, (tileY << 1) + 1, zoom + 1, maxZoomLevel, generator, exportAttributesToSeparateFile, cancellationToken,includedAttributes);
            }
        }

        private bool ProcessTile(ShapeFileFeatureLayer shapeFile, int tileX, int tileY, int zoom, EGIS.Web.Controls.VectorTileGenerator generator,
            bool exportAttributesToSeparateFile, List<string> includedAttributes = null)
        {
            ++processTileCount;
            List<ShapeFileFeatureLayer> layers = new List<ShapeFileFeatureLayer>();
            layers.Add(shapeFile);
            var vectorTile = generator.Generate(tileX, tileY, zoom, layers);
            if (vectorTile != null && vectorTile.Count > 0)
            {
                foreach (var layer in vectorTile)
                {
                    if (exportAttributesToSeparateFile)
                    {
                        foreach (var feature in layer.VectorTileFeatures)
                        {
                            feature.Attributes.Clear();
                        }
                    }
                }                
                using (System.IO.FileStream fs = new System.IO.FileStream(GetTileName(tileX, tileY, zoom), System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
                {
                    EGIS.Mapbox.Vector.Tile.VectorTileParser.Encode(vectorTile, fs);
                }
                ++totalDataTileCount;
                return true;
            }
            return false;
        }




        protected string GetTileName(int tileX, int tileY, int zoom)
        {
            return System.IO.Path.Combine(this.BaseOutputDirectory, string.Format("{0}_{1}_{2}.mvt", zoom, tileX, tileY));

        }



        /// <summary>
        ///// Transforms the coordinates a shapefile and writes to a new shapefile
        ///// </summary>
        ///// <param name="sourceShapeFileName">full path to the source shapefile</param>
        ///// <param name="targetShapeFileName">full path to the target shapefile that will be created</param>
        ///// <param name="targetCRS">target Coordinate Reference System</param>
        ///// <param name="restrictToAreaOfUse">flag to indicate whether the source coordinates should be restricted to the targetCRS area of use before transforming coordinates</param>
        //private void TransformShapeFileCRS(string sourceShapeFileName, string targetShapeFileName, ICRS targetCRS)
        //{
        //    int currentPercent = 0;
        //    using (ShapeFile sourceShapeFile = new ShapeFile(sourceShapeFileName))
        //    {                                
        //        //create a ICoordinateTransformation to transform coordinates from source shapefiles CRS to the target CRS
        //        using (ICoordinateTransformation coordinateTransformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(sourceShapeFile.CoordinateReferenceSystem, targetCRS))
        //        using (ShapeFileWriter writer = ShapeFileWriter.CreateWriter(System.IO.Path.GetDirectoryName(targetShapeFileName),
        //                System.IO.Path.GetFileNameWithoutExtension(targetShapeFileName),
        //                sourceShapeFile.ShapeType,
        //                sourceShapeFile.RenderSettings.DbfReader.DbfRecordHeader.GetFieldDescriptions()))
        //        {
        //            for (int n = 0; n < sourceShapeFile.RecordCount; ++n)
        //            {
        //                var shapeData = sourceShapeFile.GetShapeDataD(n);                            
        //                foreach (var part in shapeData)
        //                {
        //                    coordinateTransformation.Transform(part);
        //                }
        //                string[] attributes = sourceShapeFile.GetAttributeFieldValues(n);
        //                if (sourceShapeFile.ShapeType == ShapeType.PolyLineM)
        //                {
        //                    var measureData = sourceShapeFile.GetShapeMDataD(n);
        //                    writer.AddRecord(shapeData, measureData, attributes);
        //                }
        //                else
        //                {
        //                    writer.AddRecord(shapeData, attributes);
        //                }
        //                //update the progress
        //                int percent = (int)Math.Round(100 * (double)(n + 1) / (double)sourceShapeFile.RecordCount);
        //                if (percent != currentPercent)
        //                {
        //                    currentPercent = percent;                                
        //                }
        //            }
        //        }
                

        //        //write the .prj file
        //        using (System.IO.StreamWriter writer = new System.IO.StreamWriter(System.IO.Path.ChangeExtension(targetShapeFileName, ".prj")))
        //        {
        //            writer.WriteLine(targetCRS.WKT);
        //        }
        //    }
        //}



        #region process attributes

        /// <summary>
        /// Returns zero-based index of a given field name in an array of field names
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="fieldName"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static int IndexOfField(string[] fields, string fieldName, bool ignoreCase)
        {
            if (fields == null || string.IsNullOrEmpty(fieldName)) return -1;
            int index;
            for (index = fields.Length - 1; index >= 0; --index)
            {
                if (ignoreCase)
                {
                    if (string.Compare(fields[index], fieldName, StringComparison.OrdinalIgnoreCase) == 0) return index;
                }
                else
                {
                    if (string.Compare(fields[index], fieldName, StringComparison.Ordinal) == 0) return index;
                }
            }
            return index;
        }


        private void OutputMetadata(ShapeFileFeatureLayer shapeFile, string fileName, List<string> includedAttributes, bool outputAttributeValues)
        {
            shapeFile.Open();
            //string[] attributeNames = shapeFile.GetAttributeFieldNames();
            string[] attributeNames  = shapeFile.QueryTools.GetColumns().Select(f => f.ColumnName).ToArray();
            List<int> includedAttributeIndicies = null;
            Metadata metadata = new Metadata();
            metadata.TileSize = this.TileSize;
            metadata.StartZoomLevel = this.StartZoomLevel;
            metadata.EndZoomLevel = this.EndZoomLevel;
            if (includedAttributes == null)
            {
                metadata.AttrKeys.AddRange(attributeNames);
            }
            else
            {
                includedAttributeIndicies = new List<int>();
                foreach (string name in includedAttributes)
                {
                    int index = IndexOfField(attributeNames, name, true);
                    if (index < 0) throw new Exception(string.Format("field {0} from includedAttributes not found in shapefile DBF attributes", name));
                    includedAttributeIndicies.Add(index);
                }
                metadata.AttrKeys.AddRange(includedAttributes);
            }
            //if (outputAttributeValues)
            //{
            //    for (int n = 0; n < shapeFile.QueryTools.GetCount(); ++n)
            //    {
            //        Record record = new Record()
            //        {
            //            Id = n
            //        };
            //        string[] attributeValues = shapeFile.GetAttributeFieldValues(n);
            //        string[] attributeValues = shapeFile.QueryTools.GetFeatureById((n + 1).ToString(), ).Select(f => f.).ToArray();
            //        EGIS.ShapeFileLib.CsvUtil.TrimValues(attributeValues);
            //        if (includedAttributes == null)
            //        {
            //            record.AttrValues.AddRange(attributeValues);
            //        }
            //        else
            //        {
            //            foreach (int index in includedAttributeIndicies)
            //            {
            //                record.AttrValues.Add(attributeValues[index]);
            //            }
            //        }

            //        metadata.Records.Add(record);
            //    }
            //}

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(metadata);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName))
            {
                writer.WriteLine(json);
            }
        }


        #endregion

    }

    public class StatusMessageEventArgs : EventArgs
    {
        public StatusMessageEventArgs(string message) :
            base()
        {
            Status = message;
        }

        public string Status
        {
            get;set;
        }
    }

    class Metadata
    {
        public int TileSize;
        public int StartZoomLevel;
        public int EndZoomLevel;
        public List<string> AttrKeys = new List<string>();
        public List<Record> Records = new List<Record>();
    }


    class Record
    {
        public int Id;
        //public List<KeyValue> Attributes = new List<KeyValue>();
        public List<string> AttrValues = new List<string>();
    }
}
