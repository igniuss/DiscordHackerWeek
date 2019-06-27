using LiteDB;
using System;
using System.Collections.Generic;
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

        public static T FindOne<T>(string dbName, string tableName, Expression<Func<T, bool>> predicate) {
            using (var db = new LiteDatabase(dbName)) {
                var table = db.GetCollection<T>(tableName);
                var result = table.FindOne(predicate);
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

        public static IEnumerable<T> GetAll<T>(string dbName, string tableName) {
            using(var db = new LiteDatabase(dbName)) {
                var table = db.GetCollection<T>(tableName);
                return table.FindAll();
            }
        }

        public static bool Upsert<T>(string dbName, string tableName, T data) {
            using(var db = new LiteDatabase(dbName)) {
                var table = db.GetCollection<T>(tableName);
                return table.Upsert(data);
            }
        }
    }
}
