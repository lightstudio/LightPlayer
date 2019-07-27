using Light.Common;
using Light.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Phone.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RecentlyListenedView : MobileBasePage
    {
        public override bool ShowPlaybackControl => false;

        ObservableCollection<MusicPlaybackItem> HistoryList { get; set; }

        public RecentlyListenedView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HistoryList = new ObservableCollection<MusicPlaybackItem>(PlaybackHistoryManager.Instance.GetHistory(0));
            Bindings.Update();
            PlaybackHistoryManager.Instance.NewEntryAdded += OnNewHistoryEntryAdded;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            PlaybackHistoryManager.Instance.NewEntryAdded -= OnNewHistoryEntryAdded;
            base.OnNavigatedFrom(e);
        }

        private async void OnNewHistoryEntryAdded(object sender, MusicPlaybackItem e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                HistoryList.Insert(0, e);
            });
        }

        private async void OnClearHistoryClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog("This will permanently delete all playback history.", "Confirm deletion");
            dialog.Commands.Add(new UICommand(
                CommonSharedStrings.ConfirmString, new UICommandInvokedHandler(ClearHistory)));
            dialog.Commands.Add(new UICommand(CommonSharedStrings.CancelString));
            dialog.CancelCommandIndex = 1;
            dialog.DefaultCommandIndex = 0;
            await dialog.ShowAsync();
        }

        private async void OnHistoryItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var list = HistoryList.ToList();
            var item = (sender as FrameworkElement).DataContext as MusicPlaybackItem;
            var idx = list.IndexOf(item);
            if (idx == -1)
            {
                return;
            }

            await PlaybackControl.Instance.AddAndSetIndexAt(list, idx, true);
        }

        private async void ClearHistory(IUICommand command)
        {
            HistoryList.Clear();
            await PlaybackHistoryManager.Instance.ClearHistoryAsync();
        }
    }
}
