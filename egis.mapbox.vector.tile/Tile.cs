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
            foreach (var vectorTileLayer in this.Layers)
            {
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
                    feature.Id = (ulong)(n + 1);
                    feature.GenerateNativeGeometry();
                    foreach (var keyValue in feature.Attributes)
                    {
                        feature.Tags.Add((uint)keysIndex[keyValue.Key]);
                        feature.Tags.Add((uint)valuesIndex[keyValue.Value]);
                    }
                }

                vectorTileLayer.Keys.AddRange(keys);
                foreach (var value in values)
                {
                    vectorTileLayer.Values.Add(AttributeKeyValue.ToTileValue(value));
                }
            }

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
                foreach (var feature in layer.Features)
                {
                    feature.Initialize(layer.Keys, layer.Values, layer.Extent);
                    layer.Features.Add(feature);
                }
            }
            return tile;
        }
    }
}