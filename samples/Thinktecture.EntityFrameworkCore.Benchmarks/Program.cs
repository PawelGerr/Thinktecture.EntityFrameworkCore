using BenchmarkDotNet.Running;
using Thinktecture.Benchmarking;

// BenchmarkRunner.Run<CreateTempTable>();
// BenchmarkRunner.Run<BulkInsertIntoTempTable>();
// BenchmarkRunner.Run<ScalarCollectionParameter>();
// BenchmarkRunner.Run<ComplexCollectionParameter>();
BenchmarkRunner.Run<ReferenceEqualityValueComparer>();
