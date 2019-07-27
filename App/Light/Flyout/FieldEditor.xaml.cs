using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Flyout
{
    public sealed partial class FieldEditor : ContentDialog
    {
        static public Task<string> ShowAsync(
            string oldName,
            string titleText,
            string patternDesc,
            string regexPattern)
        {
            TaskCompletionSource<string> source = new TaskCompletionSource<string>();
            var editor = new FieldEditor(source, oldName, titleText, patternDesc, regexPattern);
            var task = editor.ShowAsync();
            return source.Task;
        }

        private string _inputText;
        public string InputText
        {
            get
            {
                return _inputText;
            }
            set
            {
                if (_inputText == value)
                    return;
                _inputText = value;
                Bindings.Update();
            }
        }

        public bool InputMatched
        {
            get
            {
                return _reg.Match(_inputText).Success;
            }
        }

        private string _titleText;
        private string _patternDesc;

        private Regex _reg;

        private TaskCompletionSource<string> _source;
        FieldEditor(
            TaskCompletionSource<string> source,
            string oldName,
            string titleText,
            string patternDesc,
            string regexPattern)
        {
            InitializeComponent();
            _source = source;
            _titleText = titleText;
            _inputText = oldName;
            _patternDesc = patternDesc;
            _reg = new Regex(regexPattern);
        }

        private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            _source?.SetResult(null);
            _source = null;
        }

        private void OnPrimaryButtonClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            _source?.SetResult(_inputText);
            _source = null;
        }

        private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (InputMatched)
            {
                _source?.SetResult(_inputText);
                _source = null;
                Hide();
            }
        }
    }
}
