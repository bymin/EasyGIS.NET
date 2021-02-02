using System.Collections.Generic;

namespace EGIS.Mapbox.Vector.Tile
{
    [ProtoBuf.ProtoContract(Name = @"tile")]
    public sealed class Tile : ProtoBuf.IExtensible
    {
        readonly List<TileLayer> _layers = new List<TileLayer>();

        [ProtoBuf.ProtoMember(3, Name = @"layers", DataFormat = ProtoBuf.DataFormat.Default)]
        public List<TileLayer> Layers
        {
            get { return _layers; }
        }

        ProtoBuf.IExtension _extensionObject;

        ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref _extensionObject, createIfMissing); }
    }
}