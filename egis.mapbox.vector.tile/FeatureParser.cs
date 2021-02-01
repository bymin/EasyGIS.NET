﻿using System.Collections.Generic;
using System.Globalization;

namespace EGIS.Mapbox.Vector.Tile
{
    internal static class FeatureParser
    {
        public static VectorTileFeature Parse(Tile.Feature feature, List<string> keys, List<Tile.Value> values,uint extent)
        {
            var result = new VectorTileFeature();
            var id = feature.Id;

            var geom =  GeometryParser.ParseGeometry(feature.Geometry, feature.Type);
            result.GeometryType = feature.Type;

            // add the geometry
            result.Geometry = geom;
            result.Extent = extent;

            // now add the attributes
            result.Id = id.ToString(CultureInfo.InvariantCulture);
            result.Attributes = AttributeKeyValue.Parse(keys, values, feature.Tags);
            return result;
        }

        //public static Tile.Feature  Encode(VectorTileFeature feature, List<string> keys, List<Tile.Value> values, uint extent)
        //{
        //    var result = new VectorTileFeature();
        //    var id = feature.Id;

        //    var geom = GeometryParser.ParseGeometry(feature.Geometry, feature.Type);
        //    result.GeometryType = feature.Type;

        //    // add the geometry
        //    result.Geometry = geom;
        //    result.Extent = extent;

        //    // now add the attributes
        //    result.Id = id.ToString(CultureInfo.InvariantCulture);
        //    result.Attributes = AttributesParser.Parse(keys, values, feature.Tags);
        //    return result;
        //}
    }
}
