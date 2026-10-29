using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using KendoNET.DynamicLinq.Test.Models;

namespace KendoNET.DynamicLinq.Test.Data
{
    /// <summary>
    /// A MongoDB collection for the provider tests, available only when KENDONET_MONGODB holds a connection string,
    /// e.g. mongodb://localhost:27017 after "docker run -d -p 27017:27017 mongo:8".
    /// </summary>
    public static class MongoDbContext
    {
        public const string ConnectionStringVariable = "KENDONET_MONGODB";

        static MongoDbContext()
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
            BsonClassMap.RegisterClassMap<Employee>(map =>
            {
                map.AutoMap();
                map.MapIdMember(e => e.Number);
                map.UnmapMember(e => e.Company);
            });
        }

        /// <summary>
        /// Resets the collection to MockData's employees. Returns null when no connection string is set.
        /// </summary>
        public static IMongoCollection<Employee> Employees()
        {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
            if (string.IsNullOrEmpty(connectionString)) return null;

            var database = new MongoClient(connectionString).GetDatabase("Kendo");
            database.DropCollection(nameof(Employee));
            var collection = database.GetCollection<Employee>(nameof(Employee));
            collection.InsertMany(MockData.Employees());
            return collection;
        }
    }
}
