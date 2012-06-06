using System;
using System.Collections.Generic;
using System.Linq;
using BoxSimpleSync.API.Model;
using Newtonsoft.Json.Linq;

namespace BoxSimpleSync.API.Helpers
{
    public static class JsonParse
    {
        #region Public and Internal Methods

        public static Folder Folder(string json) {
            var jObject = JObject.Parse(json);
            var items = (from r in jObject["item_collection"]["entries"]
                         select new Item {
                             Id = r["id"].ToString(),
                             Name = r["name"].ToString(),
                             Type = r["type"].ToString()
                         }).ToList();

            return new Folder {
                Id = Value(jObject, "id"),
                Name = Value(jObject, "name"),
                ParentId = Value(jObject, "parent", "id"),
                CreatedAt = DateTime(jObject, "created_at"),
                ModifiedAt = DateTime(jObject, "modified_at"),
                Items = items
            };
        }

        public static File File(string json) {
            var jObject = JObject.Parse(json);
            return new File {
                Id = Value(jObject, "id"),
                Name = Value(jObject, "name"),
                CreatedAt = DateTime(jObject, "created_at"),
                ModifiedAt = DateTime(jObject, "modified_at"),
                Sha1 = Value(jObject, "sha1")
            };
        }

        public static List<File> FilesList(string json) {
            var jObject = JObject.Parse(json);
            return jObject["entries"].Children().ToList().Select(file => File(file.ToString())).ToList();
        }

        #endregion

        #region Protected And Private Methods

        private static string Value(JToken obj, params string[] keys) {
            var result = obj;
            foreach (var key in keys) {
                if (result == null || !result.HasValues)
                    return null;
                result = result[key];
            }
            return result == null ? null : result.ToString();
        }

        private static DateTime DateTime(JToken obj, params string[] keys) {
            DateTime result;
            return System.DateTime.TryParse(Value(obj, keys), out result) ? result : new DateTime();
        }

        #endregion
    }
}