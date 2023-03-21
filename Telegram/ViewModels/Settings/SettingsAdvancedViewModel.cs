//
// Copyright Fela Ameghino 2015-2023
//
// Distributed under the GNU General Public License v3.0. (See accompanying
// file LICENSE or copy at https://www.gnu.org/licenses/gpl-3.0.txt)
//
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Common;
using Telegram.Converters;
using Telegram.Navigation.Services;
using Telegram.Services;
using Telegram.Services.Updates;
using Telegram.Td.Api;
using Telegram.Views.Popups;
using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace Telegram.ViewModels.Settings
{
    public class SettingsAdvancedViewModel : TLViewModelBase
        , IHandle
    //, IHandle<UpdateAppVersion>
    {
        private readonly ICloudUpdateService _cloudUpdateService;
        private CloudUpdate _update;

        private string _fileToken;

        public SettingsAdvancedViewModel(IClientService clientService, ISettingsService settingsService, IEventAggregator aggregator, ICloudUpdateService cloudUpdateService)
            : base(clientService, settingsService, aggregator)
        {
            _cloudUpdateService = cloudUpdateService;
        }

        protected override Task OnNavigatedToAsync(object parameter, NavigationMode mode, NavigationState state)
        {
            _update = _cloudUpdateService.NextUpdate;
            Update();

            return base.OnNavigatedToAsync(parameter, mode, state);
        }

        public override void Subscribe()
        {
            Aggregator.Subscribe<UpdateAppVersion>(this, Handle);
        }

        #region Updates

        public int UpdateFrequency
        {
            get => Array.IndexOf(_updateFrequencyIndexer, Settings.UpdateChannel);
            set
            {
                if (value >= 0 && value < _updateFrequencyIndexer.Length && Settings.UpdateChannel != _updateFrequencyIndexer[value])
                {
                    Settings.UpdateChannel = _updateFrequencyIndexer[value];
                    RaisePropertyChanged();
                    Update();
                }
            }
        }

        private readonly string[] _updateFrequencyIndexer = new[]
        {
            "#daily",
            "#update"
        };

        public List<SettingsOptionItem<string>> UpdateFrequencyOptions { get; } = new()
        {
            new SettingsOptionItem<string>("#daily", Strings.Resources.Daily),
            new SettingsOptionItem<string>("#update", Strings.Resources.Weekly),
        };

        private bool _isUpdateEnabled = true;
        public bool IsUpdateEnabled
        {
            get => _isUpdateEnabled;
            set => Set(ref _isUpdateEnabled, value);
        }

        private string _updateText;
        public string UpdateText
        {
            get => _updateText;
            set => Set(ref _updateText, value);
        }

        private string _updateFooter;
        public string UpdateFooter
        {
            get => _updateFooter;
            set => Set(ref _updateFooter, value);
        }

        public void Handle(UpdateAppVersion update)
        {
            BeginOnUIThread(() => UpdateFile(_update = update.Update, update.Update.Document, true));
        }

        private void UpdateFile(object target, File file)
        {
            UpdateFile(_update, file, false);
        }

        private void UpdateFile(CloudUpdate update, File file, bool download)
        {
            if (update == null)
            {
                IsUpdateEnabled = true;

                UpdateText = Strings.Resources.CheckForUpdates;
                UpdateFooter = Strings.Resources.CheckForUpdatesInfo;
            }
            else if (update.File != null)
            {
                // Update is ready to be installed
                IsUpdateEnabled = true;

                UpdateText = Strings.Resources.UpdateTelegram;
                UpdateFooter = Strings.Resources.UpdateTelegramInfo;
            }
            else if (file.Local.IsDownloadingActive)
            {
                // Update is being downloaded
                IsUpdateEnabled = false;

                UpdateText = string.Format("{0}... {1} / {2}", Strings.Resources.Downloading, FileSizeConverter.Convert(update.Document.Local.DownloadedSize, update.Document.Size), FileSizeConverter.Convert(update.Document.Size));
                UpdateFooter = Strings.Resources.UpdateTelegramInfo;
            }
            else if (file.Local.CanBeDownloaded)
            {
                ClientService.DownloadFile(update.Document.Id, 32);
            }

            if (download && file != null)
            {
                UpdateManager.Subscribe(this, ClientService, file, ref _fileToken, UpdateFile);
            }
        }

        public async void Update()
        {
            var update = _update;
            if (update == null)
            {
                IsUpdateEnabled = false;

                UpdateText = Strings.Resources.RetrievingInformation;

                var ticks = Environment.TickCount;

                await _cloudUpdateService.UpdateAsync(true);
                update = _update = _cloudUpdateService.NextUpdate;

                var diff = Environment.TickCount - ticks;
                if (diff < 2000)
                {
                    await Task.Delay(2000 - diff);
                }
            }
            else if (update.File != null)
            {
                await Launcher.LaunchFileAsync(update.File);
                Application.Current.Exit();
            }

            UpdateFile(update, update?.Document, true);
        }

        #endregion

        public bool IsAdaptiveWideEnabled
        {
            get => Settings.IsAdaptiveWideEnabled;
            set
            {
                Settings.IsAdaptiveWideEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool PreferIpv6
        {
            get => ClientService.Options.PreferIpv6;
            set
            {
                ClientService.Options.PreferIpv6 = value;
                RaisePropertyChanged();
            }
        }

        public bool IsTrayVisible
        {
            get => Settings.IsTrayVisible;
            set => SetTrayVisible(value);
        }

        private async void SetTrayVisible(bool value)
        {
            if (Settings.IsTrayVisible == value)
            {
                return;
            }

            Settings.IsTrayVisible = value;
            RaisePropertyChanged();

            if (value)
            {
                try
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                }
                catch
                {
                    // The app has been compiled without desktop bridge
                }
            }
            else if (App.Connection != null)
            {
                await App.Connection.SendMessageAsync(new Windows.Foundation.Collections.ValueSet { { "Exit", string.Empty } });
            }
        }
    }
}