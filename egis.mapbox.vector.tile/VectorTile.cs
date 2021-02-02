
using ProtoBuf;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace EGIS.Mapbox.Vector.Tile
{
    [ProtoBuf.ProtoContract(Name = @"tile")]
    public sealed class Tile : ProtoBuf.IExtensible
    {
        readonly System.Collections.Generic.List<Layer> _layers = new System.Collections.Generic.List<Layer>();
        [ProtoBuf.ProtoMember(3, Name = @"layers", DataFormat = ProtoBuf.DataFormat.Default)]
        public System.Collections.Generic.List<Layer> Layers
        {
            get { return _layers; }
        }

        [ProtoBuf.ProtoContract(Name = @"value")]
        public sealed class Value : ProtoBuf.IExtensible
        {
            string _stringValue = "";

            public bool HasStringValue { get; set; }
            public bool HasFloatValue { get; set; }
            public bool HasDoubleValue { get; set; }
            public bool HasIntValue { get; set; }
            public bool HasUIntValue { get; set; }
            public bool HasSIntValue { get; set; }
            public bool HasBoolValue { get; set; }

            //public bool ShouldSerializeStringValue() => HasStringValue;
            //public bool ShouldSerializeFloatValue() => HasFloatValue;
            //public bool ShouldSerializeDoubleValue() => HasDoubleValue;
            //public bool ShouldSerializeIntValue() => HasIntValue;
            //public bool ShouldSerializeUIntValue() => HasUIntValue;
            //public bool ShouldSerializeSIntValue() => HasSIntValue;
            //public bool ShouldSerializeBoolValue() => HasBoolValue;

            [ProtoBuf.ProtoMember(1, IsRequired = false, Name = @"string_value", DataFormat = ProtoBuf.DataFormat.Default)]
            [System.ComponentModel.DefaultValue("")]
            public string StringValue
            {
                get { return _stringValue; }
                set
                {
                    HasStringValue = true;
                    _stringValue = value;
                }
            }

            float _floatValue;
            [ProtoBuf.ProtoMember(2, IsRequired = false, Name = @"float_value", DataFormat = ProtoBuf.DataFormat.FixedSize)]
            [System.ComponentModel.DefaultValue(default(float))]
            public float FloatValue
            {
                get
                {
                    return _floatValue;
                }
                set
                {
                    _floatValue = value;
                    HasFloatValue = true;

                }
            }
            double _doubleValue;
            [ProtoBuf.ProtoMember(3, IsRequired = false, Name = @"double_value", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
            [System.ComponentModel.DefaultValue(default(double))]
            public double DoubleValue
            {
                get { return _doubleValue; }
                set
                {
                    _doubleValue = value;
                    HasDoubleValue = true;
                }
            }
            long _intValue;
            [ProtoBuf.ProtoMember(4, IsRequired = false, Name = @"int_value", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
            [System.ComponentModel.DefaultValue(default(long))]
            public long IntValue
            {
                get { return _intValue; }
                set
                {
                    _intValue = value;
                    HasIntValue = true;
                }
            }
            ulong _uintValue;
            [ProtoBuf.ProtoMember(5, IsRequired = false, Name = @"uint_value", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
            [System.ComponentModel.DefaultValue(default(ulong))]
            public ulong UintValue
            {
                get { return _uintValue; }
                set
                {
                    _uintValue = value;
                    HasUIntValue = true;
                }
            }
            long _sintValue;
            [ProtoBuf.ProtoMember(6, IsRequired = false, Name = @"sint_value", DataFormat = ProtoBuf.DataFormat.ZigZag)]
            [System.ComponentModel.DefaultValue(default(long))]
            public long SintValue
            {
                get { return _sintValue; }
                set
                {
                    _sintValue = value;
                    HasSIntValue = true;
                }
            }
            bool _boolValue;
            [ProtoBuf.ProtoMember(7, IsRequired = false, Name = @"bool_value", DataFormat = ProtoBuf.DataFormat.Default)]
            [System.ComponentModel.DefaultValue(default(bool))]
            public bool BoolValue
            {
                get { return _boolValue; }
                set
                {
                    _boolValue = value;
                    HasBoolValue = true;
                }
            }
            ProtoBuf.IExtension _extensionObject;
            ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            { return ProtoBuf.Extensible.GetExtensionObject(ref _extensionObject, createIfMissing); }
        }

        [ProtoBuf.ProtoContract(Name = @"feature")]
        public sealed class Feature : ProtoBuf.IExtensible
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
            readonly System.Collections.Generic.List<uint> _tags = new System.Collections.Generic.List<uint>();
            [ProtoBuf.ProtoMember(2, Name = @"tags", DataFormat = ProtoBuf.DataFormat.TwosComplement, Options = ProtoBuf.MemberSerializationOptions.Packed)]
            public System.Collections.Generic.List<uint> Tags
            {
                get { return _tags; }
            }

            GeomType _type = GeomType.Unknown;
            [ProtoBuf.ProtoMember(3, IsRequired = false, Name = @"type", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
            [System.ComponentModel.DefaultValue(GeomType.Unknown)]
            public GeomType Type
            {
                get { return _type; }
                set { _type = value; }
            }
            readonly System.Collections.Generic.List<uint> _geometry = new System.Collections.Generic.List<uint>();
            [ProtoBuf.ProtoMember(4, Name = @"geometry", DataFormat = ProtoBuf.DataFormat.TwosComplement, Options = ProtoBuf.MemberSerializationOptions.Packed)]
            private System.Collections.Generic.List<uint> Geometry
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


            public void Initialize(List<string> keys, List<Tile.Value> values, uint extent)
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
                    case Tile.GeomType.Point:
                        EncodePointGeometry(this.Geometry1, this.Geometry);
                        break;
                    case Tile.GeomType.LineString:
                        EncodeLineGeometry(this.Geometry1, this.Geometry);
                        break;
                    case Tile.GeomType.Polygon:
                        EncodePolygonGeometry(this.Geometry1, this.Geometry);
                        break;
                    default:
                        throw new System.Exception(string.Format("Unknown geometry type:{0}", this.Type));
                }
            }

            public static List<List<PointInt>> ParseGeometry(List<uint> geom, Tile.GeomType geomType)
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
                        if (geomType != Tile.GeomType.Point && !(coords.Count == 0))
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
                    Tile.Layer tileLayer = new Tile.Layer();
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

        [ProtoBuf.ProtoContract(Name = @"layer")]
        public sealed class Layer : ProtoBuf.IExtensible
        {
            uint _version;
            [ProtoBuf.ProtoMember(15, IsRequired = true, Name = @"version", DataFormat = ProtoBuf.DataFormat.Default)]
            public uint Version
            {
                get { return _version; }
                set { _version = value; }
            }
            string _name;
            [ProtoBuf.ProtoMember(1, IsRequired = true, Name = @"name", DataFormat = ProtoBuf.DataFormat.Default)]
            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }
            readonly System.Collections.Generic.List<Feature> _features = new System.Collections.Generic.List<Feature>();
            [ProtoBuf.ProtoMember(2, Name = @"features", DataFormat = ProtoBuf.DataFormat.Default)]
            public System.Collections.Generic.List<Feature> Features
            {
                get { return _features; }
            }

            readonly System.Collections.Generic.List<string> _keys = new System.Collections.Generic.List<string>();
            [ProtoBuf.ProtoMember(3, Name = @"keys", DataFormat = ProtoBuf.DataFormat.Default)]
            public System.Collections.Generic.List<string> Keys
            {
                get { return _keys; }
            }

            readonly System.Collections.Generic.List<Value> _values = new System.Collections.Generic.List<Value>();
            [ProtoBuf.ProtoMember(4, Name = @"values", DataFormat = ProtoBuf.DataFormat.Default)]
            public System.Collections.Generic.List<Value> Values
            {
                get { return _values; }
            }

            uint _extent = 4096;
            [ProtoBuf.ProtoMember(5, IsRequired = false, Name = @"extent", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
            [System.ComponentModel.DefaultValue((uint)4096)]
            public uint Extent
            {
                get { return _extent; }
                set { _extent = value; }
            }
            ProtoBuf.IExtension _extensionObject;
            ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            { return ProtoBuf.Extensible.GetExtensionObject(ref _extensionObject, createIfMissing); }
        }

        [ProtoBuf.ProtoContract(Name = @"GeomType")]
        public enum GeomType
        {

            [ProtoBuf.ProtoEnum(Name = @"Unknown", Value = 0)]
            Unknown = 0,

            [ProtoBuf.ProtoEnum(Name = @"Point", Value = 1)]
            Point = 1,

            [ProtoBuf.ProtoEnum(Name = @"LineString", Value = 2)]
            LineString = 2,

            [ProtoBuf.ProtoEnum(Name = @"Polygon", Value = 3)]
            Polygon = 3
        }

        ProtoBuf.IExtension _extensionObject;
        ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref _extensionObject, createIfMissing); }
    }
}