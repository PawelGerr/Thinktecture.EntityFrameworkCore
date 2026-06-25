namespace Thinktecture.Extensions.NpgsqlAggregateDbFunctionsExtensionsTests;

// ReSharper disable once InconsistentNaming
public class InMemoryGuard
{
   [Fact]
   public void Should_throw_when_BitOr_executed_in_memory()
   {
      var act = () => EF.Functions.BitOr(new[] { 1, 2 });
      act.Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_throw_when_BitAnd_executed_in_memory()
   {
      var act = () => EF.Functions.BitAnd(new[] { 1, 2 });
      act.Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_throw_when_BitXor_executed_in_memory()
   {
      var act = () => EF.Functions.BitXor(new[] { 1, 2 });
      act.Should().Throw<InvalidOperationException>();
   }
}
