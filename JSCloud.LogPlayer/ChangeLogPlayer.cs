using JSCloud.LogPlayer;
using JSCloud.LogPlayer.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer
{
    public class ChangeLogPlayer<T,I> : IChangeLogPlayer<T,I> 
        where T : IChangeLogTracked<I>, new()
        where I : struct
    {
        private static Type logPlayerType = typeof(T);
        private static ConcurrentDictionary<string, PropertyInfo> properties = new ConcurrentDictionary<string, PropertyInfo>();
        private static ConcurrentDictionary<string, Type> systemTypes = new ConcurrentDictionary<string, Type>();

        public ChangeLogPlayer()
        {
            lock(properties)
            {
                if(properties.Count == 0)
                {
                    properties = new ConcurrentDictionary<string, PropertyInfo>(logPlayerType.GetProperties().ToDictionary(x => x.Name));
                }
            }
        }

        public void Apply(ChangeLog<I> change, T o)
        {
            if (o != null && o.GetType().FullName == change.FullTypeName)
            {
                o.ObjectId = change.ObjectId;

                var property = properties[change.Property];

                if (property.GetValue(o) == null)
                {
                    property.SetValue(o, null);
                }
                else
                {
                    Type propertyType;
                    if (!systemTypes.ContainsKey(change.PropertySystemType))
                    {
                        propertyType = Type.GetType(change.PropertySystemType);
                        systemTypes.TryAdd(change.PropertySystemType, propertyType);
                    }
                    else
                    {
                        propertyType = systemTypes[change.PropertySystemType];
                    }

                    property.SetValue(o, System.Convert.ChangeType(change.Value, propertyType));
                }
            }
            else
            {
                throw new ArgumentException($"Unable to process as type {change.FullTypeName} is not applyable to {o?.GetType()}.");
            }
        }

        public ICollection<ChangeLog<I>> CalculateChanges(T source, T destination, int? changedBy)
        {
            ICollection<ChangeLog<I>> changes = new LinkedList<ChangeLog<I>>();

            if (source != null && destination != null)
            {
                if (source.GetHashCode() != destination.GetHashCode()) //Let's only process if they items are different
                {

                    foreach(var property in properties)
                    {
                        string sourceProperty = property.Value.GetValue(source)?.ToString();
                        string destinationProperty = property.Value.GetValue(destination)?.ToString();

                        if(sourceProperty != destinationProperty)
                        {
                            changes.Add(new ChangeLog<I>()
                            {
                             FullTypeName = logPlayerType.FullName,
                             ObjectId = source.ObjectId,
                             Property = property.Key,
                             PropertySystemType = property.Value.PropertyType.FullName,
                             Value = sourceProperty == null ? string.Empty : sourceProperty.ToString(),
                             ChangedBy = changedBy,
                             ChangedUtc = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentNullException(source == null ? "Source" : "Destination");
            }

            return changes;
        }

        public T RebuildFromLogs(ICollection<ChangeLog<I>> changeLogs)
        {
            T item = new T();
            for (int i = 0; i < changeLogs.OrderBy(x => x.ChangedUtc).ToList().Count(); i++)
            {
                var change = changeLogs.OrderBy(x => x.ChangedUtc).ToList()[i];
                this.Apply(change, item);
            }
            return item;
        }
    }
}
