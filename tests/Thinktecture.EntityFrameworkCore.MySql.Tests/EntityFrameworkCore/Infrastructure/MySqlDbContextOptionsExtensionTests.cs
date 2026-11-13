using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure.MySqlDbContextOptionsExtensionTests;

public sealed class MySqlDbContextOptionsExtensionTests
{
   [Fact]
   public void Should_enable_the_custom_factories_through_window_functions()
   {
      var extension = CreateExtension(builder => builder.AddWindowFunctionsSupport());

      extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.Should().BeTrue();
      extension.AddCustomQuerySqlGeneratorFactory.Should().BeTrue();
      extension.AddCustomRelationalParameterBasedSqlProcessorFactory.Should().BeTrue();
   }

   [Fact]
   public void Should_enable_only_the_queryable_factory_through_collection_parameters()
   {
      var extension = CreateExtension(builder => builder.AddCollectionParameterSupport());

      extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.Should().BeTrue();
      extension.AddCustomQuerySqlGeneratorFactory.Should().BeFalse();
      extension.AddCustomRelationalParameterBasedSqlProcessorFactory.Should().BeFalse();
   }

   [Fact]
   public void Should_enable_the_queryable_factory_explicitly()
   {
      var extension = CreateExtension(builder => builder.AddCustomQueryableMethodTranslatingExpressionVisitorFactory());

      extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.Should().BeTrue();
      extension.AddCustomQuerySqlGeneratorFactory.Should().BeFalse();
   }

   [Fact]
   public void Should_not_share_a_service_provider_between_different_feature_sets()
   {
      var withBulk = CreateExtension(builder => builder.AddBulkOperationSupport());
      var withWindow = CreateExtension(builder => builder.AddWindowFunctionsSupport());

      withBulk.Info.ShouldUseSameServiceProvider(withWindow.Info).Should().BeFalse();
      withBulk.Info.GetServiceProviderHashCode().Should().NotBe(withWindow.Info.GetServiceProviderHashCode());
   }

   [Fact]
   public void Should_share_a_service_provider_between_equal_feature_sets()
   {
      var first = CreateExtension(builder => builder.AddBulkOperationSupport().AddWindowFunctionsSupport());
      var second = CreateExtension(builder => builder.AddBulkOperationSupport().AddWindowFunctionsSupport());

      first.Info.ShouldUseSameServiceProvider(second.Info).Should().BeTrue();
      first.Info.GetServiceProviderHashCode().Should().Be(second.Info.GetServiceProviderHashCode());
   }

   [Fact]
   public void Should_report_the_features_in_the_debug_info()
   {
      var extension = CreateExtension(builder => builder.AddBulkOperationSupport().AddWindowFunctionsSupport());
      var debugInfo = new Dictionary<string, string>();

      extension.Info.PopulateDebugInfo(debugInfo);

      debugInfo["Thinktecture:MySqlBulkOperationSupport"].Should().Be("True");
      debugInfo["Thinktecture:MySqlWindowFunctionsSupport"].Should().Be("True");
      debugInfo["Thinktecture:MySqlCollectionParameterSupport"].Should().Be("False");
      debugInfo["Thinktecture:MySqlCustomQuerySqlGeneratorFactory"].Should().Be("True");
   }

   [Fact]
   public void Should_report_the_features_in_the_log_fragment()
   {
      var extension = CreateExtension(builder => builder.AddBulkOperationSupport().AddCollectionParameterSupport());

      extension.Info.LogFragment.Should().Contain("BulkOperationSupport").And.Contain("CollectionParameterSupport").And.NotContain("WindowFunctionsSupport");
   }

   private static MySqlDbContextOptionsExtension CreateExtension(Action<MySqlDbContextOptionsBuilder> configure)
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, configure)
                    .Options;

      return options.FindExtension<MySqlDbContextOptionsExtension>() ?? throw new InvalidOperationException("The MySQL extension is missing.");
   }
}
