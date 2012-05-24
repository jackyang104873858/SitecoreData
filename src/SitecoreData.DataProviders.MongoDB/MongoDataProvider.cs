﻿using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Sitecore;
using Sitecore.Data;

namespace SitecoreData.DataProviders.MongoDB
{
    public class MongoDataProvider : DataProviderBase, IWritableDataProvider, IDisposable
    {
        public MongoDataProvider(string connectionString) : base(connectionString)
        {
            // TODO: Figure out parameters for connectionstring

            // TODO: SubClass Item*Dto to decorate with BSON attributes
            SafeMode = SafeMode.True;

            JoinParentId = ID.Null;

            const string conn = "mongodb://localhost:27017/web";

            var databaseName = MongoUrl.Create(conn).DatabaseName;

            Server = MongoServer.Create(conn);

            Db = Server.GetDatabase(databaseName);

            Items = Db.GetCollection<ItemDto>("items", SafeMode);
            Items.EnsureIndex(IndexKeys.Ascending(new[] {"ParentID"}));
            Items.EnsureIndex(IndexKeys.Ascending(new[] {"TemplateID"}));
        }

        private ID JoinParentId { get; set; }

        private MongoServer Server { get; set; }

        private MongoDatabase Db { get; set; }

        private MongoCollection<ItemDto> Items { get; set; }

        private SafeMode SafeMode { get; set; }

        public void Dispose()
        {
        }

        public bool CreateItem(Guid id, string name, Guid templateId, Guid parentId)
        {
            var exists = GetItem(id);

            if (exists != null)
            {
                return true;
            }

            var item = new ItemDto
                           {
                               Id = id,
                               Name = name,
                               TemplateId = templateId,
                               ParentId = parentId
                           };

            Store(item);

            return true;
        }

        public bool DeleteItem(Guid id)
        {
            var result = Items.Remove(Query.EQ("_id", id), RemoveFlags.Single, SafeMode);

            return result != null && result.Ok;
        }

        public void Store(ItemDto item)
        {
            Items.Save(item, SafeMode);
        }

        public override ItemDto GetItem(Guid id)
        {
            return Items.FindOneByIdAs<ItemDto>(id);
        }

        public override IEnumerable<Guid> GetChildIds(Guid parentId)
        {
            var query = Query.EQ("ParentID",
                                 parentId == JoinParentId.ToGuid()
                                     ? Guid.Empty
                                     : parentId);

            return Items.FindAs<ItemDto>(query).Select(it => it.Id).ToArray();
        }

        public override Guid GetParentId(Guid id)
        {
            var result = Items.FindOneByIdAs<ItemDto>(id);
            
            return result != null ? (result.ParentId != Guid.Empty ? result.ParentId : JoinParentId.ToGuid()) : Guid.Empty;
        }

        public override IEnumerable<Guid> GetTemplateIds(Guid templateId)
        {
            var query = Query.EQ("TemplateID", TemplateIDs.Template.ToGuid());
            var ids = new List<Guid>();

            foreach (var id in Items.FindAs<ItemDto>(query).Select(it => it.Id))
            {
                ids.Add(id);
            }

            return ids;
        }
    }
}