using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MBTiles
{
    [ProtoBuf.ProtoContract(Name = @"feature")]
    public sealed class TileFeature : ProtoBuf.IExtensible
    {
        const uint MoveTo = 1;
        const uint LineTo = 2;
        const uint ClosePath = 7;

        ProtoBuf.IExtension _extensionObject;

        public TileFeature()
        {
            Tags = new List<uint>();
            Type = GeometryType.Unknown;
            nativeGeometry = new List<uint>();
            Geometry = new List<List<PointInt>>();
            Attributes = new List<TileAttribute>();
        }

        [ProtoBuf.ProtoMember(1, IsRequired = false, Name = @"id", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
        [DefaultValue(default(ulong))]
        public ulong Id { get; set; }

        [ProtoBuf.ProtoMember(2, Name = @"tags", DataFormat = ProtoBuf.DataFormat.TwosComplement, Options = ProtoBuf.MemberSerializationOptions.Packed)]
        public List<uint> Tags { get; }

        [ProtoBuf.ProtoMember(3, IsRequired = false, Name = @"type", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
        [DefaultValue(GeometryType.Unknown)]
        public GeometryType Type { get; set; }

        [ProtoBuf.ProtoMember(4, Name = @"geometry", DataFormat = ProtoBuf.DataFormat.TwosComplement, Options = ProtoBuf.MemberSerializationOptions.Packed)]
        private List<uint> nativeGeometry { get; set; }

        public List<List<PointInt>> Geometry { get; set; }

        public List<TileAttribute> Attributes { get; set; }

        public uint Extent { get; set; }
        ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref _extensionObject, createIfMissing); }

        public void FillInTheExternalProperties(List<string> keys, List<TileAttribute> values, uint extent)
        {
            this.Extent = extent;
            this.Geometry = GetGeometry(this.nativeGeometry, this.Type);
            this.Attributes = new List<TileAttribute>();

            for (var i = 0; i < this.Tags.Count;)
            {
                var key = keys[(int)this.Tags[i++]];
                var val = values[(int)this.Tags[i++]];
                val.Key = key;
                this.Attributes.Add(val);
            }
        }

        public void FillInNativeGeometry()
        {
            switch (this.Type)
            {
                case GeometryType.Point:
                    this.nativeGeometry = EncodePointGeometry(this.Geometry);
                    break;
                case GeometryType.LineString:
                    this.nativeGeometry = EncodeLineGeometry(this.Geometry);
                    break;
                case GeometryType.Polygon:
                    this.nativeGeometry = EncodePolygonGeometry(this.Geometry);
                    break;
                default:
                    throw new Exception(string.Format("Unknown geometry type:{0}", this.Type));
            }
        }

        private static List<List<PointInt>> GetGeometry(List<uint> geom, GeometryType geomType)
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
                    if (command == MoveTo)
                    {
                        coords = new List<PointInt>();
                        coordsList.Add(coords);
                    }
                }

                if (command == ClosePath)
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

        private static List<uint> EncodePointGeometry(List<List<PointInt>> coordList)
        {
            List<uint> geometry = new List<uint>();
            PointInt prevCoord = new PointInt() { X = 0, Y = 0 };

            foreach (List<PointInt> points in coordList)
            {
                if (points.Count == 0) throw new Exception("Encoding with no points. ");

                uint commandInteger = (MoveTo & 0x7) | ((uint)points.Count << 3);
                geometry.Add(commandInteger);
                for (int n = 0; n < points.Count; ++n)
                {
                    int dx = points[n].X - prevCoord.X;
                    int dy = points[n].Y - prevCoord.Y;
                    int parameter = ZigZag.Encode(dx);
                    geometry.Add((uint)parameter);
                    parameter = ZigZag.Encode(dy);
                    geometry.Add((uint)parameter);
                    prevCoord = points[n];
                }
            }
            return geometry;
        }

        private static List<uint> EncodeLineGeometry(List<List<PointInt>> coordList)
        {
            List<uint> geometry = new List<uint>();

            PointInt prevCoord = new PointInt() { X = 0, Y = 0 };
            foreach (List<PointInt> points in coordList)
            {
                if (points.Count == 0) throw new Exception("Encoding with no points. ");

                //start of linestring
                uint commandInteger = (MoveTo & 0x7) | (1 << 3);
                geometry.Add(commandInteger);

                int dx = points[0].X - prevCoord.X;
                int dy = points[0].Y - prevCoord.Y;

                int parameter = ZigZag.Encode(dx);
                geometry.Add((uint)parameter);
                parameter = ZigZag.Encode(dy);
                geometry.Add((uint)parameter);

                //encode the rest of the points
                commandInteger = (LineTo & 0x7) | ((uint)(points.Count - 1) << 3);
                geometry.Add(commandInteger);
                for (int n = 1; n < points.Count; ++n)
                {
                    dx = points[n].X - points[n - 1].X;
                    dy = points[n].Y - points[n - 1].Y;
                    parameter = ZigZag.Encode(dx);
                    geometry.Add((uint)parameter);
                    parameter = ZigZag.Encode(dy);
                    geometry.Add((uint)parameter);
                }
                prevCoord = points[points.Count - 1];
            }
            return geometry;
        }

        private static List<uint> EncodePolygonGeometry(List<List<PointInt>> coordList)
        {
            List<uint> geometry = new List<uint>();

            PointInt prevCoord = new PointInt() { X = 0, Y = 0 };
            foreach (List<PointInt> points in coordList)
            {
                if (points.Count == 0) throw new Exception("Encoding with no points. ");

                //start of ring
                uint commandInteger = (MoveTo & 0x7) | (1 << 3);
                geometry.Add(commandInteger);

                int dx = points[0].X - prevCoord.X;
                int dy = points[0].Y - prevCoord.Y;

                int parameter = ZigZag.Encode(dx);
                geometry.Add((uint)parameter);
                parameter = ZigZag.Encode(dy);
                geometry.Add((uint)parameter);

                bool lastPointRepeated = (points[points.Count - 1].X == points[0].X && points[points.Count - 1].Y == points[0].Y);

                int pointCount = lastPointRepeated ? points.Count - 2 : points.Count - 1;

                //encode the rest of the points
                commandInteger = (LineTo & 0x7) | ((uint)(pointCount) << 3);
                geometry.Add(commandInteger);
                for (int n = 1; n <= pointCount; ++n)
                {
                    dx = points[n].X - points[n - 1].X;
                    dy = points[n].Y - points[n - 1].Y;
                    parameter = ZigZag.Encode(dx);
                    geometry.Add((uint)parameter);
                    parameter = ZigZag.Encode(dy);
                    geometry.Add((uint)parameter);
                }

                //close path
                commandInteger = (ClosePath & 0x7) | (1 << 3);
                geometry.Add(commandInteger);

                prevCoord = points[pointCount];
            }

            return geometry;
        }

        public static class ZigZag
        {
            public static Int32 Decode(Int32 n)
            {
                return (n >> 1) ^ (-(n & 1));
            }

            public static Int32 Encode(Int32 n)
            {
                return (n << 1) ^ (n >> 31);
            }
        }
    }
}