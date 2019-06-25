using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;

namespace RPGBot {
    public class DB {
        public static BsonValue Insert<T>(string dbName, string tableName, T data) {
            using (var db = new LiteDatabase(dbName)) {
                var table = db.GetCollection<T>(tableName);
                var id = table.Insert(data);
                // returns the id of the inserted value
                return id;
            }
        }

        public static IEnumerable<T> Find<T>(string dbName, string tableName, Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue) {
            using (var db = new LiteDatabase(dbName)) {
                var table = db.GetCollection<T>(tableName);
                var result = table.Find(predicate, skip, limit);
                return result;
            }
        }

        public static bool Update<T>(string dbName, string tableName, T data) {
            using(var db = new LiteDatabase(dbName)) {
                var table = db.GetCollection<T>(tableName);
                // returns false if item not found
                var success = table.Update(data);
                return success;
            }
        }

        public static int Update<T>(string dbName, string tableName, IEnumerable<T> data) {
            using (var db = new LiteDatabase(dbName)) {
                var table = db.GetCollection<T>(tableName);
                // returns number of items updated
                var updates = table.Update(data);
                return updates;
            }
        }
    }
}
