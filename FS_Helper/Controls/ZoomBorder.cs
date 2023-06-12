using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace FS_Helper
{
    public class ZoomBorder : Border
    {
        private UIElement child = null;
        private Point origin;
        private Point start;

        public static DependencyProperty ImageTranslateTransformProperty = DependencyProperty.Register("ImageTranslateTransform", typeof(TranslateTransform),typeof(Border), new PropertyMetadata(OnImageTranslateTransformValueChanged));
        public static DependencyProperty ImageScaleTransformProperty = DependencyProperty.Register("ImageScaleTransform", typeof(ScaleTransform), typeof(Border), new PropertyMetadata(OnImageScaleTransformValueChanged));

        private static void OnImageTranslateTransformValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue!=e.OldValue)
            {
                var tt = (TranslateTransform)((TransformGroup)((ZoomBorder)d).child.RenderTransform).Children.First(tr => tr is TranslateTransform);
                tt.X = ((TranslateTransform)e.NewValue).X;
                tt.Y = ((TranslateTransform)e.NewValue).Y;
            }
        }

        private static void OnImageScaleTransformValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                var tt = (ScaleTransform)((TransformGroup)((ZoomBorder)d).child.RenderTransform).Children.First(tr => tr is ScaleTransform);
                tt.ScaleX = ((ScaleTransform)e.NewValue).ScaleX;
                tt.ScaleY = ((ScaleTransform)e.NewValue).ScaleY;
            }
        }

        public TranslateTransform ImageTranslateTransform
        {
            get { return (TranslateTransform)GetValue(ImageTranslateTransformProperty); }
            set 
            {
                SetValue(ImageTranslateTransformProperty, value.Clone());
            }
        }

        public ScaleTransform ImageScaleTransform
        {
            get { return (ScaleTransform)GetValue(ImageScaleTransformProperty); }
            set { SetValue(ImageScaleTransformProperty, value.Clone()); }
        }

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            if (child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);

                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseMove += child_MouseMove;
                this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                  child_PreviewMouseRightButtonDown);
            }
        }

        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;
                ImageScaleTransform = st; 
                ImageTranslateTransform = tt;
            }
        }

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;

                Point relative = e.GetPosition(child);
                double abosuluteX;
                double abosuluteY;

                abosuluteX = relative.X * st.ScaleX + tt.X;
                abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;
                ImageScaleTransform = st;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
                ImageTranslateTransform = tt;
            }
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                var tt = GetTranslateTransform(child);
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                child.CaptureMouse();
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
                var tt = GetTranslateTransform(child);
                ImageTranslateTransform = tt;
            }
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Reset();
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child != null)
            {
                if (child.IsMouseCaptured)
                {
                    var tt = GetTranslateTransform(child);
                    Vector v = start - e.GetPosition(this);
                    tt.X = origin.X - v.X;
                    tt.Y = origin.Y - v.Y;
                }
            }
        }

        #endregion
    }
}
