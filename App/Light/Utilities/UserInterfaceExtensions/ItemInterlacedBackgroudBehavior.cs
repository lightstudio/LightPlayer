using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Microsoft.Xaml.Interactivity;

namespace Light.Utilities.UserInterfaceExtensions
{
    [ContentProperty(Name = "Brushes")]
    public sealed class ItemInterlacedBackgroudBehavior : Behavior<ListViewBase>
    {
        public static readonly DependencyProperty BrushesProperty =
            DependencyProperty.Register("Brushes", 
                typeof(DependencyObjectCollection), 
                typeof(ItemInterlacedBackgroudBehavior), 
                new PropertyMetadata(null));

        public DependencyObjectCollection Brushes
        {
            get
            {
                var c = (DependencyObjectCollection) GetValue(BrushesProperty);
                if (c == null)
                    SetValue(BrushesProperty, c = new DependencyObjectCollection());

                return c;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ContainerContentChanging += OnContainerContentChanging;
        }

        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            args.ItemContainer.Background = 
                (Windows.UI.Xaml.Media.Brush) Brushes[args.ItemIndex % Brushes.Count];
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.ContainerContentChanging -= OnContainerContentChanging;
        }
    }
}
