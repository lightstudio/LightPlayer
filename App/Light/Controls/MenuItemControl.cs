using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Light.Controls
{
    public sealed class MenuItemControl : Control
    {


        public string Glyph
        {
            get { return (string)GetValue(GlyphProperty); }
            set { SetValue(GlyphProperty, value); }
        }
        
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register("Glyph", typeof(string), typeof(MenuItemControl), new PropertyMetadata(string.Empty));
        
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MenuItemControl), new PropertyMetadata(string.Empty));


        StackPanel RootPanel;

        public MenuItemControl()
        {
            this.DefaultStyleKey = typeof(MenuItemControl);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            RootPanel = (StackPanel)GetTemplateChild(nameof(RootPanel));
            RootPanel.PointerPressed += OnRootPanelPointerPressed;
            RootPanel.PointerReleased += OnRootPanelPointerReleased;
            RootPanel.PointerCaptureLost += OnRootPanelPointerCaptureLost;
        }

        private void OnRootPanelPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Released", true);
            RootPanel.ReleasePointerCapture(e.Pointer);
        }

        private void OnRootPanelPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Released", true);
            RootPanel.ReleasePointerCapture(e.Pointer);
        }

        private void OnRootPanelPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            RootPanel.CapturePointer(e.Pointer);
            VisualStateManager.GoToState(this, "Pressed", true);
        }
    }
}
