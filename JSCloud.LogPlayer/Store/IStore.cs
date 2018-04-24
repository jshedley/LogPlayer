using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer.Store
{
    public interface IStore<I> where I:struct
    {
        /// <summary>
        /// Stores a single change log item
        /// </summary>
        /// <param name="changeLog">The change log item to be stored</param>
        /// <returns>The commited change log</returns>
       Task<Types.ChangeLog<I>> StoreAsync(Types.ChangeLog<I> changeLog);

        /// <summary>
        /// Stores the collection of the change logs
        /// </summary>
        /// <param name="changeLogs">The change logs to be saved</param>
        /// <returns>The change logs commited to the store</returns>
        Task<ICollection<Types.ChangeLog<I>>> StoreAsync(ICollection<Types.ChangeLog<I>> changeLogs);

        /// <summary>
        /// Get all changes for a specific object
        /// </summary>
        /// <param name="objectId">The object ID of the object to get the changes for. *NULL will result in all changes
        /// for this type coming back.*</param>
        /// <param name="fullTypeName">The full name of the type to get the changes for.</param>
        /// <returns></returns>
        Task<ICollection<Types.ChangeLog<I>>> GetChangesAsync(int? objectId, string fullTypeName);

        /// <summary>
        /// Provision the data store
        /// </summary>
        Task Provision();
    }
}
