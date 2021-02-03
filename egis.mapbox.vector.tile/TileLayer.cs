using System.Collections.Generic;

namespace EGIS.Mapbox.Vector.Tile
{
    [ProtoBuf.ProtoContract(Name = @"layer")]
    public sealed class TileLayer : ProtoBuf.IExtensible
    {
        ProtoBuf.IExtension _extensionObject;

        public TileLayer()
        {
            Features = new List<TileFeature>();
            Values = new List<TileAttribute>();
            Keys = new List<string>();
            this.Extent = 4096;
            Version = 2;
        }

        [ProtoBuf.ProtoMember(15, IsRequired = true, Name = @"version", DataFormat = ProtoBuf.DataFormat.Default)]
        public uint Version { get; set; }

        [ProtoBuf.ProtoMember(1, IsRequired = true, Name = @"name", DataFormat = ProtoBuf.DataFormat.Default)]
        public string Name { get; set; }

        [ProtoBuf.ProtoMember(2, Name = @"features", DataFormat = ProtoBuf.DataFormat.Default)]
        public List<TileFeature> Features { get; }

        [ProtoBuf.ProtoMember(3, Name = @"keys", DataFormat = ProtoBuf.DataFormat.Default)]
        public List<string> Keys { get; }

        [ProtoBuf.ProtoMember(4, Name = @"values", DataFormat = ProtoBuf.DataFormat.Default)]
        public List<TileAttribute> Values { get; }

        [ProtoBuf.ProtoMember(5, IsRequired = false, Name = @"extent", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
        [System.ComponentModel.DefaultValue((uint)4096)]
        public uint Extent { get; set; }

        ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref _extensionObject, createIfMissing); }

        public void FillInTheInternalProperties()
        {
            //index the key value attributes
            List<string> keys = new List<string>();
            List<TileAttribute> values = new List<TileAttribute>();

            Dictionary<string, int> keysIndex = new Dictionary<string, int>();
            Dictionary<dynamic, int> valuesIndex = new Dictionary<dynamic, int>();

            foreach (var feature in this.Features)
            {
                foreach (var keyValue in feature.Attributes)
                {
                    if (!keysIndex.ContainsKey(keyValue.Key))
                    {
                        keysIndex.Add(keyValue.Key, keys.Count);
                        keys.Add(keyValue.Key);
                    }
                    if (!valuesIndex.ContainsKey(keyValue))
                    {
                        valuesIndex.Add(keyValue, values.Count);
                        values.Add(keyValue);
                    }
                }
            }

            for (int n = 0; n < this.Features.Count; ++n)
            {
                var feature = this.Features[n];
                feature.Id = (ulong)(n + 1);
                feature.GenerateNativeGeometry();
                foreach (var keyValue in feature.Attributes)
                {
                    feature.Tags.Add((uint)keysIndex[keyValue.Key]);
                    feature.Tags.Add((uint)valuesIndex[keyValue]);
                }
            }

            this.Keys.AddRange(keys);
            foreach (var value in values)
            {
                this.Values.Add(value);
            }
        }

        public void FillInTheExternalProperties()
        {
            foreach (var feature in this.Features)
            {
                feature.FillInTheExternalProperties(this.Keys, this.Values, this.Extent);
            }
        }
    }
}
