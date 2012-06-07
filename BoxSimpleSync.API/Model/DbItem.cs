using MongoDB.Bson;

namespace BoxSimpleSync.API.Model
{
    public class DbItem
    {
        #region Properties and Indexers

        public ObjectId Id { get; set; }
        public string FullPath { get; set; }
        public string Sha1 { get; set; }

        #endregion
    }
}