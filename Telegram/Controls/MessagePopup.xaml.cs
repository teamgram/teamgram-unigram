//
// Copyright Fela Ameghino 2015-2023
//
// Distributed under the GNU General Public License v3.0. (See accompanying
// file LICENSE or copy at https://www.gnu.org/licenses/gpl-3.0.txt)
//
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Telegram.Common;
using Telegram.Navigation;
using Telegram.Td.Api;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Telegram.Controls
{
    public sealed partial class MessagePopup : ContentPopup
    {
        public MessagePopup()
        {
            InitializeComponent();
        }

        public MessagePopup(string message)
            : this(message, null)
        {

        }

        public MessagePopup(string message, string title)
        {
            InitializeComponent();

            Message = message;
            Title = title;
            PrimaryButtonText = "OK";
        }

        public string Message
        {
            get => TextBlockHelper.GetMarkdown(MessageLabel);
            set => TextBlockHelper.SetMarkdown(MessageLabel, value);
        }

        public FormattedText FormattedMessage
        {
            get => TextBlockHelper.GetFormattedText(MessageLabel);
            set => TextBlockHelper.SetFormattedText(MessageLabel, value);
        }

        public string CheckBoxLabel
        {
            get => CheckBox.Content.ToString();
            set
            {
                CheckBox.Content = value;
                CheckBox.Visibility = string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public bool? IsChecked
        {
            get => CheckBox.IsChecked;
            set => CheckBox.IsChecked = value;
        }

        public static Task<ContentDialogResult> ShowAsync(string message, string title = null, string primary = null, string secondary = null, bool dangerous = false, ElementTheme requestedTheme = ElementTheme.Default)
        {
            var popup = new MessagePopup
            {
                Title = title ?? Strings.AppName,
                Message = message,
                PrimaryButtonText = primary ?? Strings.OK,
                SecondaryButtonText = secondary ?? string.Empty,
                RequestedTheme = requestedTheme
            };

            if (dangerous)
            {
                popup.DefaultButton = ContentDialogButton.None;
                popup.PrimaryButtonStyle = BootStrapper.Current.Resources["DangerButtonStyle"] as Style;
            }

            return popup.ShowQueuedAsync();
        }

        public static Task<ContentDialogResult> ShowAsync(FormattedText message, string title = null, string primary = null, string secondary = null, bool dangerous = false, ElementTheme requestedTheme = ElementTheme.Default)
        {
            var popup = new MessagePopup
            {
                Title = title ?? Strings.AppName,
                FormattedMessage = message,
                PrimaryButtonText = primary ?? Strings.OK,
                SecondaryButtonText = secondary ?? string.Empty,
                RequestedTheme = requestedTheme
            };

            if (dangerous)
            {
                popup.DefaultButton = ContentDialogButton.None;
                popup.PrimaryButtonStyle = BootStrapper.Current.Resources["DangerButtonStyle"] as Style;
            }

            return popup.ShowQueuedAsync();
        }

        public static Task<ContentDialogResult> ShowAsync(FrameworkElement target, string message, string title = null, string primary = null, string secondary = null, bool dangerous = false)
        {
            var tsc = new TaskCompletionSource<ContentDialogResult>();
            var popup = new TeachingTip
            {
                Title = title,
                Subtitle = message,
                ActionButtonContent = primary,
                ActionButtonStyle = BootStrapper.Current.Resources[dangerous ? "DangerButtonStyle" : "AccentButtonStyle"] as Style,
                CloseButtonContent = secondary,
                PreferredPlacement = TeachingTipPlacementMode.Top,
                Width = 314,
                MinWidth = 314,
                MaxWidth = 314,
                Target = target,
                IsLightDismissEnabled = true,
                ShouldConstrainToRootBounds = true,
                // TODO:
                RequestedTheme = target.ActualTheme
            };

            popup.ActionButtonClick += (s, args) =>
            {
                popup.IsOpen = false;
                tsc.TrySetResult(ContentDialogResult.Primary);
            };

            popup.Closed += (s, args) =>
            {
                tsc.TrySetResult(ContentDialogResult.Secondary);
            };

            if (Window.Current.Content is FrameworkElement element)
            {
                element.Resources["TeachingTip"] = popup;
            }

            popup.IsOpen = true;
            return tsc.Task;
        }
    }
}
