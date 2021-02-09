﻿
namespace MBTiles
{
    [ProtoBuf.ProtoContract(Name = @"GeomType")]
    public enum GeometryType
    {
        [ProtoBuf.ProtoEnum(Name = @"Unknown")]
        Unknown = 0,

        [ProtoBuf.ProtoEnum(Name = @"Point")]
        Point = 1,

        [ProtoBuf.ProtoEnum(Name = @"LineString")]
        LineString = 2,

        [ProtoBuf.ProtoEnum(Name = @"Polygon")]
        Polygon = 3
    }
}
