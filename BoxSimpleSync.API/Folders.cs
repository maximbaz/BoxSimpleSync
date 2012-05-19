using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace BoxSimpleSync.API
{
    public class Folders
    {
        private const string Url = "https://api.box.com/2.0/folders/{0}";
        private readonly AuthInfo authInfo;

        public Folders(AuthInfo authInfo) {
            this.authInfo = authInfo;
        }

        public void GetFolderInfo(string id, Action<Folder> onComplete) {
            DownloadStringCompletedEventHandler received = (sender, args) => onComplete(ParseFolder(args.Result));
            Get(id, received);
        }

        public void GetFolderItems(string id, Action<List<Item>> onComplete) {
            //Todo: Make request to https://api.box.com/2.0/folders/FOLDER_ID/items
            Action<Folder> getItems = folder => onComplete(folder.Items);
            GetFolderInfo(id, getItems);
        }

        public void Create(string name, Action<Folder> onComplete) {
            UploadStringCompletedEventHandler onCreated = (sender, args) => ParseFolder(args.Result);
            HttpRequest.Post(name, string.Format("{{\"name\":\"{0}\"}}", name), onCreated);
        }

        private void Get(string id, DownloadStringCompletedEventHandler requestCompleted)
        {
            var client = new WebClient();
            client.Headers.Add("Authorization", string.Format("BoxAuth api_key={0}&auth_token={1}", authInfo.ApiKey, authInfo.Token));
            client.DownloadStringAsync(new Uri(string.Format(Url, id)));
            client.DownloadStringCompleted += requestCompleted;
        }

        private static Folder ParseFolder(string json) {
            var jObject = JObject.Parse(json);
            var items = (from r in jObject["item_collection"]["entries"]
                         select new Item
                         {
                             Id = r["id"].ToString(),
                             Name = r["name"].ToString(),
                             Type = r["type"].ToString()
                         }).ToList();
            return new Folder { Id = jObject["id"].ToString(), Items = items };
        }
    }
}