using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.Database;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.Benchmarking;

[MemoryDiagnoser]
public class BulkInsertIntoTempTable : IDisposable
{
   private BenchmarkContext? _benchmarkContext;
   private IServiceScope? _scope;
   private SqliteBenchmarkDbContext? _sqliteDbContext;
   private SqlServerBenchmarkDbContext? _sqlServerDbContext;

   private readonly SqliteTempTableBulkInsertOptions _sqliteOptions = new();
   private readonly SqlServerTempTableBulkInsertOptions _sqlServerOptions = new();
   private readonly TempTable<int>[] _entities = Enumerable.Range(1, 100).Select(i => new TempTable<int>(i)).ToArray();

   [GlobalSetup]
   public void Initialize()
   {
      _benchmarkContext = new BenchmarkContext();
      _scope = _benchmarkContext.RootServiceProvider.CreateScope();
      _sqliteDbContext = _scope.ServiceProvider.GetRequiredService<SqliteBenchmarkDbContext>();
      _sqlServerDbContext = _scope.ServiceProvider.GetRequiredService<SqlServerBenchmarkDbContext>();
   }

   [GlobalCleanup]
   public void Dispose()
   {
      _scope?.Dispose();
      _benchmarkContext?.Dispose();
   }

   [Benchmark]
   public async Task Sqlite_1_column()
   {
      await using var tempTable = await _sqliteDbContext!.BulkInsertIntoTempTableAsync(_entities, _sqliteOptions);
   }

   [Benchmark]
   public async Task SqlServer_1_column()
   {
      await using var tempTable = await _sqlServerDbContext!.BulkInsertIntoTempTableAsync(_entities, _sqlServerOptions);
   }
}
