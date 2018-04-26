using JSCloud.LogPlayer.Store;
using JSCloud.LogPlayer.Types;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer.TestHarness
{
    class Program
    {

        private static string uid = DateTime.UtcNow.Ticks.ToString();

        private static IStore<int> sqlStore;
        private static IStore<int> inMemoryStore;
        private static IStore<int> inMemoryStoreWithSqlBase;

        static void Main(string[] args)
        {
            startup();

            sqlStore = new MicrosoftSqlStore<int>($"server=.;database=LogPlayer_{uid};trusted_connection=true;", $"dbo", $"TBL_{uid}");
            inMemoryStore = new InMemoryStore<int>(null);
            inMemoryStoreWithSqlBase = new InMemoryStore<int>(sqlStore);
                        
            writeSqlStoreStats(false); //Run it once to warm it up
            writeSqlStoreStats(true);

            writeInMemoryStoreNoBaseStats(false); //Run it once to warm it up
            writeInMemoryStoreNoBaseStats(true);

            writeInMemoryStoreSqlBaseStats(false); //Run it once to warm it up
            writeInMemoryStoreSqlBaseStats(true);


            shutdown();

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }


        private static void writeSqlStoreStats(bool outputStats)
        {
        
            sqlStore.Provision().GetAwaiter().GetResult();
            if (outputStats)
            {
                Console.WriteLine("### SQL Store");
            }
            writeStats(sqlStore, outputStats);
       
        }

        private static void writeInMemoryStoreNoBaseStats(bool outputStats)
        {
            inMemoryStore.Provision().GetAwaiter().GetResult();
            if (outputStats)
            {
                Console.WriteLine("### InMemoryStore - No Base Store");
            }
            writeStats(inMemoryStore, outputStats);
        }

        private static void writeInMemoryStoreSqlBaseStats(bool outputStats)
        {
          
            inMemoryStoreWithSqlBase.Provision().GetAwaiter().GetResult();
            if (outputStats)
            {
                Console.WriteLine("### InMemoryStore - With SQL Base Store");
            }
            writeStats(inMemoryStoreWithSqlBase, outputStats);
        
        }

        private static void writeStats(IStore<int> store, bool outputStats)
        {
            if (outputStats)
            {
                Console.WriteLine("| Test | Execution Time |");
                Console.WriteLine("| ------------ | ------------ |");
            }


            var items = new List<ChangeLog<int>>();
            for (int i = 0; i < 1000; i++)
            {
                items.Add(new ChangeLog<int>()
                {
                    FullTypeName = typeof(SimpleItem).FullName,
                    PropertySystemType = "int",
                    Value = i.ToString(),
                    ObjectId = i,
                    Property = "IntegerStandard"
                });
            }

            Stopwatch timer = new Stopwatch();

            timer.Start();
            store.StoreAsync(items).GetAwaiter().GetResult();
            timer.Stop();
            if (outputStats)
                Console.WriteLine($"| Inserting {items.Count} into store | {timer.ElapsedMilliseconds}ms |");

            items[0].ChangeLogId = null;
            timer.Restart();
            store.StoreAsync(items[0]).GetAwaiter().GetResult();
            if (outputStats)
                Console.WriteLine($"| Inserting a single into store | {timer.ElapsedMilliseconds}ms |");

            timer.Restart();
            store.GetChangesAsync(null, items[0].FullTypeName).GetAwaiter().GetResult();
            if(outputStats)
                Console.WriteLine($"| Getting all for type | {timer.ElapsedMilliseconds}ms |");

            timer.Restart();
            store.GetChangesAsync(items[0].ObjectId, items[0].FullTypeName).GetAwaiter().GetResult();
            if (outputStats)
                Console.WriteLine($"| Getting single item | {timer.ElapsedMilliseconds}ms |");

        }

        private static void startup()
        {
            using (var connection = new SqlConnection($"server=.;database=master;trusted_connection=true;"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"create database LogPlayer_{uid}";
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        private static void shutdown()
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
