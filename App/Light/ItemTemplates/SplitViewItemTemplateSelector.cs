using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Light.DataObjects;

namespace Light.ItemTemplates
{
    public class SplitViewItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BuiltInSymbolItemTemplate { get; set; }
        public DataTemplate CustomFontSymbolItemTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is SplitViewFeatureModel)
            {
                var dataEntity = (SplitViewFeatureModel) item;
                return (!string.IsNullOrEmpty(dataEntity.FontIconGlyphOverride))
                    ? CustomFontSymbolItemTemplate
                    : BuiltInSymbolItemTemplate;
            }

            return BuiltInSymbolItemTemplate;
        }
    }
}
