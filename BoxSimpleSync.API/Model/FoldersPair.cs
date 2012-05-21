using System.Diagnostics;

namespace BoxSimpleSync.API.Model
{
    [DebuggerDisplay("Server = {Server}, Local = {Local}")]
    public class FoldersPair
    {
        public FoldersPair(Folder server, string local) {
            Server = server;
            Local = local;
        }

        public Folder Server { get; set; }
        public string Local { get; set; }
    }
}
