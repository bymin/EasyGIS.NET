namespace MBTiles
{
    [ProtoBuf.ProtoContract(Name = @"value")]
    public sealed class TileAttribute : ProtoBuf.IExtensible
    {
        ProtoBuf.IExtension _extensionObject;

        public TileAttribute()
        { }
        public TileAttribute(string key, double val)
        {
            Key = key;
            this.DoubleValue = val;
        }
        public TileAttribute(string key, float val)
        {
            Key = key;
            this.FloatValue = val;
        }
        public TileAttribute(string key, string val)
        {
            Key = key;
            this.StringValue = val;
        }
        public TileAttribute(string key, bool val)
        {
            Key = key;
            this.BoolValue = val;
        }
        public TileAttribute(string key, long val, bool smallInt = false)
        {
            Key = key;
            if (smallInt)
            {
                this.IntValue = val;
            }
            else
            {
                this.SintValue = val;
            }
        }
        public TileAttribute(string key, ulong val)
        {
            Key = key;
            this.UintValue = val;
        }

        public string Key { get; set; }

        [ProtoBuf.ProtoMember(1, IsRequired = false, Name = @"string_value", DataFormat = ProtoBuf.DataFormat.Default)]
        [System.ComponentModel.DefaultValue("")]
        public string StringValue { get; set; }

        [ProtoBuf.ProtoMember(2, IsRequired = false, Name = @"float_value", DataFormat = ProtoBuf.DataFormat.FixedSize)]
        [System.ComponentModel.DefaultValue(default(float))]
        public float FloatValue { get; set; }

        [ProtoBuf.ProtoMember(3, IsRequired = false, Name = @"double_value", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
        [System.ComponentModel.DefaultValue(default(double))]
        public double DoubleValue { get; set; }

        [ProtoBuf.ProtoMember(4, IsRequired = false, Name = @"int_value", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
        [System.ComponentModel.DefaultValue(default(long))]
        public long IntValue { get; set; }

        [ProtoBuf.ProtoMember(5, IsRequired = false, Name = @"uint_value", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
        [System.ComponentModel.DefaultValue(default(ulong))]
        public ulong UintValue { get; set; }

        [ProtoBuf.ProtoMember(6, IsRequired = false, Name = @"sint_value", DataFormat = ProtoBuf.DataFormat.ZigZag)]
        [System.ComponentModel.DefaultValue(default(long))]
        public long SintValue { get; set; }

        [ProtoBuf.ProtoMember(7, IsRequired = false, Name = @"bool_value", DataFormat = ProtoBuf.DataFormat.Default)]
        [System.ComponentModel.DefaultValue(default(bool))]
        public bool BoolValue { get; set; }

        ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref _extensionObject, createIfMissing); }
    }
}
