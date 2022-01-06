﻿using System.ComponentModel;
using Unigram.Navigation;
using Unigram.ViewModels;
using Unigram.ViewModels.Delegates;
using Windows.UI.Xaml.Navigation;

namespace Unigram.Views
{
    public sealed partial class ChatScheduledPage : HostedPage, INavigablePage, ISearchablePage, IActivablePage
    {
        public DialogScheduledViewModel ViewModel => DataContext as DialogScheduledViewModel;
        public ChatView View => Content as ChatView;

        public ChatScheduledPage()
        {
            InitializeComponent();

            Content = new ChatView(CreateViewModel);
            Header = View.Header;
            NavigationCacheMode = NavigationCacheMode.Required;
        }

        private DialogViewModel CreateViewModel(IDialogDelegate delegato, int sessionId)
        {
            var viewModel = TLContainer.Current.Resolve<DialogScheduledViewModel, IDialogDelegate>(delegato, sessionId);
            DataContext = viewModel;

            return viewModel;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            View.OnNavigatingFrom(e.SourcePageType);
        }

        public void OnBackRequested(HandledEventArgs args)
        {
            View.OnBackRequested(args);
        }

        public void Search()
        {
            View.Search();
        }

        public void Dispose()
        {
            View.Dispose();
        }

        public void Activate(int sessionId)
        {
            View.Activate(sessionId);
        }
    }
}
