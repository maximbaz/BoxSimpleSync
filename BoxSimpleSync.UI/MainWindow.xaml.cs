using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using BoxSimpleSync.API;
using BoxSimpleSync.API.Comparisons;
using BoxSimpleSync.API.Exceptions;
using BoxSimpleSync.API.Model;
using BoxSimpleSync.API.Request;

namespace BoxSimpleSync.UI
{
    public partial class MainWindow
    {
        private Synchronization synchronization;
        private Api api;

        #region Constructors and Destructor

        public MainWindow() {
            InitializeComponent();
        }

        #endregion

        #region Protected And Private Methods

        private async void SynchronizeNowClick(object sender, RoutedEventArgs e) {

            try {
                var paths = new List<Pair<string>> {
                    new Pair<string>("test/test1", @"E:\boxSyncTest1"),
                };

                if (synchronization == null) {
                    synchronization = await CreateSynchronization();
                }

                if (await UserIsLogOff(api)) {
                    api.RefreshAuthToken(await GenerateAuthToken);
                }

                synchronization.Start(paths);
            }
            catch (AuthenticationException) {
                LogEvent("Authentication FAILED");
            }
        }

        protected Task<string> GenerateAuthToken {
            get {
                return Authentication.Login("user", "password");
            }
        }

        private async Task<Synchronization> CreateSynchronization() {
            api = new Api(await GenerateAuthToken, new Files(), new Folders());

            var sync = new Synchronization(api, new FilesComparison(), new FoldersComparison());
            sync.Preparing += () => LogEvent("Preparing");
            sync.Synchronizing += () => LogEvent("Synchronizing");
            sync.Downloading += () => LogEvent("Downloading");
            sync.Uploading += () => LogEvent("Uploading");
            sync.Deleting += () => LogEvent("Deleting");
            sync.Done += () => LogEvent("Done");
            sync.UnknownError += () => LogEvent("Unknown Error");

            return sync;
        }

        private void LogEvent(string text) {
            var item = string.Format("{0} at {1}", text, DateTime.Now.ToShortTimeString());
            Log.Items.Add(item);
            Log.ScrollIntoView(item);
        }

        private static async Task<bool> UserIsLogOff(Api api) {
            try {
                await api.Folders.GetInfo("0");
                return false;
            }
            catch (WebException e) {
                if (((HttpWebResponse) e.Response).StatusCode == HttpStatusCode.Unauthorized) {
                    return true;
                }
                throw;
            }
        }

        #endregion
    }
}