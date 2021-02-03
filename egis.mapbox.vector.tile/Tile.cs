using ProtoBuf;
using System.Collections.Generic;
using System.IO;

namespace EGIS.Mapbox.Vector.Tile
{
    [ProtoBuf.ProtoContract(Name = @"tile")]
    public sealed class Tile : ProtoBuf.IExtensible
    {
        ProtoBuf.IExtension _extensionObject;

        public Tile()
        {
            Layers = new List<TileLayer>();
        }

        [ProtoBuf.ProtoMember(3, Name = @"layers", DataFormat = ProtoBuf.DataFormat.Default)]
        public List<TileLayer> Layers { get; }

        ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref _extensionObject, createIfMissing); }

        /// <summary>
        /// Serialize to a Mapbox .mvt tile
        /// </summary>
        /// <param name="stream">output .mvt tile stream</param>
        public void Serialize(Stream stream)
        {
            Serializer.Serialize<Tile>(stream, this);
        }

        /// <summary>
        /// Parses a Mapbox .mvt binary stream and returns a Tile object
        /// </summary>
        /// <param name="stream">stream opened from a .mvt Mapbox tile</param>
        /// <returns></returns>
        public static Tile Deserialize(Stream stream)
        {
            var tile = Serializer.Deserialize<Tile>(stream);
            foreach (var layer in tile.Layers)
            {
                layer.FillInTheExternalProperties();
            }
            return tile;
        }
    }
}