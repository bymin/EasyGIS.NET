using MBTiles;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThinkGeo.Core;

namespace ShpToMBTiles
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
                ShapeFileFeatureLayer shapefileFeatureLayer = new ShapeFileFeatureLayer(ofdShapeFile.FileName);
                shapefileFeatureLayer.RequireIndex = false;
                shapefileFeatureLayer.Open();
                clbSelectedAttributes.Items.Clear();
                var attributeNames = shapefileFeatureLayer.QueryTools.GetColumns().Select(f => f.ColumnName);
                foreach (string name in attributeNames)
                {
                    clbSelectedAttributes.Items.Add(name, false);
                }
                shapefileFeatureLayer.Close();
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

            List<string> attributes = new List<string>();
            for (int i = 0; i < clbSelectedAttributes.CheckedItems.Count; ++i)
            {
                attributes.Add(clbSelectedAttributes.CheckedItems[i].ToString());
            }
            OutputMessage("Processing Vector tiles..\n");
            DateTime tick = DateTime.Now;
            ShapeFileFeatureSource.BuildIndexFile(this.txtInputShapeFile.Text, BuildIndexMode.DoNotRebuild);

            await MBTilesGenerator.Process(this.txtInputShapeFile.Text, this.txtMbtilesFilePathname.Text, cancellationTokenSource.Token, (int)(nudStartZoom.Value), (int)(nudEndZoom.Value), 512, attributes);
            OutputMessage("Processing Vector tiles complete. Elapsed time:" + DateTime.Now.Subtract(tick) + "\n");
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

            string jsonFile = File.ReadAllText(@".\SimpleRenderer.json");
            tbxJson.Text = jsonFile;

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

        private void btnReload_Click(object sender, EventArgs e)
        {
            File.WriteAllText("tmpRenderer.json", tbxJson.Text);

            ThinkGeoMBTilesLayer mbTileslayer = (ThinkGeoMBTilesLayer)((LayerOverlay)mapView1.Overlays["mbtilesOverlay"]).Layers[0];
            mbTileslayer.StyleJsonUri = new Uri(@"./tmpRenderer.json", UriKind.Relative);
            mbTileslayer.LoadStyleJson();
            mapView1.Overlays["mbtilesOverlay"].Refresh();
        }

        private void btnReloadJson_Click(object sender, EventArgs e)
        {
            string jsonFile = File.ReadAllText(@".\SimpleRenderer.json");
            tbxJson.Text = jsonFile;
        }

        private void mapView1_CurrentScaleChanged(object sender, CurrentScaleChangedMapViewEventArgs e)
        {
            int zoom = MBTilesGenerator.GetZoom(this.mapView1.ZoomLevelSet, e.NewScale);
         
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Current Zoom: {zoom}");

            MBTiles.Tile tile = MBTilesGenerator.GetFirstTile(txtMbtilesFilePathname.Text, zoom);
            if (tile != null)
            {
                foreach (MBTiles.TileLayer tileLayer in tile.Layers)
                {
                    sb.AppendLine($"Layer: {tileLayer.Name}");
                    sb.AppendLine($"Version: {tileLayer.Version}");
                    sb.AppendLine($"There are {tileLayer.Keys.Count} columns in the data");
                    foreach (string key in tileLayer.Keys)
                    {
                        sb.AppendLine($"\t Column Name: {key}");
                    }
                }
            }
            txtMBTileInfo.Text = sb.ToString();
        }
    }
}
