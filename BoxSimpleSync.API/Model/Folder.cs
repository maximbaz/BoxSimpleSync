using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BoxSimpleSync.API.Model
{
    [DebuggerDisplay("Id = {Id}, Name = {Name}, Parent = {ParentId}")]
    public class Folder : Item
    {
        #region Properties and Indexers

        public string ParentId { get; set; }
        public List<Item> Items { get; set; }

        #endregion

        #region Constructors and Destructor

        public Folder() {
            Type = "folder";
        }

        #endregion
    }
}