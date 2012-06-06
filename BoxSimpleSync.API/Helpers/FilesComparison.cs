using System.Linq;
using BoxSimpleSync.API.Model;
using FluentMongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace BoxSimpleSync.API.Helpers
{
    public class FilesComparison
    {
        #region Fields

        private readonly Pair<File> files;

        #endregion

        #region Constructors and Destructor

        public FilesComparison(Pair<File> files) {
            this.files = files;
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public bool Updated {
            get { return !LocalIdenticalToServer; }
        }

        public bool UpdatedOnLocal {
            get { return !LocalIdenticalToServer && ServerIdenticalToDb; }
        }

        public bool UpdatedOnServer {
            get { return !ServerIdenticalToDb && LocalIdenticalToDb; }
        }

        public bool DeletedOnLocal {
            get { return ServerIdenticalToDb; }
        }

        public bool CreatedOnServer {
            get { return !(from f in MongoDbQuery where f.FullPath == files.Local select f).Any(); }
        }

        public bool PreviousStateIsUnknown {
            get { return !ExistsInDb; }
        }

        public bool DeletedOnServer {
            get { return ExistsInDb; }
        }

        #endregion

        #region Public and Internal Methods

        public static void Save(string file) {
            var savedFile = (from f in MongoDbQuery where f.FullPath == file select f).SingleOrDefault() ?? new MiniFile {FullPath = file};
            savedFile.Sha1 = File.ComputeSha1(file);
            MongoDb.Save(savedFile);
        }

        public static void Delete(string file) {
            MongoDb.Remove(Query.EQ("FullPath", file));
        }

        #endregion

        #region Protected and Private Properties and Indexers

        private bool LocalIdenticalToServer {
            get { return File.ComputeSha1(files.Local) == files.Server.Sha1; }
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

        private bool LocalIdenticalToDb {
            get {
                var miniFile = (from f in MongoDbQuery where f.FullPath == files.Local select f).Single();
                return File.ComputeSha1(files.Local) == miniFile.Sha1;
            }
        }

        private bool ServerIdenticalToDb {
            get {
                var miniFile = (from f in MongoDbQuery where f.FullPath == files.Local select f).Single();
                return miniFile.Sha1 == files.Server.Sha1;
            }
        }

        private bool ExistsInDb {
            get { return (from f in MongoDbQuery where f.FullPath == files.Local select f).Any(); }
        }

        #endregion

        #region Nested type: MiniFile

        public class MiniFile
        {
            #region Properties and Indexers

            public ObjectId Id { get; set; }
            public string FullPath { get; set; }
            public string Sha1 { get; set; }

            #endregion
        }

        #endregion
    }
}