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
//            var user = new User("email", "password");
            var sync = new Synchronization(user);

            var paths = new List<PathsPair> {
                new PathsPair {Local = @"E:\boxSyncTest1", Server = "test/test1"}
            };

            sync.Start(paths);
        }

        #endregion
    }
}