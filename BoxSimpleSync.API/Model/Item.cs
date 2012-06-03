using System;
using System.Diagnostics;

namespace BoxSimpleSync.API.Model
{
    [DebuggerDisplay("Id = {Id}, Name = {Name}, Type = {Type}")]
    public class Item
    {
        #region Properties and Indexers

        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        #endregion
    }
}