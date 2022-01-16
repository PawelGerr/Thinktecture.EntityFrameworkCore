using BenchmarkDotNet.Running;
using Thinktecture.Benchmarking;

// BenchmarkRunner.Run<CreateTempTable>();
// BenchmarkRunner.Run<BulkInsertIntoTempTable>();
// BenchmarkRunner.Run<ScalarCollectionParameter>();
BenchmarkRunner.Run<ComplexCollectionParameter>();

// await ExecuteCreateTableAsync();
// await ExecuteBulkInsertIntoTempTableAsync();

async Task ExecuteCreateTableAsync()
{
   var benchmark = new CreateTempTable();

   benchmark.Initialize();
   await benchmark.Sqlite_1_column();
   await benchmark.SqlServer_1_column();

   await Execute(benchmark);

   async Task Execute(CreateTempTable createTempTable)
   {
      for (var i = 0; i < 1000; i++)
      {
         await createTempTable.Sqlite_1_column();
         await createTempTable.SqlServer_1_column();
      }
   }
}

async Task ExecuteBulkInsertIntoTempTableAsync()
{
   var benchmark = new BulkInsertIntoTempTable();

   benchmark.Initialize();
   await benchmark.Sqlite_1_column();
   await benchmark.SqlServer_1_column();

   await Execute(benchmark);

   async Task Execute(BulkInsertIntoTempTable bulkInsertIntoTempTable)
   {
      for (var i = 0; i < 1000; i++)
      {
         await bulkInsertIntoTempTable.Sqlite_1_column();
         await bulkInsertIntoTempTable.SqlServer_1_column();
      }
   }
}
