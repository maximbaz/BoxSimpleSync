namespace BoxSimpleSync.API.Model
{
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