using System;
using System.Windows;
using BoxSimpleSync.API;

namespace BoxSimpleSync.UI
{
    public partial class MainWindow
    {
        #region Constructors and Destructor

        public MainWindow() {
            InitializeComponent();
            var box = new Api();
            var user = new User("email", "password");
            Action onAuth = () => box.GetFolderInfo("0", user.AuthInfo, folder => MessageBox.Show("OK"));
//            Action onAuth = () => box.UploadFiles(Directory.GetFiles(@"E:\AeroFS\Фото - Наши\2010.11.17 Катюша и мы"), "285935855", user.AuthInfo, () => MessageBox.Show("OK"));
            box.Authenticate(user, onAuth);
        }

        #endregion
    }
}