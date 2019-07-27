using Light.Utilities;
using Microsoft.Graphics.Canvas.Effects;
using System;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Light.Controls
{
    /// <summary>
    /// Base backdrop implementation that supports both host/local backdrop scenarios.
    /// </summary>
    public class BaseBackDrop : Control
    {
        private Compositor m_compositor;
        private SpriteVisual m_blurVisual;
        private CompositionBrush m_blurBrush;
        private Visual m_rootVisual;

        private bool m_setUpExpressions;
        private CompositionSurfaceBrush m_noiseBrush;

        public Color TintColor
        {
            get { return (Color)GetValue(TintColorProperty); }
            set
            {
                SetValue(TintColorProperty, value);
            }
        }
        
        public static readonly DependencyProperty TintColorProperty =
            DependencyProperty.Register(nameof(TintColor), typeof(Color), typeof(BaseBackDrop), new PropertyMetadata(Colors.Transparent, OnTintColorChanged));

        private static void OnTintColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = (BaseBackDrop)d;
            if (!obj.m_setUpExpressions)
            {
                obj.m_blurBrush.Properties.InsertColor("Color.Color", obj.TintColor);
            }
            obj.m_rootVisual.Properties.InsertColor("TintColor", obj.TintColor);
        }


        /// <summary>
        /// Initializes new instance of the <see cref="BaseBackDrop"/> class.
        /// </summary>
        /// <param name="useHostBackdrop">Enables host backdrop is platform supports it.</param>
        public BaseBackDrop(bool useHostBackdrop = false)
        {
            m_rootVisual = ElementCompositionPreview.GetElementVisual(this as UIElement);
            Compositor = m_rootVisual.Compositor;

            m_blurVisual = Compositor.CreateSpriteVisual();
            m_noiseBrush = Compositor.CreateSurfaceBrush();

            CompositionEffectBrush brush = BuildBlurBrush();
            if (useHostBackdrop && IsHostBackDropSupported)
            {
                brush.SetSourceParameter("source", m_compositor.CreateHostBackdropBrush());
            }
            else
            {
                brush.SetSourceParameter("source", m_compositor.CreateBackdropBrush());
            }

            m_blurBrush = brush;
            m_blurVisual.Brush = m_blurBrush;

            BlurAmount = 9;
            //TintColor = Colors.Transparent;
            ElementCompositionPreview.SetElementChildVisual(this as UIElement, m_blurVisual);

            this.Loading += OnLoading;
            this.Unloaded += OnUnloaded;
        }

        public const string BlurAmountProperty = nameof(BlurAmount);
        //public const string TintColorProperty = nameof(TintColor);

        public double BlurAmount
        {
            get
            {
                float value = 0;
                m_rootVisual.Properties.TryGetScalar(BlurAmountProperty, out value);
                return value;
            }
            set
            {
                if (!m_setUpExpressions)
                {
                    m_blurBrush.Properties.InsertScalar("Blur.BlurAmount", (float)value);
                }
                m_rootVisual.Properties.InsertScalar(BlurAmountProperty, (float)value);
            }
        }

        //public Color TintColor
        //{
        //    get
        //    {
        //        m_rootVisual.Properties.TryGetColor("TintColor", out Color value);
        //        value = ((CompositionColorBrush)m_blurBrush).Color;
        //        return value;
        //    }
        //    set
        //    {
        //        if (!m_setUpExpressions)
        //        {
        //            m_blurBrush.Properties.InsertColor("Color.Color", value);
        //        }
        //        m_rootVisual.Properties.InsertColor("TintColor", value);
        //    }
        //}

        public Compositor Compositor
        {
            get
            {
                return m_compositor;
            }

            private set
            {
                m_compositor = value;
            }
        }

#pragma warning disable 1998
        private async void OnLoading(FrameworkElement sender, object args)
        {
            this.SizeChanged += OnSizeChanged;
            OnSizeChanged(this, null);

            m_noiseBrush.Surface = await SurfaceLoader.LoadFromUri(new Uri("ms-appx:///Assets/Noise.jpg"));
            m_noiseBrush.Stretch = CompositionStretch.UniformToFill;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.SizeChanged -= OnSizeChanged;
        }

        private void OnSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            if (m_blurVisual != null)
            {
                m_blurVisual.Size = new System.Numerics.Vector2((float)this.ActualWidth, (float)this.ActualHeight);
            }
        }

        private void SetUpPropertySetExpressions()
        {
            m_setUpExpressions = true;

            var exprAnimation = Compositor.CreateExpressionAnimation();
            exprAnimation.Expression = $"sourceProperties.{BlurAmountProperty}";
            exprAnimation.SetReferenceParameter("sourceProperties", m_rootVisual.Properties);

            m_blurBrush.Properties.StartAnimation("Blur.BlurAmount", exprAnimation);

            exprAnimation.Expression = $"sourceProperties.TintColor";

            m_blurBrush.Properties.StartAnimation("Color.Color", exprAnimation);
        }

        private CompositionEffectBrush BuildBlurBrush()
        {
            GaussianBlurEffect blurEffect = new GaussianBlurEffect()
            {
                Name = "Blur",
                BlurAmount = 0.0f,
                BorderMode = EffectBorderMode.Hard,
                Optimization = EffectOptimization.Balanced,
                Source = new CompositionEffectSourceParameter("source"),
            };

            BlendEffect blendEffect = new BlendEffect
            {
                Background = blurEffect,
                Foreground = new ColorSourceEffect { Name = "Color", Color = Color.FromArgb(64, 255, 255, 255) },
                Mode = BlendEffectMode.SoftLight
            };

            SaturationEffect saturationEffect = new SaturationEffect
            {
                Source = blendEffect,
                Saturation = 1.75f,
            };

            BlendEffect finalEffect = new BlendEffect
            {
                Foreground = new CompositionEffectSourceParameter("NoiseImage"),
                Background = saturationEffect,
                Mode = BlendEffectMode.Screen,
            };

            var factory = Compositor.CreateEffectFactory(
                finalEffect,
                new[] { "Blur.BlurAmount", "Color.Color" }
                );

            CompositionEffectBrush brush = factory.CreateBrush();
            brush.SetSourceParameter("NoiseImage", m_noiseBrush);
            return brush;
        }

        public CompositionPropertySet VisualProperties
        {
            get
            {
                if (!m_setUpExpressions)
                {
                    SetUpPropertySetExpressions();
                }
                return m_rootVisual.Properties;
            }
        }

        private static bool IsHostBackDropSupported => ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4);
    }

    public class BackDrop : BaseBackDrop
    {
        // Nothing here 
    }

    public class HostBackDrop : BaseBackDrop
    {
        /// <summary>
        /// Initializes new instance of the <see cref="HostBackDrop"/> class.
        /// </summary>
        public HostBackDrop() : base(true)
        {

        }
    }
}
