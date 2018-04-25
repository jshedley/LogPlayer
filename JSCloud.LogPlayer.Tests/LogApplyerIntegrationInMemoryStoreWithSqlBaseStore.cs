using JSCloud.LogPlayer.Store;
using JSCloud.LogPlayer.Types;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer.Tests
{
    [TestFixture]
    public class LogApplyerIntegrationInMemoryStoreWithSqlBaseStore: LogApplyerIntegrationTests
    {
        private static string _uid = DateTime.UtcNow.Ticks.ToString();
        private MicrosoftSqlStore<int> baseStore = new MicrosoftSqlStore<int>($"server=.;database=LogPlayer_{_uid};trusted_connection=true;", $"dbo", $"TBL_{_uid}");


        public LogApplyerIntegrationInMemoryStoreWithSqlBaseStore()
        {
            this.Store = new InMemoryStore<int>(baseStore);
        }
              
        

        [OneTimeSetUp()]
        public void provisionTestDatabase()
        {
            using (var connection = new SqlConnection($"server=.;database=master;trusted_connection=true;"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"create database LogPlayer_{_uid}";
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
            this.Store.Provision().GetAwaiter().GetResult();
        }

        [OneTimeTearDown()]
        public void tearDownTestDatabase()
        {
            GC.Collect();

            var dropAllSql = @"
                use master

                declare @dbName as nvarchar(250) = (SELECT min(name) FROM SYS.databases where name like 'LogPlayer%')
                declare @sql as nvarchar(max) = ''

                while @dbName is not null
                begin
	                EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = @dbName

	                set @sql = N'ALTER DATABASE [' + @dbName + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE'
	                exec sp_executesql @sql

	                set @sql = 'DROP DATABASE [' + @dbName +']'
	                exec sp_executesql @sql

	                select @dbName = (SELECT min(name) FROM SYS.databases where name like 'LogPlayer%' and name > @dbName)

                end
            ";


            using (var connection = new SqlConnection($"server=.;database=master;trusted_connection=true;"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = dropAllSql;
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

    }
}
