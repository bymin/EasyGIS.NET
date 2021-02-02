using ProtoBuf;
using System.Collections.Generic;
using System.IO;

namespace EGIS.Mapbox.Vector.Tile
{
    [ProtoBuf.ProtoContract(Name = @"feature")]
    public sealed class TileFeature : ProtoBuf.IExtensible
    {
        private enum CommandId : System.UInt32
        {
            MoveTo = 1,
            LineTo = 2,
            ClosePath = 7
        }

        ulong _id;
        [ProtoBuf.ProtoMember(1, IsRequired = false, Name = @"id", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
        [System.ComponentModel.DefaultValue(default(ulong))]
        public ulong Id
        {
            get { return _id; }
            set { _id = value; }
        }
        readonly List<uint> _tags = new List<uint>();
        [ProtoBuf.ProtoMember(2, Name = @"tags", DataFormat = ProtoBuf.DataFormat.TwosComplement, Options = ProtoBuf.MemberSerializationOptions.Packed)]
        public System.Collections.Generic.List<uint> Tags
        {
            get { return _tags; }
        }

        GeometryType _type = GeometryType.Unknown;
        [ProtoBuf.ProtoMember(3, IsRequired = false, Name = @"type", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
        [System.ComponentModel.DefaultValue(GeometryType.Unknown)]
        public GeometryType Type
        {
            get { return _type; }
            set { _type = value; }
        }
        readonly List<uint> _geometry = new List<uint>();
        [ProtoBuf.ProtoMember(4, Name = @"geometry", DataFormat = ProtoBuf.DataFormat.TwosComplement, Options = ProtoBuf.MemberSerializationOptions.Packed)]
        private List<uint> Geometry
        {
            get { return _geometry; }
        }

        ProtoBuf.IExtension _extensionObject;
        ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref _extensionObject, createIfMissing); }

        /// <summary>
        /// Get/Set the feature geometry
        /// </summary>
        public List<List<PointInt>> Geometry1 { get; set; }

        /// <summary>
        /// Get/Set the feature attributes
        /// </summary>
        public List<AttributeKeyValue> Attributes { get; set; }

        public uint Extent { get; set; }


        public void Initialize(List<string> keys, List<Value> values, uint extent)
        {
            // add the geometry
            this.Geometry1 = ParseGeometry(this.Geometry, this.Type);
            this.Extent = extent;

            this.Attributes = AttributeKeyValue.Parse(keys, values, this.Tags);
        }

        public void GenerateGeometry()
        {
            switch (this.Type)
            {
                case GeometryType.Point:
                    EncodePointGeometry(this.Geometry1, this.Geometry);
                    break;
                case GeometryType.LineString:
                    EncodeLineGeometry(this.Geometry1, this.Geometry);
                    break;
                case GeometryType.Polygon:
                    EncodePolygonGeometry(this.Geometry1, this.Geometry);
                    break;
                default:
                    throw new System.Exception(string.Format("Unknown geometry type:{0}", this.Type));
            }
        }

        public static List<List<PointInt>> ParseGeometry(List<uint> geom, GeometryType geomType)
        {
            int x = 0;
            int y = 0;
            var coordsList = new List<List<PointInt>>();
            List<PointInt> coords = null;
            var geometryCount = geom.Count;
            uint length = 0;
            uint command = 0;
            var i = 0;
            while (i < geometryCount)
            {
                if (length <= 0)
                {
                    length = geom[i++];
                    command = length & ((1 << 3) - 1);
                    length = length >> 3;
                }

                if (length > 0)
                {
                    if (command == (uint)CommandId.MoveTo)
                    {
                        coords = new List<PointInt>();
                        coordsList.Add(coords);
                    }
                }

                if (command == (uint)CommandId.ClosePath)
                {
                    if (geomType != GeometryType.Point && !(coords.Count == 0))
                    {
                        coords.Add(coords[0]);
                    }
                    length--;
                    continue;
                }

                var dx = geom[i++];
                var dy = geom[i++];

                length--;

                var ldx = ZigZag.Decode((int)dx);
                var ldy = ZigZag.Decode((int)dy);

                x = x + ldx;
                y = y + ldy;

                var coord = new PointInt() { X = x, Y = y };
                coords.Add(coord);
            }
            return coordsList;
        }

        private static void EncodePointGeometry(List<List<PointInt>> coordList, List<System.UInt32> geometry)
        {
            PointInt prevCoord = new PointInt() { X = 0, Y = 0 };

            foreach (List<PointInt> points in coordList)
            {
                if (points.Count == 0) throw new System.Exception(string.Format("unexpected point count encoding point geometry. Count is {0}", points.Count));

                System.UInt32 commandInteger = ((uint)CommandId.MoveTo & 0x7) | ((uint)points.Count << 3);
                geometry.Add(commandInteger);
                for (int n = 0; n < points.Count; ++n)
                {
                    int dx = points[n].X - prevCoord.X;
                    int dy = points[n].Y - prevCoord.Y;
                    int parameter = ZigZag.Encode(dx);
                    geometry.Add((System.UInt32)parameter);
                    parameter = ZigZag.Encode(dy);
                    geometry.Add((System.UInt32)parameter);
                    prevCoord = points[n];
                }
            }
        }

        private static void EncodeLineGeometry(List<List<PointInt>> coordList, List<System.UInt32> geometry)
        {
            PointInt prevCoord = new PointInt() { X = 0, Y = 0 };
            foreach (List<PointInt> points in coordList)
            {
                if (points.Count == 0) throw new System.Exception(string.Format("unexpected point count encoding line geometry. Count is {0}", points.Count));

                //start of linestring
                System.UInt32 commandInteger = ((uint)CommandId.MoveTo & 0x7) | (1 << 3);
                geometry.Add(commandInteger);

                int dx = points[0].X - prevCoord.X;
                int dy = points[0].Y - prevCoord.Y;

                int parameter = ZigZag.Encode(dx);
                geometry.Add((System.UInt32)parameter);
                parameter = ZigZag.Encode(dy);
                geometry.Add((System.UInt32)parameter);

                //encode the rest of the points
                commandInteger = ((uint)CommandId.LineTo & 0x7) | ((uint)(points.Count - 1) << 3);
                geometry.Add(commandInteger);
                for (int n = 1; n < points.Count; ++n)
                {
                    dx = points[n].X - points[n - 1].X;
                    dy = points[n].Y - points[n - 1].Y;
                    parameter = ZigZag.Encode(dx);
                    geometry.Add((System.UInt32)parameter);
                    parameter = ZigZag.Encode(dy);
                    geometry.Add((System.UInt32)parameter);
                }
                prevCoord = points[points.Count - 1];
            }
        }


        private static void EncodePolygonGeometry(List<List<PointInt>> coordList, List<System.UInt32> geometry)
        {
            PointInt prevCoord = new PointInt() { X = 0, Y = 0 };
            foreach (List<PointInt> points in coordList)
            {
                if (points.Count == 0) throw new System.Exception(string.Format("unexpected point count encoding polygon geometry. Count is {0}", points.Count));

                //start of ring
                System.UInt32 commandInteger = ((uint)CommandId.MoveTo & 0x7) | (1 << 3);
                geometry.Add(commandInteger);

                int dx = points[0].X - prevCoord.X;
                int dy = points[0].Y - prevCoord.Y;

                int parameter = ZigZag.Encode(dx);
                geometry.Add((System.UInt32)parameter);
                parameter = ZigZag.Encode(dy);
                geometry.Add((System.UInt32)parameter);

                bool lastPointRepeated = (points[points.Count - 1].X == points[0].X && points[points.Count - 1].Y == points[0].Y);

                int pointCount = lastPointRepeated ? points.Count - 2 : points.Count - 1;

                //encode the rest of the points
                commandInteger = ((uint)CommandId.LineTo & 0x7) | ((uint)(pointCount) << 3);
                geometry.Add(commandInteger);
                for (int n = 1; n <= pointCount; ++n)
                {
                    dx = points[n].X - points[n - 1].X;
                    dy = points[n].Y - points[n - 1].Y;
                    parameter = ZigZag.Encode(dx);
                    geometry.Add((System.UInt32)parameter);
                    parameter = ZigZag.Encode(dy);
                    geometry.Add((System.UInt32)parameter);
                }

                //close path
                commandInteger = ((uint)CommandId.ClosePath & 0x7) | (1 << 3);
                geometry.Add(commandInteger);

                prevCoord = points[pointCount];
            }
        }


        /// <summary>
        /// Parses a Mapbox .mvt binary stream and returns a List of VectorTileLayer objects
        /// </summary>
        /// <param name="stream">stream opened from a .mvt Mapbox tile</param>
        /// <returns></returns>
        public static List<VectorTileLayer> Parse(Stream stream)
        {
            var tile = Serializer.Deserialize<Tile>(stream);
            var list = new List<VectorTileLayer>();
            foreach (var layer in tile.Layers)
            {
                var extent = layer.Extent;
                var vectorTileLayer = new VectorTileLayer();
                vectorTileLayer.Name = layer.Name;
                vectorTileLayer.Version = layer.Version;
                vectorTileLayer.Extent = layer.Extent;

                foreach (var feature in layer.Features)
                {
                    feature.Initialize(layer.Keys, layer.Values, extent);
                    vectorTileLayer.VectorTileFeatures.Add(feature);
                }
                list.Add(vectorTileLayer);
            }
            return list;
        }

        /// <summary>
        /// Encodes a Mapbox .mvt tile
        /// </summary>
        /// <param name="layers">List of VectorTileLayers to encode. A Tile should contain at least one layer</param>
        /// <param name="stream">output .mvt tile stream</param>
        public static void Encode(List<VectorTileLayer> layers, Stream stream)
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

                foreach (var feature in vectorTileLayer.VectorTileFeatures)
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

                for (int n = 0; n < vectorTileLayer.VectorTileFeatures.Count; ++n)
                {
                    var feature = vectorTileLayer.VectorTileFeatures[n];
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
