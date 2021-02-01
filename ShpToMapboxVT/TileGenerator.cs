using EGIS.Mapbox.Vector.Tile;
using EGIS.Web.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ThinkGeo.Core;


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
        private int startZoom;
        private int endZoom;
        private int processTileCount = 0;
        private int totalDataTileCount = 0;
        private int tileSpeedCount = 0;
        private DateTime tileSpeedStartTime = DateTime.Now;
        private DateTime processingStartTime = DateTime.Now;

        public TileGenerator()
        {
            StartZoomLevel = 0;
            EndZoomLevel = 20;
            TileSize = 512;
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
        public int TileSize { get; set; }

        /// <summary>
        /// Full path to the output directory where tiles will be saved
        /// </summary>
        public string BaseOutputDirectory
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
            shapeFile.Open();
            shapeFile.Name = Path.GetFileNameWithoutExtension(shapeFile.ShapePathFilename);

            if (shapeFile.Projection != null)
            {
                shapeFile.FeatureSource.ProjectionConverter = new ProjectionConverter(shapeFile.Projection.ProjString, 3857);
                shapeFile.FeatureSource.ProjectionConverter.Open();
            }

            Process(shapeFile, cancellationToken, includedAttributes);
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

        private void Process(ShapeFileFeatureLayer shapeFile, CancellationToken cancellationToken, List<string> includedAttributes = null)
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

            SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
            double currentScale = GetZoomLevelIndex(zoomLevelSet, zoom);

            var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, tileSize, tileSize, GeographyUnit.Meter);
            var tileRange = tileMatrix.GetIntersectingRowColumnRange(shapeFileBounds);

            processTileCount = totalDataTileCount = tileSpeedCount = 0;
            processingStartTime = DateTime.Now;
            tileSpeedStartTime = DateTime.Now;


            for (long tileY = tileRange.MinRowIndex; tileY <= tileRange.MaxRowIndex && !cancellationToken.IsCancellationRequested; ++tileY)
            {
                for (long tileX = tileRange.MinColumnIndex; tileX <= tileRange.MaxColumnIndex && !cancellationToken.IsCancellationRequested; ++tileX)
                {
                    ProcessTileRecursive(shapeFile, (int)tileY, (int)tileX, zoom, endZoomLevel, cancellationToken, includedAttributes);
                }
            }

            if (tileSpeedCount >= 1000)
            {
                DateTime tick = DateTime.Now;
                double elapsedSeconds = tick.Subtract(tileSpeedStartTime).TotalSeconds;
                OnStatusMessage(new StatusMessageEventArgs(string.Format("total tiles processed:{0}, total data tiles:{1}, speed={2:0.00} tiles/second", processTileCount, totalDataTileCount, tileSpeedCount / elapsedSeconds)));
            }
        }

        private void ProcessTileRecursive(FeatureLayer shapeFile, int tileX, int tileY, int zoom, int maxZoomLevel, CancellationToken cancellationToken, List<string> includedAttributes = null)
        {
            Console.WriteLine($"Tile: {zoom}-{tileX}-{tileY}");
            if (cancellationToken.IsCancellationRequested) return;
            bool result = ProcessTile(shapeFile, tileX, tileY, zoom, includedAttributes);

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
                ProcessTileRecursive(shapeFile, tileX << 1, tileY << 1, zoom + 1, maxZoomLevel, cancellationToken, includedAttributes);
                ProcessTileRecursive(shapeFile, (tileX << 1) + 1, tileY << 1, zoom + 1, maxZoomLevel, cancellationToken, includedAttributes);
                ProcessTileRecursive(shapeFile, tileX << 1, (tileY << 1) + 1, zoom + 1, maxZoomLevel, cancellationToken, includedAttributes);
                ProcessTileRecursive(shapeFile, (tileX << 1) + 1, (tileY << 1) + 1, zoom + 1, maxZoomLevel, cancellationToken, includedAttributes);
            }
        }

        private bool ProcessTile(FeatureLayer shapeFile, int tileX, int tileY, int zoom, IEnumerable<string> columnNames)
        {
            ++processTileCount;
            List<FeatureLayer> layers = new List<FeatureLayer>();
            layers.Add(shapeFile);
            var vectorTile = VectorTileGenerator.Generate(tileX, tileY, zoom, layers, columnNames, 512, 1);
            if (vectorTile != null && vectorTile.Count > 0)
            {
                using (FileStream fs = new FileStream(GetTileName(tileX, tileY, zoom), FileMode.Create, FileAccess.ReadWrite))
                {
                    VectorTileFeature.Encode(vectorTile, fs);
                }
                ++totalDataTileCount;
                return true;
            }
            return false;
        }


        protected string GetTileName(int tileX, int tileY, int zoom)
        {
            return Path.Combine(this.BaseOutputDirectory, string.Format("{0}_{1}_{2}.mvt", zoom, tileX, tileY));
        }
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
            get; set;
        }
    }
}
