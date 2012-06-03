using System.Diagnostics;

namespace BoxSimpleSync.API.Model
{
    [DebuggerDisplay("Server = {Server}, Local = {Local}")]
    public class ItemsPair<T> where T:Item
    {
        public ItemsPair(T server, string local)
        {
            Server = server;
            Local = local;
        }

        public T Server { get; set; }
        public string Local { get; set; }
    }


}
