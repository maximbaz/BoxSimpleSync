using MongoDB.Bson;

namespace BoxSimpleSync.API.Model
{
    public class MiniItem
    {
        #region Properties and Indexers

        public ObjectId Id { get; set; }
        public string FullPath { get; set; }

        #endregion
    }
}