using System.Linq;
using BoxSimpleSync.API.Model;
using FluentMongo.Linq;
using MongoDB.Driver;

namespace BoxSimpleSync.UI
{
    public class FilesComparison
    {
        #region Properties and Indexers

        public bool Updated {
            get { return !LocalIdenticalToServer; }
        }

        private bool LocalIdenticalToServer {
            get { return File.ComputeSha1(files.Local) == files.Server.Sha1; }
        }

        public bool UpdatedOnLocal {
            get { return !LocalIdenticalToServer && ServerIdenticalToDb; }
        }

        private static MongoCollection<MiniFile> MongoDb {
            get {
                return MongoServer.Create("mongodb://localhost/?safe=true")
                    .GetDatabase("BoxSimpleSync")
                    .GetCollection<MiniFile>("files");
            }
        }

        private static IQueryable<MiniFile> MongoDbQuery {
            get { return MongoDb.AsQueryable(); }
        }

        public bool UpdatedOnServer {
            get { return !ServerIdenticalToDb && LocalIdenticalToDb; }
        }

        private bool LocalIdenticalToDb {
            get {
                var miniFile = (from f in MongoDbQuery where f.FullPath == files.Local select f).Single();
                return File.ComputeSha1(files.Local) == miniFile.Sha1;
            }
        }

        public bool DeletedOnLocal {
            get { return ServerIdenticalToDb; }
        }

        private bool ServerIdenticalToDb {
            get {
                var miniFile = (from f in MongoDbQuery where f.FullPath == files.Local select f).Single();
                return miniFile.Sha1 == files.Server.Sha1;
            }
        }

        public bool CreatedOnServer {
            get { return !(from f in MongoDbQuery where f.FullPath == files.Local select f).Any(); }
        }

        public bool PreviousStateIsUnknown {
            get { return !ExistsInDb; }
        }

        private bool ExistsInDb {
            get { return (from f in MongoDbQuery where f.FullPath == files.Local select f).Any(); }
        }

        public bool DeletedOnServer
        {
            get { return ExistsInDb; }
        }

        #endregion

        #region Fields

        private readonly ItemsPair<File> files;

        #endregion

        #region Constructors and Destructor

        public FilesComparison(ItemsPair<File> files) {
            this.files = files;
        }

        #endregion

        
        #region Protected And Private Methods

        private void EnsureExistsInDb() {
            if (!(from f in MongoDbQuery where f.FullPath == files.Local select f).Any()) {
                MongoDb.Insert(new MiniFile {FullPath = files.Local, Sha1 = files.Server.Sha1});
            }
        }

        #endregion

        #region Nested type: MiniFile

        public class MiniFile
        {
            #region Properties and Indexers

            public string FullPath { get; set; }
            public string Sha1 { get; set; }

            #endregion
        }

        #endregion
    }
}