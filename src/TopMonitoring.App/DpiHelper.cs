using System.Windows;
using System.Windows.Media;

namespace TopMonitoring.App
{
    internal static class DpiHelper
    {
        public static System.Windows.Point FromPixelsPoint(Window window, double x, double y)
        {
            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget == null) return new System.Windows.Point(x, y);
            var transform = source.CompositionTarget.TransformFromDevice;
            return transform.Transform(new System.Windows.Point(x, y));
        }

        public static System.Windows.Size FromPixelsSize(Window window, double width, double height)
        {
            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget == null) return new System.Windows.Size(width, height);
            var transform = source.CompositionTarget.TransformFromDevice;
            var size = transform.Transform(new System.Windows.Point(width, height));
            return new System.Windows.Size(size.X, size.Y);
        }
    }
}
