#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.ShapeFileLib class library of Easy GIS .NET.
** 
** Easy GIS .NET is free software: you can redistribute it and/or modify
** it under the terms of the GNU Lesser General Public License version 3 as
** published by the Free Software Foundation and appearing in the file
** lgpl-license.txt included in the packaging of this file.
**
** Easy GIS .NET is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License and
** GNU Lesser General Public License along with Easy GIS .NET.
** If not, see <http://www.gnu.org/licenses/>.
**
****************************************************************************/

#endregion


using System;
using System.Collections.Generic;
using System.Text;
using ThinkGeo.Core;

namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// Utility class with methods to convert Lat Long locations to Tile Coordinates
    /// </summary>
    /// <remarks>
    /// <para>
    /// The TileUtil class is used by the EGIS.Web.Controls TiledSFMap to convert map tile image
    /// requests to actual Lat Long position and zoom level for rendering purposes
    /// </para>
    /// <para>Note that in order to use Map tiles the <see cref="EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection"/> property
    /// must be set to true</para>
    /// <para>
    /// Tiles are organised in a manner similar to the approach used by google maps and bing maps. Each tiles dimension is by default 256x256 pixels.
    /// A tile request is made up of a zoom-level between 0 and 16(inclusive), tile x-ccord and a tile y-coord. At zoom-level
    /// 0 the entire world (-180 lon -> +180 lon) is scaled to fit in 1 tile. At level 1 the world will fit
    /// in 2 tiles x 2 tiles, at level 2 the world will fit into 4 tiles x 4 tiles, .. etc.     
    /// </para>
    /// <para>Tiles are numbered from zero in the upper left corner to (NumTiles at zoom-level)-1 as below:</para>
    /// <para>
    /// <code>
    /// (0,0) (1,0) (2,0) ..
    /// (0,1) (1,1) (2,1) ..
    /// (0,2) (1,2) (2,2) ..
    /// ..
    /// </code>      
    /// </para>
    /// <seealso cref="EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection"/>
    /// </remarks>
    
    public sealed class TileUtil
    {
        private TileUtil()
        {
        }


        /// <summary>
        /// Returns the centre point (in Mercator Projection coordinates) of a given tile
        /// </summary>
        /// <param name="tileX">zero based tile x-coord</param>
        /// <param name="tileY">zero based tile y-ccord</param>
        /// <param name="zoomLevel"></param>
        /// /// <exception cref="System.ArgumentException">If zoomLevel less than zero</exception>
        /// <returns></returns>
        public static PointD GetMercatorCenterPointFromTile(int tileX, int tileY, int zoomLevel, int tileSize=256)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", "zoomLevel");
            return PixelToMerc((tileSize>>1) + (tileX * tileSize), (tileSize>>1)+(tileY * tileSize), zoomLevel, tileSize);
        }

        public static RectangleD GetTileLatLonBounds(int tileX, int tileY, int zoomLevel, int tileSize=256)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", "zoomLevel");
            PointD topLeft = PixelToLL((tileX * tileSize), (tileY * tileSize), zoomLevel, tileSize);
            PointD bottomRight = PixelToLL(((tileX+1) * tileSize), ((tileY+1) * tileSize), zoomLevel, tileSize);
            return RectangleD.FromLTRB(topLeft.X, bottomRight.Y, bottomRight.X, topLeft.Y);
        }


        public static RectangleD GetTileSphericalMercatorBounds(long tileX, long tileY, int zoomLevel, int tileSize = 256)
        {
            SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
            //double currentScale = zoomLevelSet.CustomZoomLevels[zoomLevel].Scale;
            double currentScale = GetZoomLevelIndex(zoomLevelSet, zoomLevel);
            var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, 512, 512, GeographyUnit.Meter);
            RectangleShape bbox = tileMatrix.GetCell(tileX, tileY).BoundingBox;
            return RectangleD.FromLTRB(bbox.UpperLeftPoint.X, bbox.LowerRightPoint.Y, bbox.LowerRightPoint.X, bbox.UpperLeftPoint.Y);
        }

        public static double GetZoomLevelIndex(ZoomLevelSet zoomLevelSet, int zoomLevel)
        {
            switch (zoomLevel)
            {
                case 0: return zoomLevelSet.ZoomLevel01.Scale;
                case 1: return zoomLevelSet.ZoomLevel02.Scale;
                case 2: return zoomLevelSet.ZoomLevel03.Scale;
                case 3: return zoomLevelSet.ZoomLevel04.Scale;
                case 4: return zoomLevelSet.ZoomLevel05.Scale;
                case 5: return zoomLevelSet.ZoomLevel06.Scale;
                case 6: return zoomLevelSet.ZoomLevel07.Scale;
                case 7: return zoomLevelSet.ZoomLevel08.Scale;
                case 8: return zoomLevelSet.ZoomLevel09.Scale;
                case 9: return zoomLevelSet.ZoomLevel10.Scale;
                case 10: return zoomLevelSet.ZoomLevel11.Scale;
                case 11: return zoomLevelSet.ZoomLevel12.Scale;
                case 12: return zoomLevelSet.ZoomLevel13.Scale;
                case 13: return zoomLevelSet.ZoomLevel14.Scale;
                case 14: return zoomLevelSet.ZoomLevel15.Scale;
                case 15: return zoomLevelSet.ZoomLevel16.Scale;
                case 16: return zoomLevelSet.ZoomLevel17.Scale;
                case 17: return zoomLevelSet.ZoomLevel18.Scale;
                case 18: return zoomLevelSet.ZoomLevel19.Scale;
                case 19: return zoomLevelSet.ZoomLevel20.Scale;
                default:
                    return -1;
            }
        }

        private const long l = 1;
        /// <summary>
        /// Converts a zoomLevel to its equivalent double-precision scaling
        /// </summary>
        /// <exception cref="System.ArgumentException">If zoomLevel less than zero</exception>
        /// <param name="zoomLevel"></param>
        /// <returns></returns>
        public static double ZoomLevelToScale(int zoomLevel, int tileSize=256)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", "zoomLevel");
            return ((double)tileSize/360.0)*(l<<zoomLevel);
        }

        /// <summary>
        /// Converts a double-precision scaling to equivalent tile zoom-level
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static int ScaleToZoomLevel(double scale, int tileSize=256)
        {
            return (int)Math.Round(Math.Log(scale*360/(double)tileSize, 2));
        }

        static Dictionary<string, RectangleShape> dictionaryCache = new Dictionary<string, RectangleShape>();


        public static void LLToPixel2(PointD sphericalMercator, int zoomLevel, int tileX, int tileY, out long x, out long y, int tileSize = 256)
        {
            string key = $"{zoomLevel}-{tileX}-{tileY}";
            if (!dictionaryCache.ContainsKey(key))
            {
                SphericalMercatorZoomLevelSet zoomLevelSet = new SphericalMercatorZoomLevelSet();
                double currentScale = GetZoomLevelIndex(zoomLevelSet, zoomLevel);
                var tileMatrix = TileMatrix.GetDefaultMatrix(currentScale, 512, 512, GeographyUnit.Meter);
                dictionaryCache.Add(key, tileMatrix.GetCell(tileX, tileY).BoundingBox);
            }

            RectangleShape bbox = dictionaryCache[key];



            double scale = ((double)tileSize / 40075016.4629396) * (l << zoomLevel);
            x = (long)Math.Round((sphericalMercator.X - bbox.LowerLeftPoint.X) * scale);

            y = (long)Math.Round((bbox.UpperLeftPoint.Y - sphericalMercator.Y) * scale);

        }

        public static PointD PixelToLL(int pixX, int pixY, int zoomLevel, int tileSize=256)
        {
            return ShapeFile.MercatorToLL(PixelToMerc(pixX, pixY, zoomLevel, tileSize));
        }

        public static PointD PixelToMerc(int pixX, int pixY, int zoomLevel, int tileSize=256)
        {
            double d = 1.0 / ZoomLevelToScale(zoomLevel, tileSize);
            return new PointD((d * pixX) - 180, 180 - (d * pixY));
        }

        public static void NormaliseTileCoordinates(ref int tileX, ref int tileY, int zoomLevel)
        {
            if (zoomLevel < 0) return;
            int maxTilesAtZoomLevel = 1 << zoomLevel;
            tileX = tileX % maxTilesAtZoomLevel;
            if (tileX < 0) tileX += maxTilesAtZoomLevel;
            tileY = tileY % maxTilesAtZoomLevel;
            if (tileY < 0) tileY += maxTilesAtZoomLevel;
        }
        
    }


}
