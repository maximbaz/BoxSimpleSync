using System;
using System.Net;
using System.Web;
using System.Xml;

namespace BoxSimpleSync.API
{
    public class Authentication
    {
        private const string Server = "https://www.box.com/api/1.0/";
        private const string Auth = Server + "auth/";
        private const string Rest = Server + "rest?action=";
        private readonly User user;

        public Authentication(User user) {
            this.user = user;
        }

        public void Login(Action onComplete)
        {
            DownloadStringCompletedEventHandler processTicket = (sender, args) => RetrieveTicket(args.Result, onComplete);
            HttpRequest.Get(Rest + "get_ticket", processTicket);
        }

        private void RetrieveTicket(string response, Action onComplete) {
            var xml = new XmlDocument();
            xml.LoadXml(response);
            user.AuthInfo.Ticket = xml.DocumentElement.SelectSingleNode("ticket").InnerText;
            SendAuthenticationData(onComplete);
        }

        private void SendAuthenticationData(Action onComplete) {
            UploadStringCompletedEventHandler retrieveAuthToken = (sender, args) => RetrieveAuthToken(onComplete);
            HttpRequest.Post(Auth + user.AuthInfo.Ticket, string.Format("login={0}&password={1}&dologin=1&__login=1", HttpUtility.UrlEncode(user.Login), HttpUtility.UrlEncode(user.Password)), retrieveAuthToken);
        }

        private void RetrieveAuthToken(Action onComplete)
        {
            DownloadStringCompletedEventHandler saveAuthToken = (sender, args) => SaveAuthToken(args.Result, onComplete);
            HttpRequest.Get(Rest + "get_auth_token&ticket=" + user.AuthInfo.Ticket, saveAuthToken);
        }

        private void SaveAuthToken(string response, Action onComplete ) {
            var xml = new XmlDocument();
            xml.LoadXml(response);
            user.AuthInfo.Token = xml.DocumentElement.SelectSingleNode("auth_token").InnerText;
            onComplete();
        }
    }
}