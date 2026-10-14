using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Template base entity. Also implements ABP's <see cref="Volo.Abp.Domain.Entities.IEntity{TKey}"/>
    /// so every entity can be used with ABP repositories (<see cref="Volo.Abp.Domain.Repositories.IRepository{TEntity,TKey}"/>).
    /// </summary>
    public abstract class Entity<TKey> : IHasKey<TKey>, ITrackable, Volo.Abp.Domain.Entities.IEntity<TKey>
    {
        public TKey Id { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public DateTimeOffset? UpdatedDateTime { get; set; }

        /// <summary>
        /// ABP <see cref="Volo.Abp.Domain.Entities.IEntity"/> contract.
        /// </summary>
        public virtual object[] GetKeys()
        {
            return new object[] { Id };
        }

        string Volo.Abp.IKeyedObject.GetObjectKey()
        {
            return Id?.ToString();
        }
    }
}
