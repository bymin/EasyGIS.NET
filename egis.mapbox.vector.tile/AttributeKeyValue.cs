using System;
using System.Collections.Generic;
using System.Text;

namespace EGIS.Mapbox.Vector.Tile
{
    public class AttributeKeyValue
    {
        public AttributeKeyValue(string key, double val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.DoubleValue;
        }

        public AttributeKeyValue(string key, float val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.FloatValue;
        }

        public AttributeKeyValue(string key, string val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.StringValue;
        }

        public AttributeKeyValue(string key, bool val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.BoolValue;
        }

        public AttributeKeyValue(string key, System.Int64 val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.IntValue;
        }

        public AttributeKeyValue(string key, System.UInt64 val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.UIntValue;
        }

        public AttributeKeyValue(string key, dynamic val, AttributeType attributeType)
        {
            Key = key;
            Value = val;
            AttributeType = attributeType;
        }

        public string Key;
        public dynamic Value;
        public AttributeType AttributeType;

        public static Value ToTileValue(AttributeKeyValue value)
        {
            Value tileValue = new Value();
            if (value.AttributeType == AttributeType.StringValue)
            {
                tileValue.StringValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.BoolValue)
            {
                tileValue.BoolValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.DoubleValue)
            {
                tileValue.DoubleValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.FloatValue)
            {
                tileValue.FloatValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.IntValue)
            {
                tileValue.IntValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.UIntValue)
            {
                tileValue.UintValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.SIntValue)
            {
                tileValue.SintValue = value.Value;
            }
            else
            {
                throw new System.Exception(string.Format("Could not determine tileValue. valye type is {0}", value.GetType()));
            }
            return tileValue;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static List<AttributeKeyValue> Parse(List<string> keys, List<Value> values, List<uint> tags)
        {
            var result = new List<AttributeKeyValue>();

            for (var i = 0; i < tags.Count;)
            {
                var key = keys[(int)tags[i++]];
                var val = values[(int)tags[i++]];
                result.Add(GetAttr(key, val));
            }
            return result;
        }

        private static AttributeKeyValue GetAttr(string key, Value value)
        {
            AttributeKeyValue res = null;

            if (value.HasBoolValue)
            {
                res = new AttributeKeyValue(key, value.BoolValue);
            }
            else if (value.HasDoubleValue)
            {
                res = new AttributeKeyValue(key, value.DoubleValue);
            }
            else if (value.HasFloatValue)
            {
                res = new AttributeKeyValue(key, value.FloatValue);
            }
            else if (value.HasIntValue)
            {
                res = new AttributeKeyValue(key, value.IntValue);
            }
            else if (value.HasStringValue)
            {
                res = new AttributeKeyValue(key, value.StringValue);
            }
            else if (value.HasSIntValue)
            {
                res = new AttributeKeyValue(key, value.SintValue, AttributeType.SIntValue);
            }
            else if (value.HasUIntValue)
            {
                res = new AttributeKeyValue(key, value.UintValue);
            }
            return res;
        }
    }
}
