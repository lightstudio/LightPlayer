using System;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Light.Phone.View
{
    public static class Extensions
    {
        public static ScrollViewer GetScrollViewer(this DependencyObject element)
        {
            if (element is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);

                var result = GetScrollViewer(child);
                if (result == null) continue;

                return result;
            }

            return null;
        }

        public static bool AlmostEqual(this float x, float y, float tolerance = 0.01f) =>
            Math.Abs(x - y) < tolerance;
    }

    partial class MobileHomeView
    {
        #region Now Playing List
        ScrollViewer _nowPlayingScrollViewer;
        Compositor _nowPlayingCompositor;

        CompositionPropertySet _nowPlayingManipulation;
        ExpressionAnimation _nowPlayingOpacityAnimation, _nowPlayingRefreshBorderOffsetAnimation, _nowPlayingBorderOffsetAnimation;
        ScalarKeyFrameAnimation _nowPlayingResetAnimation;

        Visual _nowPlayingBorderVisual, _nowPlayingReleaseBorderVisual;

        private void OnNowPlayingListLoaded(object sender, RoutedEventArgs e)
        {
            LoadNowPlayingListAnimation();
        }

        private void OnNowPlayingListUnloaded(object sender, RoutedEventArgs e)
        {
            _nowPlayingScrollViewer.DirectManipulationStarted -= OnNowPlayingListManipulationStarted;
            _nowPlayingScrollViewer.DirectManipulationCompleted -= OnNowPlayingListManipulationCompleted;
        }

        private void LoadNowPlayingListAnimation()
        {
            _nowPlayingScrollViewer = NowPlayingList.GetScrollViewer();
            _nowPlayingScrollViewer.DirectManipulationStarted += OnNowPlayingListManipulationStarted;
            _nowPlayingScrollViewer.DirectManipulationCompleted += OnNowPlayingListManipulationCompleted;

            _nowPlayingManipulation = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(_nowPlayingScrollViewer);
            _nowPlayingCompositor = _nowPlayingManipulation.Compositor;

            _nowPlayingOpacityAnimation = _nowPlayingCompositor.CreateExpressionAnimation("min(max(0, -ScrollManipulation.Translation.X*4) / Divider, 1)");
            _nowPlayingOpacityAnimation.SetScalarParameter("Divider", 95.0f);
            _nowPlayingOpacityAnimation.SetReferenceParameter("ScrollManipulation", _nowPlayingManipulation);

            _nowPlayingRefreshBorderOffsetAnimation = _nowPlayingCompositor.CreateExpressionAnimation(" ControlWidth+(max(min(0, ScrollManipulation.Translation.X*4) / Divider, -1)) * MaxOffsetX");
            _nowPlayingRefreshBorderOffsetAnimation.SetScalarParameter("Divider", 95.0f);
            _nowPlayingRefreshBorderOffsetAnimation.SetScalarParameter("MaxOffsetX", 95.0f);
            _nowPlayingRefreshBorderOffsetAnimation.SetScalarParameter("ControlWidth", (float)(ActualWidth));
            _nowPlayingRefreshBorderOffsetAnimation.SetReferenceParameter("ScrollManipulation", _nowPlayingManipulation);

            _nowPlayingBorderOffsetAnimation = _nowPlayingCompositor.CreateExpressionAnimation("(max(min(0, ScrollManipulation.Translation.X) / Divider, -1)) * MaxOffsetX");
            _nowPlayingBorderOffsetAnimation.SetScalarParameter("Divider", 95.0f);
            _nowPlayingBorderOffsetAnimation.SetScalarParameter("MaxOffsetX", 95.0f);
            _nowPlayingBorderOffsetAnimation.SetReferenceParameter("ScrollManipulation", _nowPlayingManipulation);

            _nowPlayingResetAnimation = _nowPlayingCompositor.CreateScalarKeyFrameAnimation();
            _nowPlayingResetAnimation.InsertKeyFrame(1.0f, 0.0f);

            _nowPlayingReleaseBorderVisual = ElementCompositionPreview.GetElementVisual(NowPlayingReleaseBorder);

            var border = (Border)VisualTreeHelper.GetChild(NowPlayingList, 0);
            _nowPlayingBorderVisual = ElementCompositionPreview.GetElementVisual(border);

            PrepareNowPlayingListExpressionAnimationsOnScroll();
        }

        private void PrepareNowPlayingListExpressionAnimationsOnScroll()
        {
            _nowPlayingReleaseBorderVisual.StartAnimation("Opacity", _nowPlayingOpacityAnimation);
            _nowPlayingReleaseBorderVisual.StartAnimation("Offset.X", _nowPlayingRefreshBorderOffsetAnimation);
            _nowPlayingBorderVisual.StartAnimation("Offset.X", _nowPlayingBorderOffsetAnimation);
        }

        private void OnNowPlayingListManipulationStarted(object sender, object e)
        {
            Windows.UI.Xaml.Media.CompositionTarget.Rendering += OnNowPlayingCompositionRendering;

            _nowPlayingPulledToMax = false;
            _nowPlayingEnterDetail = false;
        }

        float _nowPlayingReleaseBorderOffsetX;
        bool _nowPlayingPulledToMax = false;
        bool _nowPlayingEnterDetail = false;
        DateTime _nowPlayingFirstMaxTime;
        DateTime _nowPlayingPulledDownTime;
        DateTime _nowPlayingRestoreTime;

        private void OnNowPlayingCompositionRendering(object sender, object e)
        {
            _nowPlayingReleaseBorderVisual.StopAnimation("Offset.X");
            _nowPlayingReleaseBorderOffsetX = (float)ActualWidth - _nowPlayingReleaseBorderVisual.Offset.X;

            if (!_nowPlayingPulledToMax)
            {
                _nowPlayingPulledToMax = _nowPlayingReleaseBorderOffsetX >= 94.9f;
                if (_nowPlayingPulledToMax)
                {
                    _nowPlayingFirstMaxTime = DateTime.Now;
                }
            }
            else
            {
                _nowPlayingPulledToMax = _nowPlayingReleaseBorderOffsetX >= 94.9f;
                if (_nowPlayingPulledToMax)
                {
                    if (DateTime.Now - _nowPlayingFirstMaxTime > TimeSpan.FromMilliseconds(100))
                    {
                        if (!_nowPlayingEnterDetail)
                        {
                            var sb = (Storyboard)NowPlayingPullIcon.Resources["NowPlayingPullIconRotate"];
                            sb.Begin();
                            _nowPlayingEnterDetail = true;
                        }
                        _nowPlayingPulledDownTime = DateTime.Now;
                    }
                }
            }

            if (_nowPlayingEnterDetail && _nowPlayingReleaseBorderOffsetX < 10.0f)
            {
                _nowPlayingRestoreTime = DateTime.Now;
            }

            _nowPlayingReleaseBorderVisual.StartAnimation("Offset.X", _nowPlayingRefreshBorderOffsetAnimation);
        }

        private void OnNowPlayingListManipulationCompleted(object sender, object o)
        {
            Windows.UI.Xaml.Media.CompositionTarget.Rendering -= OnNowPlayingCompositionRendering;
            NowPlayingPullIconTransform.Angle = 0;

            var cancelled = _nowPlayingRestoreTime - _nowPlayingPulledDownTime > TimeSpan.FromMilliseconds(250);

            if (_nowPlayingEnterDetail && !cancelled)
            {
                Frame.Navigate(typeof(MobileNowPlayingListView));
            }

            var batch = _nowPlayingCompositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            batch.Completed += (s, e) => PrepareNowPlayingListExpressionAnimationsOnScroll();
            _nowPlayingBorderVisual.StartAnimation("Offset.X", _nowPlayingResetAnimation);
            _nowPlayingReleaseBorderVisual.StartAnimation("Opacity", _nowPlayingResetAnimation);
            batch.End();
        }
        #endregion

        #region Recently
        ScrollViewer _recentlyScrollViewer;
        Compositor _recentlyCompositor;

        CompositionPropertySet _recentlyManipulation;
        ExpressionAnimation _recentlyOpacityAnimation, _recentlyRefreshBorderOffsetAnimation, _recentlyBorderOffsetAnimation;
        ScalarKeyFrameAnimation _recentlyResetAnimation;

        Visual _recentlyBorderVisual, _recentlyReleaseBorderVisual;

        private void OnRecentlyListLoaded(object sender, RoutedEventArgs e)
        {
            LoadRecentlyListAnimation();
        }

        private void OnRecentlyListUnloaded(object sender, RoutedEventArgs e)
        {
            _recentlyScrollViewer.DirectManipulationStarted -= OnRecentlyListManipulationStarted;
            _recentlyScrollViewer.DirectManipulationCompleted -= OnRecentlyListManipulationCompleted;
        }

        private void LoadRecentlyListAnimation()
        {
            _recentlyScrollViewer = RecentlyList.GetScrollViewer();
            _recentlyScrollViewer.DirectManipulationStarted += OnRecentlyListManipulationStarted;
            _recentlyScrollViewer.DirectManipulationCompleted += OnRecentlyListManipulationCompleted;

            _recentlyManipulation = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(_recentlyScrollViewer);
            _recentlyCompositor = _recentlyManipulation.Compositor;

            _recentlyOpacityAnimation = _recentlyCompositor.CreateExpressionAnimation("min(max(0, -ScrollManipulation.Translation.X*4) / Divider, 1)");
            _recentlyOpacityAnimation.SetScalarParameter("Divider", 95.0f);
            _recentlyOpacityAnimation.SetReferenceParameter("ScrollManipulation", _recentlyManipulation);

            _recentlyRefreshBorderOffsetAnimation = _recentlyCompositor.CreateExpressionAnimation(" ControlWidth+(max(min(0, ScrollManipulation.Translation.X*4) / Divider, -1)) * MaxOffsetX");
            _recentlyRefreshBorderOffsetAnimation.SetScalarParameter("Divider", 95.0f);
            _recentlyRefreshBorderOffsetAnimation.SetScalarParameter("MaxOffsetX", 95.0f);
            _recentlyRefreshBorderOffsetAnimation.SetScalarParameter("ControlWidth", (float)(ActualWidth));
            _recentlyRefreshBorderOffsetAnimation.SetReferenceParameter("ScrollManipulation", _recentlyManipulation);

            _recentlyBorderOffsetAnimation = _recentlyCompositor.CreateExpressionAnimation("(max(min(0, ScrollManipulation.Translation.X) / Divider, -1)) * MaxOffsetX");
            _recentlyBorderOffsetAnimation.SetScalarParameter("Divider", 95.0f);
            _recentlyBorderOffsetAnimation.SetScalarParameter("MaxOffsetX", 95.0f);
            _recentlyBorderOffsetAnimation.SetReferenceParameter("ScrollManipulation", _recentlyManipulation);

            _recentlyResetAnimation = _recentlyCompositor.CreateScalarKeyFrameAnimation();
            _recentlyResetAnimation.InsertKeyFrame(1.0f, 0.0f);

            _recentlyReleaseBorderVisual = ElementCompositionPreview.GetElementVisual(RecentlyReleaseBorder);

            var border = (Border)VisualTreeHelper.GetChild(RecentlyList, 0);
            _recentlyBorderVisual = ElementCompositionPreview.GetElementVisual(border);

            PrepareRecentlyListExpressionAnimationsOnScroll();
        }

        private void PrepareRecentlyListExpressionAnimationsOnScroll()
        {
            _recentlyReleaseBorderVisual.StartAnimation("Opacity", _recentlyOpacityAnimation);
            _recentlyReleaseBorderVisual.StartAnimation("Offset.X", _recentlyRefreshBorderOffsetAnimation);
            _recentlyBorderVisual.StartAnimation("Offset.X", _recentlyBorderOffsetAnimation);
        }

        private void OnRecentlyListManipulationStarted(object sender, object e)
        {
            Windows.UI.Xaml.Media.CompositionTarget.Rendering += OnRecentlyCompositionRendering;

            _recentlyPulledToMax = false;
            _recentlyEnterDetail = false;
        }

        float _recentlyReleaseBorderOffsetX;
        bool _recentlyPulledToMax = false;
        bool _recentlyEnterDetail = false;
        DateTime _recentlyFirstMaxTime;
        DateTime _recentlyPulledDownTime;
        DateTime _recentlyRestoreTime;

        private void OnRecentlyCompositionRendering(object sender, object e)
        {
            _recentlyReleaseBorderVisual.StopAnimation("Offset.X");
            _recentlyReleaseBorderOffsetX = (float)ActualWidth - _recentlyReleaseBorderVisual.Offset.X;

            if (!_recentlyPulledToMax)
            {
                _recentlyPulledToMax = _recentlyReleaseBorderOffsetX >= 94.9f;
                if (_recentlyPulledToMax)
                {
                    _recentlyFirstMaxTime = DateTime.Now;
                }
            }
            else
            {
                _recentlyPulledToMax = _recentlyReleaseBorderOffsetX >= 94.9f;
                if (_recentlyPulledToMax)
                {
                    if (DateTime.Now - _recentlyFirstMaxTime > TimeSpan.FromMilliseconds(100))
                    {
                        if (!_recentlyEnterDetail)
                        {
                            var sb = (Storyboard)RecentlyPullIcon.Resources["RecentlyPullIconRotate"];
                            sb.Begin();
                            _recentlyEnterDetail = true;
                        }
                        _recentlyPulledDownTime = DateTime.Now;
                    }
                }
            }

            if (_recentlyEnterDetail && _recentlyReleaseBorderOffsetX < 10.0f)
            {
                _recentlyRestoreTime = DateTime.Now;
            }

            _recentlyReleaseBorderVisual.StartAnimation("Offset.X", _recentlyRefreshBorderOffsetAnimation);
        }

        private void OnRecentlyListManipulationCompleted(object sender, object o)
        {
            Windows.UI.Xaml.Media.CompositionTarget.Rendering -= OnRecentlyCompositionRendering;
            RecentlyPullIconTransform.Angle = 0;

            var cancelled = _recentlyRestoreTime - _recentlyPulledDownTime > TimeSpan.FromMilliseconds(250);

            if (_recentlyEnterDetail && !cancelled)
            {
                Frame.Navigate(typeof(RecentlyListenedView));
            }

            var batch = _recentlyCompositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            batch.Completed += (s, e) => PrepareRecentlyListExpressionAnimationsOnScroll();
            _recentlyBorderVisual.StartAnimation("Offset.X", _recentlyResetAnimation);
            _recentlyReleaseBorderVisual.StartAnimation("Opacity", _recentlyResetAnimation);
            batch.End();
        }
        #endregion
    }
}
