﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;

namespace MBTiles
{
    public class TilesTable
    {
        private SqliteConnection connection;
        public List<TilesEntry> Entries { get; }
        public int Cursor { get; set; }
        public string TableName { get; set; }

        public static readonly string ZoomLevelColumnName = "zoom_level";
        public static readonly string TileColColumnName = "tile_column";
        public static readonly string TileRowColumnName = "tile_row";
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

            string insertSqlStatement = $"INSERT INTO {TableName} ({ZoomLevelColumnName},{TileColColumnName},{TileRowColumnName},{TileDataColumnName}) VALUES (@{ZoomLevelColumnName}, @{TileColColumnName}, @{TileRowColumnName}, @{TileDataColumnName});";

            SqliteCommand command = new SqliteCommand(insertSqlStatement, connection);
            command.Parameters.Add($"@{ZoomLevelColumnName}", SqliteType.Integer);
            command.Parameters.Add($"@{TileColColumnName}", SqliteType.Integer);
            command.Parameters.Add($"@{TileRowColumnName}", SqliteType.Integer);
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
    }
}
