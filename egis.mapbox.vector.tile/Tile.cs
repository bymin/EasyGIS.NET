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
        /// Encodes a Mapbox .mvt tile
        /// </summary>
        /// <param name="layers">List of VectorTileLayers to encode. A Tile should contain at least one layer</param>
        /// <param name="stream">output .mvt tile stream</param>
        public static void Serialize(List<TileLayer> layers, Stream stream)
        {
            Tile tile = new Tile();

            foreach (var vectorTileLayer in layers)
            {
                TileLayer tileLayer = new TileLayer();
                tile.Layers.Add(tileLayer);

                tileLayer.Name = vectorTileLayer.Name;
                tileLayer.Version = vectorTileLayer.Version;
                tileLayer.Extent = vectorTileLayer.Extent;

                //index the key value attributes
                List<string> keys = new List<string>();
                List<AttributeKeyValue> values = new List<AttributeKeyValue>();

                Dictionary<string, int> keysIndex = new Dictionary<string, int>();
                Dictionary<dynamic, int> valuesIndex = new Dictionary<dynamic, int>();

                foreach (var feature in vectorTileLayer.Features)
                {
                    foreach (var keyValue in feature.Attributes)
                    {
                        if (!keysIndex.ContainsKey(keyValue.Key))
                        {
                            keysIndex.Add(keyValue.Key, keys.Count);
                            keys.Add(keyValue.Key);
                        }
                        if (!valuesIndex.ContainsKey(keyValue.Value))
                        {
                            valuesIndex.Add(keyValue.Value, values.Count);
                            values.Add(keyValue);
                        }
                    }
                }

                for (int n = 0; n < vectorTileLayer.Features.Count; ++n)
                {
                    var feature = vectorTileLayer.Features[n];
                    tileLayer.Features.Add(feature);
                    feature.Id = (ulong)(n + 1);
                    feature.GenerateGeometry();
                    foreach (var keyValue in feature.Attributes)
                    {
                        feature.Tags.Add((uint)keysIndex[keyValue.Key]);
                        feature.Tags.Add((uint)valuesIndex[keyValue.Value]);
                    }
                }

                tileLayer.Keys.AddRange(keys);
                foreach (var value in values)
                {
                    tileLayer.Values.Add(AttributeKeyValue.ToTileValue(value));
                }
            }

            Serializer.Serialize<Tile>(stream, tile);
        }
    }
}