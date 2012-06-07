using System.Diagnostics;

namespace BoxSimpleSync.API.Model
{
    [DebuggerDisplay("Id = {Id}, Name = {Name}, Sha1 = {Sha1}, Type = {Type}")]
    public class Item
    {
        #region Public and Internal Properties and Indexers

        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Sha1 { get; set; }

        #endregion
    }
}