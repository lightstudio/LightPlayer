using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Light.Core;

namespace Light.Controls
{
    public class SliderValueChangeCompletedEventArgs : RoutedEventArgs
    {
        public double Value { get; }

        public SliderValueChangeCompletedEventArgs(double value)
        {
            Value = value;
        }
    }
    public delegate void SlideValueChangeCompletedEventHandler(object sender, SliderValueChangeCompletedEventArgs args);

    public class ExtendedSlider : Slider
    {
        public event SlideValueChangeCompletedEventHandler ValueChangeCompleted;
        private bool _dragging = false;

        protected void OnValueChangeCompleted(double value)
        {
            ValueChangeCompleted?.Invoke(this, new SliderValueChangeCompletedEventArgs(value));
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var thumb = base.GetTemplateChild("HorizontalThumb") as Thumb;
            if (thumb != null)
            {
                thumb.DragStarted += ThumbOnDragStarted;
                thumb.DragCompleted += ThumbOnDragCompleted;
            }
            thumb = base.GetTemplateChild("VerticalThumb") as Thumb;
            if (thumb != null)
            {
                thumb.DragStarted += ThumbOnDragStarted;
                thumb.DragCompleted += ThumbOnDragCompleted;
            }
        }

        private void ThumbOnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            _dragging = false;
            OnValueChangeCompleted(this.Value);
        }

        private void ThumbOnDragStarted(object sender, DragStartedEventArgs e)
        {
            _dragging = true;
            ValueChangeStarting?.Invoke(this, null);
        }

        protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            Core.PlaybackControl.Instance.SetPosition(TimeSpan.FromMilliseconds(this.Value));
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            if (!_dragging)
            {
                OnValueChangeCompleted(newValue);
            }
        }

        public event EventHandler ValueChangeStarting;
    }
}
