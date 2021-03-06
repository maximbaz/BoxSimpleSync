using System.Collections.Generic;

namespace BoxSimpleSync.API.Model
{
    public class Folder : Item
    {
        #region Constructors and Destructor

        public Folder() {
            Type = "folder";
            Items = new List<Item>();
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public List<Item> Items { get; set; }

        #endregion
    }
}