using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSCloud.LogPlayer.Types;

namespace JSCloud.LogPlayer.Store
{
    public class MicrosoftSqlStore<I> : IStore<I>
        where I:struct, IComparable<I>
    {

        private string _select => $"select * from [{_tableSchema}].[{_tableName}] where ";
        private string _insert => $"insert into [{_tableSchema}].[{_tableName}] select @changeLogId, @ObjectId, @fullTypeName, @propertySystemType, @property, @value, @changedBy, @ChangedUtc";
        private string _createTable => $"IF OBJECT_ID(N'{_tableSchema}.{_tableName}', N'U') IS NULL \n" +
            $"BEGIN \n" +
            $"create table  [{_tableSchema}].[{_tableName}] ( \n" +
            $"ChangeLogId UniqueIdentifier not null, \n" +
            $"ObjectId {SqlHelper.GetDbType<I>().ToString()} null, \n" +
            $"FullTypeName nvarchar(250) not null, \n" +
            $"PropertySystemType nvarchar(20) not null, \n" +
            $"Property nvarchar(100) not null, \n" +
            $"Value nvarchar(max) null, \n" +
            $"ChangedBy int null, \n" +
            $"ChangedUtc datetime not null \n" +
            $") \n" +
            $"END";


        private readonly string _connectionString;
        private readonly string _tableSchema;
        private readonly string _tableName;
        private readonly int _commandTimeout;

        public MicrosoftSqlStore(string connectionString, string tableSchema, string tableName, int commandTimeout = 120)
        {
           _connectionString = connectionString;
           _tableSchema = tableSchema;
           _tableName = tableName;
           _commandTimeout = commandTimeout;
        }

        public async Task<ICollection<ChangeLog<I>>> GetChangesAsync(I? objectId, string fullTypeName)
        {
            ICollection<ChangeLog<I>> changes = new LinkedList<ChangeLog<I>>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        if (objectId.HasValue)
                        {
                            command.CommandText = $"{_select} FullTypeName = @fullTypeName  AND ObjectId = @objectId";
                            command.Parameters.Add(new SqlParameter("fullTypeName", fullTypeName));
                            command.Parameters.Add(new SqlParameter("objectId", objectId));
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(fullTypeName))
                            {
                                command.CommandText = $"{_select} 1 = 1 ";
                            }
                            else
                            {
                                command.CommandText = $"{_select} FullTypeName = @fullTypeName ";
                                command.Parameters.Add(new SqlParameter("fullTypeName", fullTypeName));
                            }
                        }
                        
                        command.CommandTimeout = _commandTimeout;

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                var read = await reader.ReadAsync();
                                while (read)
                                {
                                    changes.Add(getFromDataReader(reader));
                                    read = await reader.ReadAsync();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (connection.State != System.Data.ConnectionState.Closed)
                    {
                        connection.Close();
                    }
                }
            }
            return changes;
        }

        public async Task<ChangeLog<I>> StoreAsync(ChangeLog<I> changeLog)
        {
            if(changeLog.ChangeLogId.HasValue)
            {
                throw new ArgumentException("The ChangeLogId cannot be set on a new insertion. This is to ensure that the Change Log is not tampered with.");
            }
            else
            {
                changeLog.ChangeLogId = Guid.NewGuid(); //Not we allow it as it is in control of this player.
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = _insert;
                        command.CommandTimeout = _commandTimeout;

                        command.Parameters.Add(new SqlParameter("ChangeLogId", changeLog.ChangeLogId));
                        command.Parameters.Add(new SqlParameter("objectId", changeLog.ObjectId.HasValue ? (object)changeLog.ObjectId : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("FullTypeName", changeLog.FullTypeName));
                        command.Parameters.Add(new SqlParameter("PropertySystemType", changeLog.PropertySystemType));
                        command.Parameters.Add(new SqlParameter("Property", changeLog.Property));
                        command.Parameters.Add(new SqlParameter("Value", changeLog.Value));
                        command.Parameters.Add(new SqlParameter("ChangedBy", changeLog.ChangedBy.HasValue ? (object) changeLog.ChangedBy.Value : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("ChangedUtc", changeLog.ChangedUtc));

                        await command.ExecuteNonQueryAsync();

                    }
                }
                catch(Exception ex)
                {
                    changeLog.ChangeLogId = null;
                    throw new Exception("Unable to store change.", ex);
                }
                finally
                {
                    if (connection.State != System.Data.ConnectionState.Closed)
                    {
                        connection.Close();
                    }
                }
            }
            return changeLog;
        }

        public async Task<ICollection<ChangeLog<I>>> StoreAsync(ICollection<ChangeLog<I>> changeLogs)
        {
            ICollection<ChangeLog<I>> changes = new LinkedList<ChangeLog<I>>(changeLogs);
            if (changeLogs.Any(x => x.ChangeLogId.HasValue))
            {
                throw new ArgumentException("The ChangeLogId cannot be set on any of the new insertions. This is to ensure that the Change Log is not tampered with.");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {

                try
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            DataTable dt = new DataTable();
                            dt.Columns.Add("ChangeLogId", typeof(Guid));
                            dt.Columns.Add("ObjectId", typeof(I));
                            dt.Columns.Add("FullTypeName", typeof(string));
                            dt.Columns.Add("PropertySystemType", typeof(string));
                            dt.Columns.Add("Property", typeof(string));
                            dt.Columns.Add("Value", typeof(string));
                            dt.Columns.Add("ChangedBy", typeof(int));
                            dt.Columns.Add("ChangedUtc", typeof(DateTime));
                            foreach (var changeLog in changeLogs)
                            {
                                changeLog.ChangeLogId = Guid.NewGuid();
                                dt.Rows.Add(new object[] { changeLog.ChangeLogId, changeLog.ObjectId, changeLog.FullTypeName,
                                    changeLog.PropertySystemType, changeLog.Property, changeLog.Value, changeLog.ChangedBy, changeLog.ChangedUtc });
                            }

                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                            {
                                bulkCopy.DestinationTableName = $"[{_tableSchema}].[{_tableName}]";
                                await bulkCopy.WriteToServerAsync(dt);
                            }
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            foreach (var changeLog in changeLogs)
                            {
                                changeLog.ChangeLogId = null;
                            }
                            throw;
                        }
                    }
                }
                finally
                {
                    if (connection.State != System.Data.ConnectionState.Closed)
                    {
                        connection.Close();
                    }
                    
                }
            }

            return changes;
        }

        public async Task Provision()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = _createTable;
                        command.CommandTimeout = _commandTimeout;

                        await command.ExecuteNonQueryAsync();
                    }
                }
                finally
                {
                    if (connection.State != System.Data.ConnectionState.Closed)
                    {
                        connection.Close();
                    }
                }
            }
        }

        private ChangeLog<I> getFromDataReader(IDataReader reader)
        {
            return new ChangeLog<I>()
            {
                ChangeLogId = reader.IsDBNull(0) ? null : (Guid?)reader.GetGuid(0),
                ObjectId = reader.IsDBNull(1) ? null : (I?)reader.GetValue(1),
                FullTypeName = reader.IsDBNull(2) ? null : reader.GetString(2),
                PropertySystemType = reader.IsDBNull(3) ? null : reader.GetString(3),
                Property = reader.IsDBNull(4) ? null : reader.GetString(4),
                Value = reader.IsDBNull(5) ? null : reader.GetString(5),
                ChangedBy = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6),
                ChangedUtc = reader.GetDateTime(7)
            };
        }

    }
}
