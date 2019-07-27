using System;
using Windows.UI.Xaml.Controls;

namespace Light.DataObjects
{
    public class SplitViewFeatureModel
    {
        public string Name { get; set; }
        public Symbol Symbol { get; set; }
        public string FontIconGlyphOverride { get; set; }
        public Type FeaturePageType { get; set; }

        public SplitViewFeatureModel(string name, Symbol symbol, Type pageType)
        {
            Name = name;
            Symbol = symbol;
            FontIconGlyphOverride = string.Empty;
        }

        public SplitViewFeatureModel(string name, string fontIconOverride, Type pageType)
        {
            Name = name;
            Symbol = Symbol.Emoji;
            FontIconGlyphOverride = fontIconOverride;
        }
    }
}
