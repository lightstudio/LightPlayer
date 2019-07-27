using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Light.Controls
{
    /// <summary>
    /// General purpose toast control.
    /// </summary>
    public sealed partial class NotificationToast : UserControl
    {
        public static DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(NotificationToast), new PropertyMetadata(
                default(string)));

        public static DependencyProperty NotificationBodyProperty = DependencyProperty.Register(
            nameof(NotificationBody), typeof(string), typeof(NotificationToast), new PropertyMetadata(
                default(string)));

        private readonly Storyboard m_popupStoryBoard;
        private readonly ConcurrentQueue<ValueTuple<Guid, string, string>> m_toastQueue;
        private Guid m_currentToast;
        private const int BlockTime = 1000;

        /// <summary>
        /// Toast title.
        /// </summary>
        public string Title
        {
            get
            {
                return (string) GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

        /// <summary>
        /// Toast content.
        /// </summary>
        public string NotificationBody
        {
            get
            {
                return (string) GetValue(NotificationBodyProperty);
            }
            set
            {
                SetValue(NotificationBodyProperty, value);
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public NotificationToast()
        {
            InitializeComponent();
            m_popupStoryBoard = (Storyboard) Resources["PopupStoryBoard"];
            m_toastQueue = new ConcurrentQueue<(Guid, string, string)>();
            m_currentToast = Guid.Empty;
            m_popupStoryBoard.Completed += OnPopupStoryBoardCompleted;
        }

        /// <summary>
        /// Pop up notification.
        /// </summary>
        /// <param name="title">Notification title.</param>
        /// <param name="body">Notification body.</param>
        /// 
        public async void Popup(string title, string body)
        {
            // Filter requests
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(body)) return;

            var canSendNow = m_toastQueue.IsEmpty;

            // Enqueue notification
            m_toastQueue.Enqueue((Guid.NewGuid(), title, body));

            if (canSendNow)
            {
                // Check if we need dispatcher access
                if (!Dispatcher.HasThreadAccess)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => DequeueAndPresentToast());
                }
                else
                {
                    DequeueAndPresentToast();
                }
            }
        }

        /// <summary>
        /// Dequeue message and send to screen.
        /// </summary>
        private void DequeueAndPresentToast()
        {
            if (m_toastQueue.TryPeek(out (Guid, string, string) messagePreview))
            {
                m_currentToast = messagePreview.Item1;
                Title = messagePreview.Item2;
                NotificationBody = messagePreview.Item3;

                m_popupStoryBoard.AutoReverse = false;
                m_popupStoryBoard.Begin();
            }
        }

        /// <summary>
        /// Handles storyboard completion.
        /// </summary>
        /// <param name="sender">Storyboard itself.</param>
        /// <param name="e">Completion details.</param>
        private async void OnPopupStoryBoardCompleted(object sender, object e)
        {
            // Dequeue last toast.
            if(m_toastQueue.TryDequeue(out (Guid, string, string) message))
            {
#if DEBUG
                // Consistency check
                System.Diagnostics.Debug.Assert(m_currentToast == message.Item1);
#endif
            }

            // Wait for some time
            await Task.Delay(BlockTime);

            // Send next
            DequeueAndPresentToast();
        }
    }
}
