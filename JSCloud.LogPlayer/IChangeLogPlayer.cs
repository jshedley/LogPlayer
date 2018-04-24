using JSCloud.LogPlayer.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer
{
    public interface IChangeLogPlayer<T, I>
        where T : IChangeLogTracked<I>, new()
        where I : struct
    {
        /// <summary>
        /// Apply a change log to the passed in instance
        /// </summary>
        /// <param name="change">The change being made</param>
        /// <param name="o">The instance to apply the change too</param>
        void Apply(ChangeLog<I> change, T o);

        /// <summary>
        /// Rebuild an entire instance from a series of change logs
        /// </summary>
        /// <param name="changeLogs">The change logs to apply</param>
        /// <returns>A reconstructed instance</returns>
        T RebuildFromLogs(ICollection<ChangeLog<I>> changeLogs);

        /// <summary>
        /// Calculatye the changes between two instances
        /// </summary>
        /// <param name="source">The source instance</param>
        /// <param name="destination">The destination instance</param>
        /// <param name="changedBy">The user creating the changes</param>
        /// <returns>The changes that have been made</returns>
        ICollection<ChangeLog<I>> CalculateChanges(T source, T destination, int? changedBy);
    }
}
