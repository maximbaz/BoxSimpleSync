using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.UI
{
    public partial class MainWindow
    {
        #region Constructors and Destructor

        public MainWindow() {
            InitializeComponent();
        }

        #endregion

        private void SyncronizeNowClick(object sender, RoutedEventArgs e)
        {
            var paths = new List<PathsPair> {
                new PathsPair {Local = @"E:\boxSyncTest1", Server = "test/test1"}
            };

            var user = new User("email", "password");
            new Synchronization(user).Start(paths);
        }
    }
}