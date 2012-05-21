using System.Diagnostics;

namespace BoxSimpleSync.API.Model
{
    [DebuggerDisplay("Server = {Server}, Local = {Local}")]
    public class PathsPair
    {
        public string Server { get; set; }
        public string Local { get; set; }
    }
}