using Light.Lyrics.Analyzer;
using Light.Lyrics.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using WinRTXamlToolkit.Controls.Extensions;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Light.Lyrics.Controls
{
    public sealed class LyricsPresenter : Control
    {
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register("TextAlignment", typeof(TextAlignment), typeof(LyricsPresenter), new PropertyMetadata(TextAlignment.Left));

        public static readonly DependencyProperty AllowScrollProperty =
            DependencyProperty.Register(
                "AllowScroll", typeof(bool), typeof(LyricsPresenter), new PropertyMetadata(false));

        //public static readonly DependencyProperty HighlightTextBrushProperty =
        //    DependencyProperty.Register("HighlightTextBrush", typeof(Brush), typeof(LyricsPresenter), null);

        //LrcInfo m_lrcInfo;
        ParsedLrc m_parsedLrc;
        MediaElement m_mediaElement;

        ScrollViewer m_scrollViewer;
        StackPanel m_container;
        //Border m_top;
        //Border m_bottom;
        TextBlock[] m_textBlocks;
        TimeSpan m_offset;

        bool m_scrolling = true;
        int m_currentPosition = -1;
        double m_lastPos = 0;

        public bool AllowScroll
        {
            get { return (bool)GetValue(AllowScrollProperty); }
            set
            {
                SetValue(AllowScrollProperty, value);
                ScrollToCurrent();
            }
        }

        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        //public Brush HighlightTextBrush
        //{
        //    get { return (Brush)GetValue(HighlightTextBrushProperty); }
        //    set { SetValue(HighlightTextBrushProperty, value); }
        //}

        public ParsedLrc Lyrics
        {
            get { return m_parsedLrc; }
            set
            {
                m_currentPosition = -1;

                if (value == null || value.Sentences == null || value.Sentences.Count == 0)
                {
                    m_parsedLrc = null;
                }
                else m_parsedLrc = value;

                BuildLyricTextBlocks();

                PlaceTimelineMarkers();
                if (m_mediaElement != null)
                    OnCurrentStateChanged(m_mediaElement, null);
            }
        }
        public MediaElement Player
        {
            get { return m_mediaElement; }
            set
            {
                if (m_mediaElement != null)
                    InitializeMediaElement(unload: true);

                if ((m_mediaElement = value) == null) return;

                InitializeMediaElement(unload: false);
                PlaceTimelineMarkers();
            }
        }

        public LyricsPresenter()
        {
            this.DefaultStyleKey = typeof(LyricsPresenter);
            SizeChanged += OnSizeChanged;
        }

        async void ScrollToCurrent(double animDurationMillis = 0, bool force = false)
        {
            if (m_scrollViewer == null)
                return;

            double currPos = -150d;
            for (int i = 0; i < m_currentPosition; i++)
            {
                currPos += m_textBlocks[i].ActualHeight + 10.45d;
            }

            if (!force && Math.Abs(m_lastPos - currPos) < 5) return;

            m_lastPos = currPos;

            if (animDurationMillis < 50)
                m_scrollViewer.ChangeView(null, currPos, null);
            else
            {
                m_scrolling = true;
                await m_scrollViewer.ScrollToVerticalOffsetWithAnimation(currPos,
                    TimeSpan.FromMilliseconds(Math.Min(animDurationMillis, 500d)));
                m_scrolling = false;
            }
        }

        public void ForceRefresh()
        {
            ScrollToCurrent(0, true);
        }

        protected override void OnApplyTemplate()
        {
            m_scrollViewer = (ScrollViewer)GetTemplateChild("ScrollViewer");
            //m_top = (Border)GetTemplateChild("Top");
            //m_bottom = (Border)GetTemplateChild("Bottom");
            m_container = (StackPanel)GetTemplateChild("Container");

            base.OnApplyTemplate();
        }

        async void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var height = e.NewSize.Height / 2;
            //m_top.Height = height;
            //m_bottom.Height = height;

            if (!AllowScroll)
            {
                if (m_scrolling)
                    await Task.Delay(500);
                ScrollToCurrent();
            }
        }

        void BuildLyricTextBlocks()
        {
            m_container.Children.Clear();

            var count = (m_parsedLrc?.Sentences.Count).GetValueOrDefault();

            m_textBlocks = new TextBlock[count];
            for (int i = 0; i < count; i++)
            {
                m_textBlocks[i] = new TextBlock
                {
                    Text = m_parsedLrc.Sentences[i].Content,
                    TextAlignment = TextAlignment,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 5, 0, 5),
                    CharacterSpacing = 70
                };

                m_container.Children.Add(m_textBlocks[i]);
            }
        }

        void PlaceTimelineMarkers()
        {
            if (m_parsedLrc != null && m_mediaElement != null)
            {
                m_mediaElement.Markers.Clear();
                for (int i = 0; i < m_parsedLrc.Sentences.Count; i++)
                {
                    var marker = new TimelineMarker
                    {
                        Time = new TimeSpan(m_parsedLrc.Sentences[i].Time * TimeSpan.TicksPerMillisecond),
                        Text = i.ToString()
                    };
                    m_mediaElement.Markers.Insert(i, marker);
                }
            }
        }

        public void UpdateOffset(TimeSpan offset)
        {
            foreach (var marker in m_mediaElement.Markers)
            {
                marker.Time -= offset;
            }

            m_offset += offset;
            SyncLyrics(m_parsedLrc.GetPositionFromTime(
               (m_mediaElement.Position.Ticks + m_offset.Ticks) / TimeSpan.TicksPerMillisecond));
        }

        void InitializeMediaElement(bool unload)
        {
            m_mediaElement.Markers.Clear();

            if (unload)
            {
                m_mediaElement.MarkerReached -= OnMarkerReached;
                m_mediaElement.CurrentStateChanged -= OnCurrentStateChanged;
            }
            else
            {
                m_mediaElement.MarkerReached += OnMarkerReached;
                m_mediaElement.CurrentStateChanged += OnCurrentStateChanged;
            }
        }

        void SyncLyrics(int position, double animDurationMillis = 0)
        {
            if (m_parsedLrc != null)
            {
                if (m_textBlocks.Length == 0 ||
                    m_currentPosition == position) return;
                m_textBlocks[position].FontWeight = FontWeights.Bold;

                // Restore ForegroundProperty.
                if (m_currentPosition != -1)
                    m_textBlocks[m_currentPosition].ClearValue(TextBlock.FontWeightProperty);


                m_currentPosition = position;

                if (!AllowScroll)
                    ScrollToCurrent(animDurationMillis);
            }
        }

        async void OnCurrentStateChanged(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                if (m_parsedLrc != null)
                    SyncLyrics(m_parsedLrc.GetPositionFromTime(
                        (m_mediaElement.Position.Ticks + m_offset.Ticks) / TimeSpan.TicksPerMillisecond));
            });
        }

        async void OnMarkerReached(object sender, TimelineMarkerRoutedEventArgs args)
        {
            var index = Convert.ToInt32(args.Marker.Text, 10);
            if (m_parsedLrc == null || index >= m_parsedLrc.Sentences.Count ||

                m_mediaElement.Position.Ticks + TimeSpan.TicksPerSecond <
                (TimeSpan.TicksPerMillisecond * m_parsedLrc.Sentences[index].Time - m_offset.Ticks))
                return;

            // Divide by 8 => Right shift by 3
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                SyncLyrics(index,
                    index > 0 ?
                        (m_parsedLrc.Sentences[index].Time - m_parsedLrc.Sentences[index - 1].Time) >> 3 : 0)
            );
        }
    }
}
