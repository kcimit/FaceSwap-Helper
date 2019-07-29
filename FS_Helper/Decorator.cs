using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FS_Helper
{
    public class DpiDecorator : Decorator
    {
        public DpiDecorator()
        {
            this.Loaded += (s, e) =>
            {
                var m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
                var dpiTransform = new ScaleTransform(1 / m.M11, 1 / m.M22);
                if (dpiTransform.CanFreeze)
                    dpiTransform.Freeze();
                this.LayoutTransform = dpiTransform;
            };
        }
    };
}