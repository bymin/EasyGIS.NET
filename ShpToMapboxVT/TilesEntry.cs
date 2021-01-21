using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShpToMapboxVT
{

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
}
