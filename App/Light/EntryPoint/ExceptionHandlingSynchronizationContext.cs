using System;
using System.Threading;
using Windows.UI.Xaml.Controls;

namespace Light
{
    /// <summary>
    /// Provides the basic functionality for propagating a synchronization context in exception synchronization models.
    /// </summary>
    public class ExceptionHandlingSynchronizationContext : SynchronizationContext
    {

        private readonly SynchronizationContext _syncContext;

        /// <summary>
        /// Register context to application domain.
        /// </summary>
        /// <returns>Instance of <see cref="ExceptionHandlingSynchronizationContext"/>.</returns>
        /// <remarks>
        /// Call this from OnLaunched and OnActivated inside the App.xaml.cs
        /// </remarks>
        public static ExceptionHandlingSynchronizationContext Register()
        {
            var syncContext = Current;
            if (syncContext == null) throw new InvalidOperationException("Ensure a synchronization context exists before calling this method.");


            var customSynchronizationContext = syncContext as ExceptionHandlingSynchronizationContext;
            if (customSynchronizationContext == null)
            {
                customSynchronizationContext = new ExceptionHandlingSynchronizationContext(syncContext);
                SetSynchronizationContext(customSynchronizationContext);
            }
            return customSynchronizationContext;
        }

        /// <summary>
        /// Links the synchronization context to the specified frame
        /// and ensures that it is still in use after each navigation event
        /// </summary>
        /// <param name="rootFrame">Instance of <see cref="Frame"/> that hosts application content.</param>
        /// <returns>Instance of <see cref="ExceptionHandlingSynchronizationContext"/>.</returns>
        public static ExceptionHandlingSynchronizationContext RegisterForFrame(Frame rootFrame)
        {
            if (rootFrame == null)
                throw new ArgumentNullException(nameof(rootFrame));

            var synchronizationContext = Register();

            rootFrame.Navigating += (sender, args) => EnsureContext(synchronizationContext);
            rootFrame.Loaded += (sender, args) => EnsureContext(synchronizationContext);

            return synchronizationContext;
        }

        /// <summary>
        /// Ensure current synchronization context points to the given context.
        /// </summary>
        /// <param name="context">Instance of <see cref="SynchronizationContext"/>.</param>
        private static void EnsureContext(SynchronizationContext context)
        {
            if (Current != context) SetSynchronizationContext(context);
        }

        /// <summary>
        /// Initializes new instance of <see cref="ExceptionHandlingSynchronizationContext"/>.
        /// </summary>
        /// <param name="syncContext">Instance of <see cref="SynchronizationContext"/>.</param>
        public ExceptionHandlingSynchronizationContext(SynchronizationContext syncContext)
        {
            _syncContext = syncContext;
        }

        /// <inheritdoc />
        public override SynchronizationContext CreateCopy()
        {
            return new ExceptionHandlingSynchronizationContext(_syncContext.CreateCopy());
        }

        /// <inheritdoc />
        public override void OperationCompleted()
        {
            _syncContext.OperationCompleted();
        }

        /// <inheritdoc />
        public override void OperationStarted()
        {
            _syncContext.OperationStarted();
        }

        /// <inheritdoc />
        public override void Post(SendOrPostCallback d, object state)
        {
            _syncContext.Post(WrapCallback(d), state);
        }

        /// <inheritdoc />
        public override void Send(SendOrPostCallback d, object state)
        {
            _syncContext.Send(d, state);
        }

        /// <summary>
        /// Wrap the given instance of <see cref="SendOrPostCallback"/>.
        /// </summary>
        /// <param name="sendOrPostCallback">Instance of <see cref="SendOrPostCallback"/>.</param>
        /// <returns>Instance of <see cref="SendOrPostCallback"/>.</returns>
        private SendOrPostCallback WrapCallback(SendOrPostCallback sendOrPostCallback)
        {
            return state =>
            {
                try
                {
                    sendOrPostCallback(state);
                }
                catch (Exception ex)
                {
                    if (!HandleException(ex)) throw;
                }
            };
        }

        /// <summary>
        /// Handles unhandled asynchronous exceptions.
        /// </summary>
        /// <param name="exception">Instance of <see cref="Exception"/>.</param>
        /// <returns>Whether exception has been handled or not.</returns>
        private bool HandleException(Exception exception)
        {
            if (UnhandledException == null)
                return false;

            var exWrapper = new UnhandledExceptionEventArgs
            {
                Exception = exception
            };

            UnhandledException(this, exWrapper);

#if DEBUG && !DISABLE_XAML_GENERATED_BREAK_ON_UNHANDLED_EXCEPTION
            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
#endif

            return exWrapper.Handled;
        }

        /// <summary>
        /// Listen to this event to catch any unhandled exceptions and allow for handling them
        /// so they don't crash your application
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> UnhandledException;
    }

    /// <summary>
    /// Represents unhandled exception event data.
    /// </summary>
    public class UnhandledExceptionEventArgs : EventArgs
    {

        /// <summary>
        /// Whether exception has been handled or not.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Actual instance of <see cref="Exception"/> that contains exception details.
        /// </summary>
        public Exception Exception { get; set; }

    }
}
