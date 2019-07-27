using System;
using System.Linq;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Core;
using System.Collections;
using Light.Utilities;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Light.Managed.Database.Entities;

namespace Light.Controls
{
    public sealed partial class PlaylistControl : UserControl
    {
        private readonly SolidColorBrush listViewDragOverBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 247, 247, 247));
        // Data binding
        public static readonly DependencyProperty PlaylistProperty =
            DependencyProperty.Register(nameof(Playlist), typeof(object), typeof(PlaylistControl),
                new PropertyMetadata(default(object), PlaylistChanged));

        public static readonly DependencyProperty IsPlaylistPinnedProperty =
            DependencyProperty.Register(nameof(IsPlaylistPinned), typeof(bool), typeof(PlaylistControl),
                new PropertyMetadata(default(bool), OnIsPlaylistPinnedChanged));

        public static readonly DependencyProperty IsInNowPlayingViewProperty =
            DependencyProperty.Register(nameof(IsInNowPlayingView), typeof(bool), typeof(PlaylistControl), new PropertyMetadata(false));


        private static void OnIsPlaylistPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        public bool IsPlaylistPinned
        {
            get { return (bool)GetValue(IsPlaylistPinnedProperty); }
            set
            {
                SetValue(IsPlaylistPinnedProperty, value);
                
                // Pinning is implemented via data binding
                // Message is only used for prefs save
                Messenger.Default.Send(
                    value
                        ? new GenericMessage<SplitViewDisplayMode>(SplitViewDisplayMode.Inline)
                        : new GenericMessage<SplitViewDisplayMode>(SplitViewDisplayMode.Overlay),
                    CommonSharedStrings.InnerSplitViewModeChangeToken);
            }
        }

        public bool IsInNowPlayingView
        {
            get { return (bool)GetValue(IsInNowPlayingViewProperty); }
            set
            {
                SetValue(IsInNowPlayingViewProperty, value);
            }
        }

        private static void PlaylistChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public object Playlist
        {
            get
            {
                return GetValue(PlaylistProperty);
            }
            set
            {
                SetValue(PlaylistProperty, value);
                PlayItemsListView.ItemsSource = value;
            }
        }

        public PlaylistControl()
        {
            this.InitializeComponent();
        }

        #region Utils
        static ContainerVisual GetVisual(UIElement element)
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(element);
            ContainerVisual root = hostVisual.Compositor.CreateContainerVisual();
            ElementCompositionPreview.SetElementChildVisual(element, root);
            return root;
        }
        #endregion

        internal object[] GetSelectedItemsCopy() =>
            PlayItemsListView.SelectedItems.ToArray();

        internal void ExitEditMode()
        {
            EditToggleButton.IsChecked = false;
            PlayItemsListView.SelectionMode = ListViewSelectionMode.None;
        }

        private bool m_ControlPressed = false;

        #region Event Handlers
        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            m_ControlPressed = (e.Key == VirtualKey.Control);

            if ((EditToggleButton.IsChecked ?? false))
            {
                if (e.Key == VirtualKey.Delete)
                {
                    ((ICommand)this.Resources["DeleteButtonCommand"]).Execute(null);
                    e.Handled = true;
                }

                if (e.Key == VirtualKey.Escape)
                {
                    ExitEditMode();
                    e.Handled = true;
                }

                if (m_ControlPressed && e.Key == VirtualKey.A)
                {
                    PlayItemsListView.SelectAll();
                    e.Handled = true;
                }
            }
        }

        private void RemoveBatch()
        {

        }

        private void OnEditToggleButtonClicked(object sender, RoutedEventArgs e) =>
            PlayItemsListView.SelectionMode =
                (EditToggleButton.IsChecked ?? false) ?
                    ListViewSelectionMode.Multiple :
                    ListViewSelectionMode.None;

        #endregion

        private async void OnMediaPlaybackItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch &&
                PlayItemsListView.SelectionMode == ListViewSelectionMode.None)
            {
                await Core.PlaybackControl.Instance.PlayAt((Playlist as IList).IndexOf((sender as Grid).DataContext as MusicPlaybackItem));
            }
        }

        private async void OnMediaPlaybackItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch &&
                PlayItemsListView.SelectionMode == ListViewSelectionMode.None)
            {
                await Core.PlaybackControl.Instance.PlayAt((Playlist as IList).IndexOf((sender as Grid).DataContext as MusicPlaybackItem));
            }
        }

        private async void OnPlayItemsListViewDrop(object sender, DragEventArgs e)
        {
            IEnumerable<MusicPlaybackItem> addItems = null;
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                addItems = await FileOpen.GetPlaybackItemsFromFilesAsync(
                    await FileOpen.GetAllFiles(items));
            }
            else if (e.DataView.Contains(StandardDataFormats.Text))
            {
                var items = DragHelper.Get(await e.DataView.GetTextAsync());

                if (items is Playlist)
                {
                    var list = items as Playlist;
                    addItems =
                        from i
                        in list.Items
                        select MusicPlaybackItem.CreateFromMediaFile(i.ToMediaFile());
                }
                else if (items is PlaylistItem)
                {
                    addItems = new MusicPlaybackItem[]
                    {
                        MusicPlaybackItem.CreateFromMediaFile(
                            (items as PlaylistItem).ToMediaFile())
                    };
                }
                else if (items is IEnumerable<DbMediaFile>)
                {
                    addItems =
                        from i
                        in items as IEnumerable<DbMediaFile>
                        select MusicPlaybackItem.CreateFromMediaFile(i);
                }
                else if (items is DbMediaFile)
                {
                    addItems = new MusicPlaybackItem[]
                    {
                        MusicPlaybackItem.CreateFromMediaFile(items as DbMediaFile)
                    };
                }

            }
            if (addItems != null)
            {
                var targetListView = sender as ListView;

                if (targetListView == null)
                {
                    return;
                }
                targetListView.Background = null;
                Border border = VisualTreeHelper.GetChild(targetListView, 0) as Border;

                ScrollViewer scrollViewer = border.Child as ScrollViewer;
                var droppedPosition = e.GetPosition(targetListView).Y + scrollViewer.VerticalOffset;
                var itemsSource = targetListView.ItemsSource as IList;
                var highWaterMark = 3d;  // 3px of padding
                var dropIndex = 0;
                var foundDropLocation = false;

                for (int i = 0; i < itemsSource.Count && !foundDropLocation; i++)
                {
                    var itemContainer = (ListViewItem)targetListView.ContainerFromIndex(i);

                    if (itemContainer != null)
                        highWaterMark = highWaterMark + itemContainer.ActualHeight;

                    if (droppedPosition <= highWaterMark)
                    {
                        dropIndex = i;
                        foundDropLocation = true;
                    }
                }

                if (foundDropLocation)
                {
                    await Core.PlaybackControl.Instance.AddFile(addItems, dropIndex);
                }
                else
                {
                    await Core.PlaybackControl.Instance.AddFile(addItems);
                }
            }
        }

        public void ScrollToNowPlaying()
        {
            var idx = Core.PlaybackControl.Instance.Items.IndexOf(
                Core.PlaybackControl.Instance.Current);
            if (idx != -1)
            {
                PlayItemsListView.ScrollIntoView(PlayItemsListView.Items[idx], ScrollIntoViewAlignment.Leading);
            }
        }

        private async void OnPlayItemsListViewDragEnter(object sender, DragEventArgs e)
        {
            var def = e.GetDeferral();
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
            else if (e.DataView.Contains(StandardDataFormats.Text))
            {
                if (DragHelper.Contains(await e.DataView.GetTextAsync()))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
            def.Complete();
        }

        private void PlaylistControlEntity_Loaded(object sender, RoutedEventArgs e)
        {
            BackDrop.TintColor = (Color)Application.Current.Resources["BackDropColor2"];
        }
    }

    public class DeleteButtonCommand : ICommand
    {
        public PlaylistControl Parent { get; set; }

        public bool CanExecute(object parameter) =>
            parameter is MusicPlaybackItem || parameter == null;

        public async void Execute(object parameter)
        {
            if (Parent == null) return;
            if (parameter is MusicPlaybackItem)
            {
                await Core.PlaybackControl.Instance.RemoveAsync((MusicPlaybackItem)parameter);
            }
            if (parameter == null)
            {
                var listCopy = Parent.GetSelectedItemsCopy();
                foreach (var entity in listCopy)
                {
                    await Core.PlaybackControl.Instance.RemoveAsync((MusicPlaybackItem)entity);
                }
                Parent.ExitEditMode();
            }
        }

#pragma warning disable CS0067 // Reserved for XAML Framework
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }

    public class PlayButtonCommand : ICommand
    {
        public bool CanExecute(object parameter) =>
            parameter is MusicPlaybackItem;

        public async void Execute(object parameter)
        {
            if (parameter is MusicPlaybackItem)
                await Core.PlaybackControl.Instance.PlayAt((MusicPlaybackItem)parameter);
        }

#pragma warning disable CS0067 // Reserved for XAML Framework
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }

    public class SavePlaylistCommand : ICommand
    {
        public bool CanExecute(object parameter) =>
            !string.IsNullOrEmpty(parameter as string);

        public async void Execute(object parameter)
        {
            if (!(parameter is string) ||
                string.IsNullOrWhiteSpace(parameter as string))
                return;
            var playlistTitle = (string)parameter;
            Exception exc = null;
            try
            {
                await PlaylistManager.Instance.SaveNowPlayingList(playlistTitle);
            }
            catch (Exception ex)
            {
                exc = ex;
            }

            ContentDialog dialog = new ContentDialog
            {
                FullSizeDesired = false,
                PrimaryButtonText = "OK",
                IsPrimaryButtonEnabled = true,
                MaxWidth = 300,
                Style = (Style)Application.Current.Resources["LightContentDialogStyle"]
            };
            if (exc != null)
            {
                dialog.Title = CommonSharedStrings.PlaylistSaveErrorTitle;
                var exceptionPanel = new StackPanel();
                exceptionPanel.Children.Add(
                    new TextBlock
                    {
                        Text = string.Format(CommonSharedStrings.UnknownErrorPromptContent,
                            exc.GetType().Name,
                            exc.Message,
                            exc.StackTraceEx()),
                        TextWrapping = TextWrapping.Wrap
                    });
                var innerException = exc.InnerException;
                while (innerException != null)
                {
                    exceptionPanel.Children.Add(
                        new TextBlock
                        {
                            Text = string.Format(CommonSharedStrings.InnerExceptionPromptContent,
                            innerException.GetType().Name,
                            innerException.Message,
                            innerException.StackTraceEx()),
                            TextWrapping = TextWrapping.Wrap
                        });
                    innerException = innerException.InnerException;
                }
                var scrollViewer = new ScrollViewer();
                scrollViewer.Content = exceptionPanel;
                dialog.Content = scrollViewer;
            }
            else
            {
                dialog.Title = CommonSharedStrings.PlaylisySaveSucceededTitle;
            }
            await dialog.ShowAsync();
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }
}
