using System.Collections.Generic;
using System.Windows;
using BoxSimpleSync.API;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.UI
{
    public partial class MainWindow
    {
        #region Fields

        private readonly Synchronization synchronization;

        #endregion

        #region Constructors and Destructor

        public MainWindow() {
            InitializeComponent();

            synchronization = new Synchronization();
            synchronization.Authenticating += () => Status.Content = "Authenticating ...";
            synchronization.Preparing += () => Status.Content = "... Preparing ...";
            synchronization.Synchronizating += () => Status.Content = "... Synchronizating ...";
            synchronization.Done += () => Status.Content = "... Done!";
        }

        #endregion

        #region Protected And Private Methods

        private void SynchronizeNowClick(object sender, RoutedEventArgs e) {
            var paths = new List<Pair<string>> {
                new Pair<string>("test/test1", @"E:\boxSyncTest1")
            };

            synchronization.Start(paths);
        }

        #endregion
    }
}