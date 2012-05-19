using System.Collections.Generic;

namespace BoxSimpleSync.API
{
    public class Folder
    {
        #region Properties and Indexers

        public string Id { get; set; }
        public string Name { get; set; }
        public string CreatedAt { get; set; }
        public string ModifiedAt { get; set; }
        public List<Item> Items { get; set; }

        #endregion
    }
}