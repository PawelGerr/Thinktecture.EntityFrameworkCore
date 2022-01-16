using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.Database;
using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.Benchmarking;

[MemoryDiagnoser]
public class ComplexCollectionParameter : IDisposable
{
   private BenchmarkContext? _benchmarkContext;
   private IServiceScope? _scope;
   private SqlServerBenchmarkDbContext? _sqlServerDbContext;
   private readonly SqlServerTempTableBulkInsertOptions _sqlServerOptions = new();

   private readonly MyParameter[] _objects = Enumerable.Range(1, 500).Select(i => new MyParameter(Guid.NewGuid(), i.ToString())).ToArray();

   [GlobalSetup]
   public void Initialize()
   {
      _benchmarkContext = new BenchmarkContext();
      _scope = _benchmarkContext.RootServiceProvider.CreateScope();
      _sqlServerDbContext = _scope.ServiceProvider.GetRequiredService<SqlServerBenchmarkDbContext>();

      _sqlServerDbContext.CreateComplexCollectionParameter(_objects).Count();
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
      return _sqlServerDbContext!.CreateComplexCollectionParameter(_objects).Count();
   }

   [Benchmark]
   public async Task<int> TempTable()
   {
      await using var tempTable = await _sqlServerDbContext!.BulkInsertIntoTempTableAsync(_objects, _sqlServerOptions);

      return tempTable.Query.Count();
   }
}
