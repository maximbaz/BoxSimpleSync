using System.Diagnostics;

namespace BoxSimpleSync.API.Model
{
    [DebuggerDisplay("Server = {Server}, Local = {Local}")]
    public class Pair<T>
    {
        #region Constructors and Destructor

        public Pair(T server, string local) {
            Server = server;
            Local = local;
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public T Server { get; set; }
        public string Local { get; set; }

        #endregion
    }
}