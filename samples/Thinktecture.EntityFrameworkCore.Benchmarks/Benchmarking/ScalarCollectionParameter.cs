using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.Database;
using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.Benchmarking;

[MemoryDiagnoser]
public class ScalarCollectionParameter : IDisposable
{
   private BenchmarkContext? _benchmarkContext;
   private IServiceScope? _scope;
   private SqlServerBenchmarkDbContext? _sqlServerDbContext;
   private readonly SqlServerTempTableBulkInsertOptions _sqlServerOptions = new();

   private readonly Guid[] _values = Enumerable.Range(1, 500).Select(i => Guid.NewGuid()).ToArray();

   [GlobalSetup]
   public void Initialize()
   {
      _benchmarkContext = new BenchmarkContext();
      _scope = _benchmarkContext.RootServiceProvider.CreateScope();
      _sqlServerDbContext = _scope.ServiceProvider.GetRequiredService<SqlServerBenchmarkDbContext>();

      _sqlServerDbContext.CreateScalarCollectionParameter(_values).Count();
      TempTable().Wait();
   }

   [GlobalCleanup]
   public void Dispose()
   {
      _scope?.Dispose();
      _benchmarkContext?.Dispose();
   }

   [Benchmark]
   public int Json()
   {
      return _sqlServerDbContext!.CreateScalarCollectionParameter(_values).Count();
   }

   [Benchmark]
   public async Task<int> TempTable()
   {
      await using var tempTable = await _sqlServerDbContext!.BulkInsertValuesIntoTempTableAsync(_values, _sqlServerOptions);

      return tempTable.Query.Count();
   }
}
