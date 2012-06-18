using System.Threading.Tasks;
using System.Web;
using System.Xml;
using BoxSimpleSync.API.Exceptions;
using HttpRequest = BoxSimpleSync.API.Helpers.HttpRequest;

namespace BoxSimpleSync.API.Request
{
    public static class Authentication
    {
        #region Static Fields and Constants

        private const string Server = "https://www.box.com/api/1.0/";
        private const string Auth = Server + "auth/";
        private const string Rest = Server + "rest?action=";

        private static string ticket;

        #endregion

        #region Public and Internal Methods

        public static async Task<string> Login(string email, string password) {
            RetrieveTicket(await HttpRequest.Get(Rest + "get_ticket", null));
            await SendAuthenticationData(email, password);
            return await RetrieveAuthToken();
        }

        #endregion

        #region Protected And Private Methods

        private static void RetrieveTicket(string response) {
            var xml = new XmlDocument();
            xml.LoadXml(response);
            var document = xml.DocumentElement;
            XmlNode node;
            if (document != null && (node = document.SelectSingleNode("ticket")) != null) {
                ticket = node.InnerText;
            } else {
                throw new AuthenticationException("ticket");
            }
        }

        private static Task SendAuthenticationData(string email, string password) {
            return HttpRequest.Post(Auth + ticket,
                                    string.Format("login={0}&password={1}&dologin=1&__login=1",
                                                  HttpUtility.UrlEncode(email),
                                                  HttpUtility.UrlEncode(password)), null);
        }

        private static async Task<string> RetrieveAuthToken() {
            var response = await HttpRequest.Get(Rest + "get_auth_token&ticket=" + ticket, null);
            var xml = new XmlDocument();
            xml.LoadXml(response);
            var document = xml.DocumentElement;
            XmlNode node; 
            if (document != null && (node = document.SelectSingleNode("auth_token")) != null) {
                return node.InnerText;
            }
            throw new AuthenticationException("token");
        }

        #endregion
    }
}