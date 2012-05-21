using System.Threading.Tasks;
using System.Web;
using System.Xml;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Request
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

        public async Task Login() {
            RetrieveTicket(await HttpRequest.Get(Rest + "get_ticket", null));
            SendAuthenticationData();
            await RetrieveAuthToken();
        }

        private void RetrieveTicket(string response) {
            var xml = new XmlDocument();
            xml.LoadXml(response);
            user.AuthInfo.Ticket = xml.DocumentElement.SelectSingleNode("ticket").InnerText;
        }

        private void SendAuthenticationData() {
            HttpRequest.Post(Auth + user.AuthInfo.Ticket,
                             string.Format("login={0}&password={1}&dologin=1&__login=1",
                                           HttpUtility.UrlEncode(user.Login),
                                           HttpUtility.UrlEncode(user.Password)), null);
        }

        private async Task RetrieveAuthToken() {
            var authToken = await HttpRequest.Get(Rest + "get_auth_token&ticket=" + user.AuthInfo.Ticket, null);
            SaveAuthToken(authToken);
        }

        private void SaveAuthToken(string response) {
            var xml = new XmlDocument();
            xml.LoadXml(response);
            user.AuthInfo.Token = xml.DocumentElement.SelectSingleNode("auth_token").InnerText;
        }
    }
}