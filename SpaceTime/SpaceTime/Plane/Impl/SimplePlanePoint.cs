#region usings

using System.Drawing;

#endregion

namespace Composable.SpaceTime.Plane.Impl
{
    internal class SimplePlanePoint : IPlanePoint
    {
        private readonly Point _point;

        private SimplePlanePoint(Point position)
        {
            _point = position;
        }

        protected SimplePlanePoint(IPlanePoint point) : this(point.AsPoint())
        {
        }

        public IPlanePoint PlanePosition { get { return this; } }

        public Point AsPoint()
        {
            return _point;
        }

        public static IPlanePoint FromXAndY(int x, int y)
        {
            return new SimplePlanePoint(new Point(x, y));
        }
    }
}