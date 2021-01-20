using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using EGIS.Mapbox.Vector.Tile;
using Microsoft.Data.Sqlite;
using System.IO;
using ThinkGeo.Core;
using System.IO.Compression;

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

        private List<string> inputShapeFiles = new List<string>();

        public MainForm()
        {
            InitializeComponent();

            ValidateCanProcess();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            LoadSettings();

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            SaveSettings();
        }

        private void LoadSettings()
        {
            if (Properties.Settings.Default.LastOutputDir != null)
            {
                this.txtOutputDirectory.Text = Properties.Settings.Default.LastOutputDir;
            }
        }

        private void SaveSettings()
        {
            if (!string.IsNullOrEmpty(this.txtOutputDirectory.Text) && System.IO.Directory.Exists(this.txtOutputDirectory.Text))
            {
            }
            Properties.Settings.Default.LastOutputDir = this.txtOutputDirectory.Text;
            Properties.Settings.Default.Save();


        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void btnBrowseShapeFile_Click(object sender, EventArgs e)
        {
            if (ofdShapeFile.ShowDialog(this) == DialogResult.OK)
            {
                OpenShapeFiles(ofdShapeFile.FileName);
            }
        }




        private void btnBrowseOutputDirectory_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this.txtOutputDirectory.Text) && System.IO.Directory.Exists(this.txtOutputDirectory.Text))
            {
                this.fbdOutput.SelectedPath = this.txtOutputDirectory.Text;
            }
            if (this.fbdOutput.ShowDialog(this) == DialogResult.OK)
            {
                SetOutputDirectory(fbdOutput.SelectedPath);

            }
        }


        private void OpenShapeFiles(string filename)
        {
            this.txtInputShapeFile.Text = filename;
            ShapeFileFeatureLayer sf = new ShapeFileFeatureLayer(filename);
            sf.Open();
            {
                clbSelectedAttributes.Items.Clear();
                //string[] attributeNames = sf.GetAttributeFieldNames();
                string[] attributeNames = sf.QueryTools.GetColumns().Select(f => f.ColumnName).ToArray();
                foreach (string name in attributeNames)
                {
                    clbSelectedAttributes.Items.Add(name, true);
                }
            }
            ValidateCanProcess();

        }

        private void SetOutputDirectory(string outputDirectory)
        {
            this.txtOutputDirectory.Text = outputDirectory;
            ValidateCanProcess();
        }

        private void ValidateCanProcess()
        {
            bool ok = (!string.IsNullOrEmpty(this.txtOutputDirectory.Text) && System.IO.Directory.Exists(this.txtOutputDirectory.Text)) &&
                (!string.IsNullOrEmpty(this.txtInputShapeFile.Text) && System.IO.File.Exists(this.txtInputShapeFile.Text));

            this.btnProcess.Enabled = ok;

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
                        string resultFile = Path.Combine(txtOutputDirectory.Text, "result.mbtiles");
                        if (File.Exists(resultFile))
                            File.Delete(resultFile);
                        await GenerateMbTiles(resultFile, txtOutputDirectory.Text, (int)(nudEndZoom.Value));
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

        private System.Threading.CancellationTokenSource cancellationTokenSource = null;

        private async Task<bool> GenerateTiles()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
            cancellationTokenSource = new System.Threading.CancellationTokenSource();

            return await Task.Run(() =>
            {

                TileGenerator generator = new TileGenerator()
                {
                    BaseOutputDirectory = this.txtOutputDirectory.Text,
                    StartZoomLevel = (int)nudStartZoom.Value,
                    EndZoomLevel = (int)nudEndZoom.Value,
                    ExportAttributesToSeparateFile = this.chkExportAttributesToSeparateFile.Checked
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
                    generator.Process(this.txtInputShapeFile.Text, cancellationTokenSource.Token, attributes);
                    OutputMessage("Processing Vector tiles complete. Elapsed time:" + DateTime.Now.Subtract(tick) + "\n");
                }
                finally
                {
                    generator.StatusMessage -= Generator_StatusMessage;
                }
                return true;
            });
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
                    //unzippedStream.CopyTo(zippedStream);
                    //zippedBytes = zippedMemoryStream.ToArray();
                    zippedBytes = zippedMemoryStream.ToArray();
                }
            }
            return zippedBytes;
        }

        private async Task GenerateMbTiles(string targetFilePath, string sourceDirectory, int maxZoom)
        {
            ShapeFileFeatureLayer.BuildIndexFile(txtInputShapeFile.Text, BuildIndexMode.DoNotRebuild);
            ShapeFileFeatureLayer layer = new ShapeFileFeatureLayer(txtInputShapeFile.Text);
            layer.Open();
            RectangleShape extent = layer.GetBoundingBox();
            layer.Close();

            PointShape centerPoint = extent.GetCenterPoint();
            string center = $"{centerPoint.X},{centerPoint.Y},14";

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
            Entries.Add(new MetadataEntry() { Name = "minzoom", Value = "0" });
            Entries.Add(new MetadataEntry() { Name = "maxzoom", Value = "14" });
            Entries.Add(new MetadataEntry() { Name = "attribution", Value = "Copyright @2020 ThinkGeo LLC.All rights reserved." });
            Entries.Add(new MetadataEntry() { Name = "description", Value = "ThinkGeo World Street Vector Tile Data in EPSG:3857" });
            Entries.Add(new MetadataEntry() { Name = "version", Value = "2.0" });
            Entries.Add(new MetadataEntry() { Name = "json", Value = "" });
            targetMetadata.Insert(Entries);


            await Task.Run(() =>
            {
                string[] files = Directory.GetFiles(sourceDirectory, "*.mvt");
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
    }


    public class TilesTable
    {
        private SqliteConnection connection;
        public List<TilesEntry> Entries { get; }
        public int Cursor { get; set; }
        public string TableName { get; set; }

        public static readonly string ZoomLevelColumnName = "zoom_level";
        public static readonly string TileColColumnName = "tile_column";
        public static readonly string TileRowColumnName = "tile_row";
        public static readonly string TileIdColumnName = "Id";
        public static readonly string TileDataColumnName = "tile_data";

        public TilesTable(SqliteConnection connection)
        {
            this.connection = connection;
            Entries = new List<TilesEntry>();
            Cursor = 0;
            TableName = "tiles";
        }

        public bool Insert(IEnumerable<TilesEntry> entries)
        {
            bool result = true;

            string insertSqlStatement = $"INSERT INTO {TableName} ({ZoomLevelColumnName},{TileColColumnName},{TileRowColumnName},{TileIdColumnName},{TileDataColumnName}) VALUES (@{ZoomLevelColumnName}, @{TileColColumnName}, @{TileRowColumnName}, @{TileIdColumnName}, @{TileDataColumnName});";

            SqliteCommand command = new SqliteCommand(insertSqlStatement, connection);
            command.Parameters.Add($"@{ZoomLevelColumnName}", SqliteType.Integer);
            command.Parameters.Add($"@{TileColColumnName}", SqliteType.Integer);
            command.Parameters.Add($"@{TileRowColumnName}", SqliteType.Integer);
            command.Parameters.Add($"@{TileIdColumnName}", SqliteType.Text);
            command.Parameters.Add($"@{TileDataColumnName}", SqliteType.Blob);
            command.Prepare();
            IDbTransaction dbTransaction = connection.BeginTransaction();
            command.Transaction = dbTransaction as SqliteTransaction;
            try
            {
                foreach (TilesEntry entry in entries)
                {
                    command.Parameters[$"@{ZoomLevelColumnName}"].Value = ParseValue(entry.ZoomLevel);
                    command.Parameters[$"@{TileColColumnName}"].Value = ParseValue(entry.TileColumn);
                    command.Parameters[$"@{TileRowColumnName}"].Value = ParseValue(entry.TileRow);
                    command.Parameters[$"@{TileIdColumnName}"].Value = ParseValue(entry.TileId);
                    command.Parameters[$"@{TileDataColumnName}"].Value = entry.TileData;
                    command.ExecuteNonQuery();
                }
                dbTransaction.Commit();
            }
            catch
            {
                dbTransaction.Rollback();
                result = false;
            }

            return result;
        }

        private object ParseValue(object value)
        {
            if (value == null)
                return DBNull.Value;

            return value;
        }

        public List<TilesEntry> Query(string sqlString)
        {
            List<TilesEntry> result = new List<TilesEntry>();

            SqliteCommand command = new SqliteCommand()
            {
                Connection = connection,
            };
            command.CommandText = sqlString;
            SqliteDataReader dataReader = command.ExecuteReader();
            while (dataReader.Read())
            {
                TilesEntry newEntry = new TilesEntry();
                newEntry.ZoomLevel = (long)dataReader[ZoomLevelColumnName];
                newEntry.TileColumn = (long)dataReader[TileColColumnName];
                newEntry.TileRow = (long)dataReader[TileRowColumnName];
                newEntry.TileId = (long)dataReader[TileIdColumnName];
                newEntry.TileData = (byte[])dataReader[TileDataColumnName];

                result.Add(newEntry);
                Cursor++;
            }

            return result;
        }
    }

    public class TilesEntry
    {
        public long ZoomLevel { get; set; }
        public long TileColumn { get; set; }
        public long TileRow { get; set; }
        public long TileId { get; set; }
        public byte[] TileData { get; set; }

        public TilesEntry()
        { }
    }

    public class MetadataTable
    {
        private SqliteConnection connection;
        public List<MetadataEntry> Entries { get; }
        private string TableName { get; set; }

        public static readonly string MetadataValueColumnName = "value";
        public static readonly string MetadataNameColumnName = "name";

        public MetadataTable(SqliteConnection connection)
        {
            this.connection = connection;
            Entries = new List<MetadataEntry>();
            TableName = "metadata";
        }

        public bool Insert(IEnumerable<MetadataEntry> entries)
        {
            bool result = true;
            string insertSqlStatement = $"INSERT INTO {TableName} (name, value) VALUES (@name, @value);";
            SqliteCommand command = new SqliteCommand(insertSqlStatement, connection);
            command.Parameters.Add("@name", SqliteType.Text);
            command.Parameters.Add("@value", SqliteType.Text);
            command.Prepare();
            IDbTransaction dbTransaction = connection.BeginTransaction();
            command.Transaction = dbTransaction as SqliteTransaction;
            try
            {
                foreach (MetadataEntry entry in entries)
                {
                    command.Parameters["@name"].Value = entry.Name;
                    command.Parameters["@value"].Value = entry.Value;
                    command.ExecuteNonQuery();
                }

                dbTransaction.Commit();
            }
            catch
            {
                dbTransaction.Rollback();
                result = false;
            }

            return result;
        }

        public void ReadAllEntries()
        {
            SqliteDataReader dataReader = null;
            Entries.Clear();

            SqliteCommand command = new SqliteCommand()
            {
                Connection = connection,
            };
            command.CommandText = $"SELECT * FROM {TableName}";
            dataReader = command.ExecuteReader();

            while (dataReader.Read())
            {
                string name = (string)dataReader["name"];
                string value = (string)dataReader["value"];
                Entries.Add(new MetadataEntry() { Name = name, Value = value });
            }
        }


    }
    public class MetadataEntry
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
