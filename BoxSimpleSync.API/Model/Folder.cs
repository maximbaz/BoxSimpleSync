using System.Collections.Generic;
using System.Diagnostics;

namespace BoxSimpleSync.API.Model
{
    [DebuggerDisplay("Id = {Id}, Name = {Name}")]
    public class Folder : Item
    {
        #region Constructors and Destructor

        public Folder() {
            Type = "folder";
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public List<Item> Items { get; set; }

        #endregion
    }
}