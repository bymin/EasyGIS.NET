namespace MBTilesGenerator
{
    public struct RectangleInt
    {
        public int XMin { get; set; }
        public int XMax { get; set; }
        public int YMin { get; set; }
        public int YMax { get; set; }

        public override string ToString()
        {
            return string.Format("RectangleInt XMin:{0}, XMax:{1}, YMin:{2}, YMax:{3}", XMin, XMax, YMin, YMax);
        }
    }
}