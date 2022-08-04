﻿using JetBrains.Annotations;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;
using Volo.CmsKit.Tags;
using Tag = Volo.CmsKit.Tags.Tag;

namespace Volo.CmsKit.MongoDB.Tags;

public class MongoEntityTagRepository : MongoDbRepository<ICmsKitMongoDbContext, EntityTag>, IEntityTagRepository
{
    public MongoEntityTagRepository(IMongoDbContextProvider<ICmsKitMongoDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }

    public virtual async Task DeleteManyAsync(Guid[] tagIds, CancellationToken cancellationToken = default)
    {
        var token = GetCancellationToken(cancellationToken);

        var collection = await GetCollectionAsync(token);
        await collection.DeleteManyAsync(Builders<EntityTag>.Filter.In(x => x.TagId, tagIds), token);
    }

    public virtual Task<EntityTag> FindAsync(
        [NotNull] Guid tagId,
        [NotNull] string entityId,
        [CanBeNull] Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        Check.NotNullOrEmpty(entityId, nameof(entityId));
        return base.FindAsync(x =>
                x.TagId == tagId &&
                x.EntityId == entityId &&
                x.TenantId == tenantId,
            cancellationToken: GetCancellationToken(cancellationToken));
    }

    public virtual async Task<List<string>> GetEntityIdsFilteredByTagAsync(
        [NotNull] Guid tagId,
        [CanBeNull] Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var blogPostQueryable = (await GetQueryableAsync())
            .Where(q => q.TagId == tagId && q.TenantId == tenantId)
            .Select(q => q.EntityId);

        return await AsyncExecuter.ToListAsync(blogPostQueryable, GetCancellationToken(cancellationToken));
    }

    public async Task<List<string>> GetEntityIdsFilteredByTagNameAsync(
        [NotNull] string tagName,
        [NotNull] string entityType,
        [CanBeNull] Guid? tenantId=null,
        CancellationToken cancellationToken = default)
    {
        var entityTagQueryable = await GetMongoQueryableAsync(GetCancellationToken(cancellationToken));
        var tagQueryable = await GetMongoQueryableAsync<Tag>(GetCancellationToken(cancellationToken));
        var resultQueryable = from et in entityTagQueryable
            join t in tagQueryable on et.TagId equals t.Id
            where t.Name == tagName 
                  && t.EntityType == entityType 
                  && et.TenantId == tenantId 
                  && t.TenantId == tenantId
                  && !t.IsDeleted
            select et.EntityId;
        return await AsyncExecuter.ToListAsync(resultQueryable, GetCancellationToken(cancellationToken));
    }
}
