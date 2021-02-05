namespace MBTilesGenerator
{
    public struct PointInt
    {
        public PointInt(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public override bool Equals(object obj)
        {
            return Equals((PointInt)obj);
        }

        private bool Equals(PointInt pointI)
        {
            return ((X == pointI.X) && (Y == pointI.Y));
        }

        public static bool operator ==(PointInt a, PointInt b)
        {
            return ((a.X == b.X) && (a.Y == b.Y));
        }

        public static bool operator !=(PointInt a, PointInt b)
        {
            return !((a.X == b.X) && (a.Y == b.Y));
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
    }
}
