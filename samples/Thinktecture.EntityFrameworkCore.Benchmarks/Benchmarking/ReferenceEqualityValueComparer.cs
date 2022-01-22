using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.Database;

namespace Thinktecture.Benchmarking;

[MemoryDiagnoser]
public class ReferenceEqualityValueComparer : IDisposable
{
   private BenchmarkContext? _benchmarkContext;
   private IServiceScope? _scope;
   private SqlServerBenchmarkDbContext? _sqlServerDbContext;

   private const int _BYTES_LENGTH = 1024;

   private int _counter;
   private readonly byte[] _bytesBestCase = new byte[_BYTES_LENGTH];
   private readonly byte[] _bytesWorstCase = new byte[_BYTES_LENGTH];

   private List<EntityWithByteArray> _entitiesWithDefaultComparer = null!;
   private List<EntityWithByteArrayAndValueComparer> _entitiesWithCustomComparer = null!;

   [GlobalSetup]
   public void Initialize()
   {
      _benchmarkContext = new BenchmarkContext();
      _scope = _benchmarkContext.RootServiceProvider.CreateScope();
      _sqlServerDbContext = _scope.ServiceProvider.GetRequiredService<SqlServerBenchmarkDbContext>();

      _sqlServerDbContext.Database.EnsureDeleted();
      _sqlServerDbContext.Database.EnsureCreated();

      _sqlServerDbContext.EntitiesWithByteArray.BulkDelete();
      _sqlServerDbContext.EntitiesWithByteArrayAndValueComparer.BulkDelete();

      var bytes = new byte[_BYTES_LENGTH];

      for (var i = 0; i < 10_000; i++)
      {
         var id = new Guid($"66AFED1B-92EA-4483-BF4F-{i.ToString("X").PadLeft(12, '0')}");

         _sqlServerDbContext.EntitiesWithByteArray.Add(new EntityWithByteArray(id, bytes));
         _sqlServerDbContext.EntitiesWithByteArrayAndValueComparer.Add(new EntityWithByteArrayAndValueComparer(id, bytes));
      }

      _sqlServerDbContext.SaveChanges();
      _sqlServerDbContext.ChangeTracker.Clear();
   }

   [GlobalCleanup]
   public void Dispose()
   {
      _scope?.Dispose();
      _benchmarkContext?.Dispose();
   }

   [IterationSetup]
   public void IterationSetup()
   {
      _sqlServerDbContext!.ChangeTracker.Clear();
      _entitiesWithDefaultComparer = _sqlServerDbContext.EntitiesWithByteArray.ToList();
      _entitiesWithCustomComparer = _sqlServerDbContext.EntitiesWithByteArrayAndValueComparer.ToList();

      _bytesBestCase[0] = _bytesWorstCase[^1] = (byte)(++_counter % Byte.MaxValue);
   }

   [Benchmark]
   public async Task Default_BestCase()
   {
      _entitiesWithDefaultComparer.ForEach(e => e.Bytes = _bytesBestCase);

      await _sqlServerDbContext!.SaveChangesAsync();
   }

   [Benchmark]
   public async Task Default_WorstCase()
   {
      _entitiesWithDefaultComparer.ForEach(e => e.Bytes = _bytesWorstCase);

      await _sqlServerDbContext!.SaveChangesAsync();
   }

   [Benchmark]
   public async Task ReferenceEquality_BestCase()
   {
      _entitiesWithCustomComparer.ForEach(e => e.Bytes = _bytesBestCase);

      await _sqlServerDbContext!.SaveChangesAsync();
   }

   [Benchmark]
   public async Task ReferenceEquality_WorstCase()
   {
      _entitiesWithCustomComparer.ForEach(e => e.Bytes = _bytesWorstCase);

      await _sqlServerDbContext!.SaveChangesAsync();
   }
}
