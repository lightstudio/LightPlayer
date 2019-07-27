using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Light.Model;

namespace Light.Controls
{
    public sealed partial class DetailedPageHeaderControl : UserControl
    {
        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register(nameof(DataType), typeof (CommonItemType), typeof (DetailedPageHeaderControl),
                new PropertyMetadata(default(CommonItemType), DataTypeChanged));

        public static readonly DependencyProperty DataEntityIdProperty =
            DependencyProperty.Register(nameof(DataEntityId), typeof (int), typeof (DetailedPageHeaderControl),
                new PropertyMetadata(default(int), DataEntityIdChanged));

        public DetailedPageHeaderControl()
        {
            InitializeComponent();
        }

        public CommonItemType DataType
        {
            get { return (CommonItemType) GetValue(DataTypeProperty); }
            set { SetValue(DataTypeProperty, value); }
        }

        public int DataEntityId
        {
            get { return (int) GetValue(DataEntityIdProperty); }
            set { SetValue(DataEntityIdProperty, value); }
        }

        private static void DataEntityIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private static void DataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}