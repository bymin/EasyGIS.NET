﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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
    /// Main Form of the Shape to MapBox Vector Tiles Generator 
    /// </summary>
    public partial class MainForm : Form
    {
        private System.Threading.CancellationTokenSource cancellationTokenSource = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnBrowseShapeFile_Click(object sender, EventArgs e)
        {
            if (ofdShapeFile.ShowDialog(this) == DialogResult.OK)
            {
                this.txtInputShapeFile.Text = ofdShapeFile.FileName;

                // load the column names
                ShapeFileFeatureSource.BuildIndexFile(ofdShapeFile.FileName, BuildIndexMode.DoNotRebuild);
                ShapeFileFeatureLayer sf = new ShapeFileFeatureLayer(ofdShapeFile.FileName);
                sf.Open();
                clbSelectedAttributes.Items.Clear();
                var attributeNames = sf.QueryTools.GetColumns().Select(f => f.ColumnName);
                foreach (string name in attributeNames)
                {
                    clbSelectedAttributes.Items.Add(name, false);
                }
                sf.Close();
                txtMbtilesFilePathname.Text = txtInputShapeFile.Text.ToLower().Replace(".shp", ".mbtiles");
                ValidateCanProcess();
            }
        }

        private void btnBrowseOutputFilePathname_Click(object sender, EventArgs e)
        {
            sfdMbtiles = new SaveFileDialog();
            sfdMbtiles.Filter = "mbtiles|*.mbtiles";
            if (!string.IsNullOrEmpty(this.txtMbtilesFilePathname.Text) && System.IO.File.Exists(this.txtMbtilesFilePathname.Text))
            {
                this.sfdMbtiles.FileName = this.txtMbtilesFilePathname.Text;
            }
            if (this.sfdMbtiles.ShowDialog(this) == DialogResult.OK)
            {
                this.txtMbtilesFilePathname.Text = sfdMbtiles.FileName;
                ValidateCanProcess();
            }
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.btnProcess.Text.Equals("Cancel"))
                {
                    if (this.cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested) this.cancellationTokenSource.Cancel();
                }
                else
                {
                    this.btnProcess.Text = "Cancel";
                    try
                    {
                        await GenerateTiles();
                        string resultFile = txtMbtilesFilePathname.Text;
                        if (File.Exists(resultFile))
                            File.Delete(resultFile);
                        await GenerateMbTiles(resultFile, (int)(nudStartZoom.Value), (int)(nudEndZoom.Value));

                    }
                    finally
                    {
                        this.btnProcess.Text = "Process";
                    }
                }
            }
            catch (Exception ex)
            {
                OutputMessage(ex.ToString() + "\n");
            }
        }

        private void Generator_StatusMessage(object sender, StatusMessageEventArgs e)
        {
            OutputMessage(e.Status + "\n");
        }

        private void btnSelectAllAttributes_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbSelectedAttributes.Items.Count; i++)
                clbSelectedAttributes.SetItemChecked(i, true);
        }

        private void btnSelectNoAttributes_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbSelectedAttributes.Items.Count; i++)
                clbSelectedAttributes.SetItemChecked(i, false);
        }
        private void ValidateCanProcess()
        {
            this.btnProcess.Enabled = (!string.IsNullOrEmpty(this.txtMbtilesFilePathname.Text))
                && (!string.IsNullOrEmpty(this.txtInputShapeFile.Text) && System.IO.File.Exists(this.txtInputShapeFile.Text));
        }

        private void OutputMessage(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OutputMessage(text)));
            }
            else
            {
                this.rtbOutput.AppendText(text);
                this.rtbOutput.Refresh();
            }
        }

        private async Task GenerateTiles()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
            cancellationTokenSource = new System.Threading.CancellationTokenSource();

            TileGenerator generator = new TileGenerator()
            {
                BaseOutputDirectory = Path.GetDirectoryName(this.txtMbtilesFilePathname.Text),
                StartZoomLevel = (int)nudStartZoom.Value,
                EndZoomLevel = (int)nudEndZoom.Value,
            };
            generator.StatusMessage += Generator_StatusMessage;
            try
            {
                List<string> attributes = new List<string>();
                for (int i = 0; i < clbSelectedAttributes.CheckedItems.Count; ++i)
                {
                    attributes.Add(clbSelectedAttributes.CheckedItems[i].ToString());
                }
                OutputMessage("Processing Vector tiles..\n");
                DateTime tick = DateTime.Now;
                await generator.Process(this.txtInputShapeFile.Text, cancellationTokenSource.Token, attributes);
                OutputMessage("Processing Vector tiles complete. Elapsed time:" + DateTime.Now.Subtract(tick) + "\n");
            }
            finally
            {
                generator.StatusMessage -= Generator_StatusMessage;
            }
        }

        public static byte[] GZipData(byte[] bytes)
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

        private async Task GenerateMbTiles(string targetFilePath, int minZoom, int maxZoom)
        {
            ShapeFileFeatureLayer.BuildIndexFile(txtInputShapeFile.Text, BuildIndexMode.DoNotRebuild);
            ShapeFileFeatureSource layer = new ShapeFileFeatureSource(txtInputShapeFile.Text);
            layer.Open();

            if (layer.Projection != null)
            {
                layer.ProjectionConverter = new ProjectionConverter(layer.Projection.ProjString, 3857);
                layer.ProjectionConverter.Open();
            }

            RectangleShape extent = layer.GetBoundingBox();
            layer.Close();

            PointShape centerPoint = extent.GetCenterPoint();
            string center = $"{centerPoint.X},{centerPoint.Y},{maxZoom}";
            string bounds = $"{extent.UpperLeftPoint.X},{extent.UpperLeftPoint.Y},{extent.LowerRightPoint.X},{extent.LowerRightPoint.Y}";

            ThinkGeoMBTilesLayer.CreateDatabase(targetFilePath);

            var targetDBConnection = new SqliteConnection($"Data Source={targetFilePath}");
            targetDBConnection.Open();
            var targetMap = new TilesTable(targetDBConnection);
            var targetMetadata = new MetadataTable(targetDBConnection);

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

            await Task.Run(() =>
            {
                string[] files = Directory.GetFiles(Path.GetDirectoryName(targetFilePath), "*.mvt");
                long index = 0;

                List<TilesEntry> entries = new List<TilesEntry>();

                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string[] NameValues = fileName.Split('_');

                    byte[] bytes = File.ReadAllBytes(file);
                    byte[] gzippedBytes = GZipData(bytes);


                    TilesEntry newEntry = new TilesEntry();
                    int zoomLevel = int.Parse(NameValues[0]);
                    newEntry.ZoomLevel = zoomLevel;
                    long row = long.Parse(NameValues[1]);
                    row = (long)Math.Pow(2, zoomLevel) - row - 1;
                    newEntry.TileRow = row;
                    newEntry.TileColumn = long.Parse(NameValues[2]);
                    newEntry.TileId = index++;
                    newEntry.TileData = gzippedBytes;
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

        private void btnDisplay_Click(object sender, EventArgs e)
        {
            ShapeFileFeatureLayer shapeFileFeatureLayer = new ShapeFileFeatureLayer(txtInputShapeFile.Text);
            shapeFileFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = AreaStyle.CreateSimpleAreaStyle(GeoColors.LightYellow, GeoColors.Black);
            shapeFileFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = LineStyle.CreateSimpleLineStyle(GeoColors.Black, 2, true);
            shapeFileFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = PointStyle.CreateSimpleCircleStyle(GeoColors.Transparent, 7, GeoColors.Black);
            shapeFileFeatureLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            shapeFileFeatureLayer.Open();

            if (shapeFileFeatureLayer.Projection != null)
            {
                shapeFileFeatureLayer.FeatureSource.ProjectionConverter = new ProjectionConverter(shapeFileFeatureLayer.Projection.ProjString, 3857);
                shapeFileFeatureLayer.FeatureSource.ProjectionConverter.Open();
            }

            LayerOverlay shapeFileOverlay = (LayerOverlay)(mapView1.Overlays["shapeFileOverlay"]);
            shapeFileOverlay.TileBuffer = 0;
            shapeFileOverlay.Layers.Add(shapeFileFeatureLayer);

            ThinkGeoMBTilesLayer thinkGeoMBTilesLayer = new ThinkGeoMBTilesLayer(txtMbtilesFilePathname.Text, new Uri(@"./SimpleRenderer.json", UriKind.Relative));
            LayerOverlay mbtilesOverlay = (LayerOverlay)(mapView1.Overlays["mbtilesOverlay"]);
            mbtilesOverlay.TileBuffer = 0;
            mbtilesOverlay.Layers.Add(thinkGeoMBTilesLayer);

            mapView1.MapUnit = GeographyUnit.Meter;
            shapeFileFeatureLayer.Open();
            mapView1.CurrentExtent = shapeFileFeatureLayer.GetBoundingBox();

            mapView1.Refresh();

            ckbDisplayShapeFile.Enabled = true;
            ckbDisplayMbtiles.Enabled = true;
        }

        private void ckbDisplayMbtiles_CheckedChanged(object sender, EventArgs e)
        {
            mapView1.Overlays["shapeFileOverlay"].IsVisible = ckbDisplayShapeFile.Checked;
            mapView1.Overlays["mbtilesOverlay"].IsVisible = ckbDisplayMbtiles.Checked;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LayerOverlay shapeFileOverlay = new LayerOverlay();
            mapView1.Overlays.Add("shapeFileOverlay", shapeFileOverlay);

            LayerOverlay mbtilesOverlay = new LayerOverlay();
            mapView1.Overlays.Add("mbtilesOverlay", mbtilesOverlay);
        }
    }
}
