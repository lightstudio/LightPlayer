using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Light.Utilities
{
    class AlternatingColorItemContainerStyleSelector : StyleSelector
    {
        private Style _oddStyle = new Style { TargetType = typeof(ListViewItem) }, _evenStyle = new Style { TargetType = typeof(ListViewItem) };
        public Style OddStyle { get { return _oddStyle; } }
        public Style EvenStyle { get { return _evenStyle; } }

        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            var listViewItem = (ListViewItem)container;
            var listView = GetParent<ListView>(listViewItem);

            var index = listView.IndexFromContainer(listViewItem);

            if (index % 2 == 0)
            {
                return EvenStyle;
            }
            else
            {
                return OddStyle;
            }
        }

        public static T GetParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (!(child is T))
            {
                child = VisualTreeHelper.GetParent(child);
            }

            return (T)child;
        }
    }
    public class ListViewAlternatingColorBehavior : DependencyObject, IBehavior
    {
        public DependencyObject AssociatedObject { get; set; }

        public Style SharedItemContainerStyle { get; set; }

        #region colors dp

        public SolidColorBrush OddBrush
        {
            get { return (SolidColorBrush)GetValue(OddBrushProperty); }
            set { SetValue(OddBrushProperty, value); }
        }

        public static readonly DependencyProperty OddBrushProperty =
            DependencyProperty.Register("OddBrush", typeof(SolidColorBrush), typeof(ListViewAlternatingColorBehavior), new PropertyMetadata(null, OnOddBrushChanged));

        public SolidColorBrush EvenBrush
        {
            get { return (SolidColorBrush)GetValue(EvenBrushProperty); }
            set { SetValue(EvenBrushProperty, value); }
        }

        public static readonly DependencyProperty EvenBrushProperty =
            DependencyProperty.Register("EvenBrush", typeof(SolidColorBrush), typeof(ListViewAlternatingColorBehavior), new PropertyMetadata(null, OnEvenBrushChanged));

        public Thickness Margin
        {
            get { return (Thickness)GetValue(MarginProperty); }
            set { SetValue(MarginProperty, value); }
        }

        public static readonly DependencyProperty MarginProperty =
            DependencyProperty.Register(nameof(Margin), typeof(Thickness), typeof(ListViewAlternatingColorBehavior), new PropertyMetadata(new Thickness(0, 0, 0, 0)));

        #endregion

        private static void OnOddBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = (ListViewAlternatingColorBehavior)d;
            if (obj.AssociatedObject != null)
            {
                var lv = (ListView)obj.AssociatedObject;
                var itemContainerStyleSelector = new AlternatingColorItemContainerStyleSelector();
                itemContainerStyleSelector.OddStyle.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = obj.OddBrush });
                itemContainerStyleSelector.EvenStyle.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = obj.EvenBrush });
                itemContainerStyleSelector.OddStyle.Setters.Add(new Setter { Property = Control.HorizontalContentAlignmentProperty, Value = HorizontalAlignment.Stretch });
                itemContainerStyleSelector.EvenStyle.Setters.Add(new Setter { Property = Control.HorizontalContentAlignmentProperty, Value = HorizontalAlignment.Stretch });
                itemContainerStyleSelector.OddStyle.Setters.Add(new Setter { Property = FrameworkElement.MarginProperty, Value = obj.Margin });
                itemContainerStyleSelector.EvenStyle.Setters.Add(new Setter { Property = FrameworkElement.MarginProperty, Value = obj.Margin });
                lv.ItemContainerStyleSelector = itemContainerStyleSelector;
            }
        }

        private static void OnEvenBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = (ListViewAlternatingColorBehavior)d;
            if (obj.AssociatedObject != null)
            {
                var lv = (ListView)obj.AssociatedObject;
                var itemContainerStyleSelector = new AlternatingColorItemContainerStyleSelector();
                itemContainerStyleSelector.OddStyle.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = obj.OddBrush });
                itemContainerStyleSelector.EvenStyle.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = obj.EvenBrush });
                itemContainerStyleSelector.OddStyle.Setters.Add(new Setter { Property = Control.HorizontalContentAlignmentProperty, Value = HorizontalAlignment.Stretch });
                itemContainerStyleSelector.EvenStyle.Setters.Add(new Setter { Property = Control.HorizontalContentAlignmentProperty, Value = HorizontalAlignment.Stretch });
                itemContainerStyleSelector.OddStyle.Setters.Add(new Setter { Property = FrameworkElement.MarginProperty, Value = obj.Margin });
                itemContainerStyleSelector.EvenStyle.Setters.Add(new Setter { Property = FrameworkElement.MarginProperty, Value = obj.Margin });
                lv.ItemContainerStyleSelector = itemContainerStyleSelector;
            }
        }

        public void Attach(DependencyObject associatedObject)
        {
            AssociatedObject = associatedObject;

            ApplyItemContainerStyleSelectors();
        }

        private void ApplyItemContainerStyleSelectors()
        {
            var itemContainerStyleSelector = new AlternatingColorItemContainerStyleSelector();

            if (SharedItemContainerStyle != null)
            {
                itemContainerStyleSelector.OddStyle.BasedOn = SharedItemContainerStyle;
                itemContainerStyleSelector.EvenStyle.BasedOn = SharedItemContainerStyle;
            }

            itemContainerStyleSelector.OddStyle.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = OddBrush });
            itemContainerStyleSelector.EvenStyle.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = EvenBrush });
            itemContainerStyleSelector.OddStyle.Setters.Add(new Setter { Property = Control.HorizontalContentAlignmentProperty, Value = HorizontalAlignment.Stretch});
            itemContainerStyleSelector.EvenStyle.Setters.Add(new Setter { Property = Control.HorizontalContentAlignmentProperty, Value = HorizontalAlignment.Stretch });
            itemContainerStyleSelector.OddStyle.Setters.Add(new Setter { Property = FrameworkElement.MarginProperty, Value = Margin });
            itemContainerStyleSelector.EvenStyle.Setters.Add(new Setter { Property = FrameworkElement.MarginProperty, Value = Margin });

            var listView = (ListView)AssociatedObject;
            listView.ItemContainerStyleSelector = itemContainerStyleSelector;
        }

        public void Detach()
        {
        }
    }
}
