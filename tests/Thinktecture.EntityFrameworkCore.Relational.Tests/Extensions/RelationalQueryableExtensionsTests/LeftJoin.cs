using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.RelationalQueryableExtensionsTests;

public class LeftJoin : IntegrationTestsBase
{
   public LeftJoin(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public void Should_generate_same_sql_with_and_without_result_selector()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var expectedSql = @"SELECT ""t"".""Id"", ""t"".""Column1"", ""t"".""Column2"", ""t0"".""Id"", ""t0"".""Column1"", ""t0"".""Column2""" + Environment.NewLine +
                        @"FROM ""TestEntities"" AS ""t""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t0"" ON ""t"".""Column1"" = ""t0"".""Column1""";

      IQueryable<LeftJoinResult<TestEntity, TestEntity?>> result = ActDbContext.TestEntities
                                                                               .LeftJoin(ActDbContext.TestEntities, e => e.Column1, e => e.Column1);
      result.ToQueryString().Should().Be(expectedSql);

      ActDbContext.TestEntities
                  .LeftJoin(ActDbContext.TestEntities,
                            e => e.Column1, e => e.Column1, r => new { r.Left, r.Right })
                  .ToQueryString().Should().Be(expectedSql);
   }

   [Fact]
   public void Should_cascade_left_join_the_2nd_time()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var expectedSql = @"SELECT ""t"".""Id"", ""t"".""Column1"", ""t"".""Column2"", ""t0"".""Id"", ""t0"".""Column1"", ""t0"".""Column2""" +
                        @", ""t1"".""Id"", ""t1"".""Column1"", ""t1"".""Column2""" +
                        Environment.NewLine +
                        @"FROM ""TestEntities"" AS ""t""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t0"" ON ""t"".""Column1"" = ""t0"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t1"" ON ""t"".""Column1"" = ""t1"".""Column1""";

      IQueryable<LeftJoinResult<TestEntity, TestEntity?, TestEntity?>> result = ActDbContext.TestEntities
                                                                                            .LeftJoin(ActDbContext.TestEntities, e => e.Column1, e => e.Column1)
                                                                                            .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1);
      result.ToQueryString().Should().Be(expectedSql);
   }

   [Fact]
   public void Should_cascade_left_join_the_3rd_time()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var expectedSql = @"SELECT ""t"".""Id"", ""t"".""Column1"", ""t"".""Column2"", ""t0"".""Id"", ""t0"".""Column1"", ""t0"".""Column2""" +
                        @", ""t1"".""Id"", ""t1"".""Column1"", ""t1"".""Column2""" +
                        @", ""t2"".""Id"", ""t2"".""Column1"", ""t2"".""Column2""" +
                        Environment.NewLine +
                        @"FROM ""TestEntities"" AS ""t""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t0"" ON ""t"".""Column1"" = ""t0"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t1"" ON ""t"".""Column1"" = ""t1"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t2"" ON ""t"".""Column1"" = ""t2"".""Column1""";

      IQueryable<LeftJoinResult<TestEntity, TestEntity?, TestEntity?, TestEntity?>> result = ActDbContext.TestEntities
                                                                                                         .LeftJoin(ActDbContext.TestEntities, e => e.Column1, e => e.Column1)
                                                                                                         .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1)
                                                                                                         .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1);
      result.ToQueryString().Should().Be(expectedSql);
   }

   [Fact]
   public void Should_cascade_left_join_the_4th_time()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var expectedSql = @"SELECT ""t"".""Id"", ""t"".""Column1"", ""t"".""Column2"", ""t0"".""Id"", ""t0"".""Column1"", ""t0"".""Column2""" +
                        @", ""t1"".""Id"", ""t1"".""Column1"", ""t1"".""Column2""" +
                        @", ""t2"".""Id"", ""t2"".""Column1"", ""t2"".""Column2""" +
                        @", ""t3"".""Id"", ""t3"".""Column1"", ""t3"".""Column2""" +
                        Environment.NewLine +
                        @"FROM ""TestEntities"" AS ""t""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t0"" ON ""t"".""Column1"" = ""t0"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t1"" ON ""t"".""Column1"" = ""t1"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t2"" ON ""t"".""Column1"" = ""t2"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t3"" ON ""t"".""Column1"" = ""t3"".""Column1""";

      IQueryable<LeftJoinResult<TestEntity, TestEntity?, TestEntity?, TestEntity?, TestEntity?>> result = ActDbContext.TestEntities
                                                                                                                      .LeftJoin(ActDbContext.TestEntities, e => e.Column1, e => e.Column1)
                                                                                                                      .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1)
                                                                                                                      .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1)
                                                                                                                      .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1);
      result.ToQueryString().Should().Be(expectedSql);
   }

   [Fact]
   public void Should_cascade_left_join_the_5th_time()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var expectedSql = @"SELECT ""t"".""Id"", ""t"".""Column1"", ""t"".""Column2"", ""t0"".""Id"", ""t0"".""Column1"", ""t0"".""Column2""" +
                        @", ""t1"".""Id"", ""t1"".""Column1"", ""t1"".""Column2""" +
                        @", ""t2"".""Id"", ""t2"".""Column1"", ""t2"".""Column2""" +
                        @", ""t3"".""Id"", ""t3"".""Column1"", ""t3"".""Column2""" +
                        @", ""t4"".""Id"", ""t4"".""Column1"", ""t4"".""Column2""" +
                        Environment.NewLine +
                        @"FROM ""TestEntities"" AS ""t""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t0"" ON ""t"".""Column1"" = ""t0"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t1"" ON ""t"".""Column1"" = ""t1"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t2"" ON ""t"".""Column1"" = ""t2"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t3"" ON ""t"".""Column1"" = ""t3"".""Column1""" + Environment.NewLine +
                        @"LEFT JOIN ""TestEntities"" AS ""t4"" ON ""t"".""Column1"" = ""t4"".""Column1""";

      IQueryable<LeftJoinResult<TestEntity, TestEntity?, TestEntity?, TestEntity?, TestEntity?, TestEntity?>> result = ActDbContext.TestEntities
                                                                                                                                   .LeftJoin(ActDbContext.TestEntities, e => e.Column1, e => e.Column1)
                                                                                                                                   .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1)
                                                                                                                                   .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1)
                                                                                                                                   .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1)
                                                                                                                                   .LeftJoin(ActDbContext.TestEntities, e => e.Left.Column1, e => e.Column1);
      result.ToQueryString().Should().Be(expectedSql);
   }
}
