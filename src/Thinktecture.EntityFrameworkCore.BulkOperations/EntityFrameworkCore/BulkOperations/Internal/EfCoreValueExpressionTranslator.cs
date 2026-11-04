using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// This is an internal API. It may be changed or removed without notice.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class EfCoreValueExpressionTranslator
{
   private readonly DbContext _ctx;

   /// <summary>
   /// Initializes a new instance.
   /// </summary>
   /// <param name="ctx">The database context.</param>
   public EfCoreValueExpressionTranslator(DbContext ctx)
   {
      _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
   }

   /// <summary>
   /// Translates a bulk update from query into SQL using EF Core's expression translation pipeline.
   /// </summary>
   public (string Sql, IReadOnlyDictionary<string, object?> Parameters) TranslateUpdateFromQuery<TTarget, TSource, TResult>(
      IQueryable<TSource> sourceQuery,
      Expression<Func<TTarget, TResult?>> targetKeySelector,
      Expression<Func<TSource, TResult?>> sourceKeySelector,
      IReadOnlyList<ISetPropertyEntry> setClauseEntries,
      LambdaExpression? filter,
      IBulkOperationTableNameProvider tableNameProvider)
      where TTarget : class
      where TSource : class
   {
      var targetEntityType = _ctx.Model.GetEntityType(typeof(TTarget));
      var targetStoreObject = StoreObjectIdentifier.Create(targetEntityType, StoreObjectType.Table)
                              ?? throw new InvalidOperationException($"Could not create StoreObjectIdentifier for table '{targetEntityType.Name}'.");

      // Step 1: Build the join query
      var joinQuery = _ctx.Set<TTarget>()
                          .Join(sourceQuery,
                                targetKeySelector,
                                sourceKeySelector,
                                (t, s) => new { Target = t, Source = s });

      // Step 2: Extract parameters and compile through EF Core pipeline
      var parameterValues = new Dictionary<string, object?>();
      var (shapedQuery, compilationContext, methodTranslator) = CompileQuery(joinQuery.Expression, parameterValues);

      var selectExpression = (SelectExpression)shapedQuery.QueryExpression;

      // Step 3: Extract entity shapers
      var (targetShaper, sourceShaper) = ExtractEntityShapers<TTarget, TSource>(shapedQuery.ShaperExpression);

      // Step 4: Create SQL translator
      var sqlTranslatorFactory = _ctx.GetService<IRelationalSqlTranslatingExpressionVisitorFactory>();
      var sqlTranslator = sqlTranslatorFactory.Create(compilationContext, methodTranslator);
      var sqlExprFactory = _ctx.GetService<ISqlExpressionFactory>();

      // Step 5: Translate SET entries
      var setters = new List<ColumnValueSetter>();
      var targetTableAlias = GetTargetTableAlias(selectExpression);
      var targetProperties = new HashSet<IProperty>();

      foreach (var entry in BulkUpdateFromQueryHelper.ExpandComplexProperties(targetEntityType, setClauseEntries))
      {
         // Resolve target column
         var targetAccess = BulkUpdateFromQueryHelper.ExtractPropertyAccess(entry.TargetPropertySelector);
         var targetProp = BulkUpdateFromQueryHelper.ResolveProperty(targetEntityType, targetAccess);
         var targetColumnName = targetProp.GetColumnName(targetStoreObject)
                                ?? throw new InvalidOperationException($"Could not get column name for property '{targetProp.Name}' on entity type '{targetEntityType.Name}'.");

         if (!targetProperties.Add(targetProp))
            throw new ArgumentException($"The target column '{targetColumnName}' is set more than once. Expression: {entry.TargetPropertySelector}");

         var targetColumn = CreateColumnExpression(targetProp, targetColumnName, targetTableAlias);

         // Translate value expression
         var valueExpr = entry.ValueSelector;

         if (valueExpr.Parameters.Count != 2)
            throw new ArgumentException($"Update value expression must have exactly 2 parameters. Expression: {valueExpr}");

         var remapped = RemapTwoParameterLambda(valueExpr, targetShaper, sourceShaper);
         var flattenedRemapped = FlattenClosures(remapped);

         var sqlExpr = TranslateValueExpression(valueExpr, flattenedRemapped, sqlTranslator, sqlExprFactory, targetProp.GetRelationalTypeMapping());

         setters.Add(new ColumnValueSetter(targetColumn, sqlExpr));
      }

      // Step 6: Translate filter
      if (filter is not null)
      {
         if (filter.Parameters.Count != 2)
            throw new ArgumentException($"Filter expression must have exactly 2 parameters. Expression: {filter}");

         var remappedFilter = RemapTwoParameterLambda(filter, targetShaper, sourceShaper);
         var flattenedFilter = FlattenClosures(remappedFilter);

         var sqlFilter = sqlTranslator.TranslateProjection(flattenedFilter)
                         ?? throw new InvalidOperationException($"Could not translate filter expression: {filter}");

         // A filter that is false on the client becomes a bare constant, which SQL Server does not accept as a condition.
         // ApplyPredicate ignores a constant true.
         if (sqlFilter is SqlConstantExpression { Value: false })
            selectExpression.ApplyPredicate(sqlExprFactory.Equal(sqlExprFactory.Constant(1), sqlExprFactory.Constant(0)));
         else if (sqlFilter is SqlExpression filterExpr)
            selectExpression.ApplyPredicate(filterExpr);
      }

      // Step 7: Clear projections and build UpdateExpression
      // VisitUpdate requires Projection: [] on the SelectExpression.
      selectExpression.ReplaceProjection(new List<Expression>());
      selectExpression.ApplyProjection();

      // The target TableExpression is Tables[0] in the SelectExpression.
      var targetTable = selectExpression.Tables[0];

      if (targetTable is not TableExpression targetTableExpr)
         throw new InvalidOperationException($"Expected TableExpression as first table but got {targetTable.GetType().Name}.");

      var updateExpression = new UpdateExpression(targetTableExpr, selectExpression, setters);

      // Step 8: Render SQL
      var sqlGeneratorFactory = _ctx.GetService<IQuerySqlGeneratorFactory>();
      var command = sqlGeneratorFactory.Create().GetCommand(updateExpression);

      var sql = command.CommandText;

      // Step 9: Apply the table name of the provider via string replacement
      sql = ApplyTableName(sql, tableNameProvider.GetValidTableName(targetEntityType), targetEntityType);

      return (sql, parameterValues);
   }

   private string ApplyTableName(
      string sql,
      BulkOperationTableName tableName,
      IEntityType targetEntityType)
   {
      var originalName = targetEntityType.GetTableName()!;
      var originalSchema = targetEntityType.GetSchema();

      if (tableName.Name == originalName && tableName.Schema == originalSchema)
         return sql;

      var sqlGenerationHelper = _ctx.GetService<ISqlGenerationHelper>();
      var originalQualified = sqlGenerationHelper.DelimitIdentifier(originalName, originalSchema);
      var overriddenQualified = sqlGenerationHelper.DelimitIdentifier(tableName.Name, tableName.Schema);

      var updatedSql = sql.Replace(originalQualified, overriddenQualified);

      if (ReferenceEquals(sql, updatedSql))
         throw new InvalidOperationException($"The table name from 'TableNameProvider' could not be applied, because the table '{originalQualified}' was not found in the generated UPDATE SQL.");

      return updatedSql;
   }

   /// <summary>
   /// Translates a bulk insert from query into SQL using EF Core's expression translation pipeline.
   /// </summary>
   public (string Sql, IReadOnlyDictionary<string, object?> Parameters) TranslateInsertFromQuery<TTarget, TSource>(
      IQueryable<TSource> sourceQuery,
      IReadOnlyList<ISetPropertyEntry> entries,
      IBulkOperationTableNameProvider tableNameProvider)
      where TTarget : class
      where TSource : class
   {
      var targetEntityType = _ctx.Model.GetEntityType(typeof(TTarget));
      var targetStoreObject = StoreObjectIdentifier.Create(targetEntityType, StoreObjectType.Table)
                              ?? throw new InvalidOperationException($"Could not create StoreObjectIdentifier for table '{targetEntityType.Name}'.");

      // Step 1: Compile source query through EF Core pipeline
      var parameterValues = new Dictionary<string, object?>();
      var (shapedQuery, compilationContext, methodTranslator) = CompileQuery(sourceQuery.Expression, parameterValues);

      var selectExpression = (SelectExpression)shapedQuery.QueryExpression;

      // Step 2: Extract source entity shaper
      var sourceShaper = ExtractSingleEntityShaper<TSource>(shapedQuery.ShaperExpression);

      // Step 3: Create SQL translator
      var sqlTranslatorFactory = _ctx.GetService<IRelationalSqlTranslatingExpressionVisitorFactory>();
      var sqlTranslator = sqlTranslatorFactory.Create(compilationContext, methodTranslator);
      var sqlExprFactory = _ctx.GetService<ISqlExpressionFactory>();

      // Step 4: Build target column list and translate value expressions
      var sqlGenerationHelper = _ctx.GetService<ISqlGenerationHelper>();
      var targetColumns = new List<string>();
      var valueExpressions = new List<SqlExpression>();
      var targetProperties = new HashSet<IProperty>();

      foreach (var entry in BulkUpdateFromQueryHelper.ExpandComplexProperties(targetEntityType, entries))
      {
         // Resolve target column name
         var targetAccess = BulkUpdateFromQueryHelper.ExtractPropertyAccess(entry.TargetPropertySelector);
         var targetProp = BulkUpdateFromQueryHelper.ResolveProperty(targetEntityType, targetAccess);
         var targetColumnName = targetProp.GetColumnName(targetStoreObject)
                                ?? throw new InvalidOperationException($"Could not get column name for property '{targetProp.Name}' on entity type '{targetEntityType.Name}'.");

         if (!targetProperties.Add(targetProp))
            throw new ArgumentException($"The target column '{targetColumnName}' is mapped more than once. Expression: {entry.TargetPropertySelector}");

         targetColumns.Add(sqlGenerationHelper.DelimitIdentifier(targetColumnName));

         // Translate value expression (1 parameter: source)
         var valueExpr = entry.ValueSelector;

         if (valueExpr.Parameters.Count != 1)
            throw new ArgumentException($"Insert value expression must have exactly 1 parameter. Expression: {valueExpr}");

         var remapped = RemapSingleParameterLambda(valueExpr, sourceShaper);
         var flattenedRemapped = FlattenClosures(remapped);

         var sqlExpr = TranslateValueExpression(valueExpr, flattenedRemapped, sqlTranslator, sqlExprFactory, targetProp.GetRelationalTypeMapping());

         valueExpressions.Add(sqlExpr);
      }

      // Step 5: Convert SqlConstantExpressions to SqlParameterExpressions with unique names
      // to prevent SelectExpression.ApplyProjection() from deduplicating identical constants
      // (e.g., two columns both mapped to 0 would otherwise collapse into a single projection).
      var projectionExpressions = new List<SqlExpression>(valueExpressions.Count);
      var constParamIndex = 0;

      foreach (var valExpr in valueExpressions)
      {
         if (valExpr is SqlConstantExpression constExpr)
         {
            var paramName = $"__insert_const_{constParamIndex++}";
            // The parameter value is bound as-is by the executors, so the value converter (if any) must be applied here.
            var converter = constExpr.TypeMapping?.Converter;
            parameterValues[paramName] = converter is null ? constExpr.Value : converter.ConvertToProvider(constExpr.Value);
            projectionExpressions.Add(new SqlParameterExpression(paramName, valExpr.Type, constExpr.TypeMapping));
         }
         else
         {
            projectionExpressions.Add(valExpr);
         }
      }

      // Step 6: Replace projections in SelectExpression with our value expressions
      selectExpression.ReplaceProjection(projectionExpressions.Cast<Expression>().ToList());
      selectExpression.ApplyProjection();

      // Step 7: Render SELECT SQL
      var sqlGeneratorFactory = _ctx.GetService<IQuerySqlGeneratorFactory>();
      var selectCommand = sqlGeneratorFactory.Create().GetCommand(selectExpression);

      // Step 8: Build INSERT INTO ... SELECT ...
      var tableName = tableNameProvider.GetValidTableName(targetEntityType);

      var insertSql = $"INSERT INTO {sqlGenerationHelper.DelimitIdentifier(tableName.Name, tableName.Schema)} ({string.Join(", ", targetColumns)})\n{selectCommand.CommandText}";

      return (insertSql, parameterValues);
   }

   private (ShapedQueryExpression ShapedQuery, QueryCompilationContext CompilationContext, QueryableMethodTranslatingExpressionVisitor MethodTranslator)
      CompileQuery(Expression queryExpression, Dictionary<string, object?> parameterValues)
   {
      // Extract parameters using EF Core's ExpressionTreeFuncletizer (via QueryCompiler)
      var queryCompiler = _ctx.GetService<IQueryCompiler>();
      var logger = _ctx.GetService<IDiagnosticsLogger<DbLoggerCategory.Query>>();

      if (queryCompiler is not QueryCompiler concreteCompiler)
         throw new InvalidOperationException($"Expected QueryCompiler but got {queryCompiler.GetType().Name}.");

      var extractedQuery = concreteCompiler.ExtractParameters(queryExpression, parameterValues, logger);

      // Compile through pipeline: preprocess → translate
      var compilationContextFactory = _ctx.GetService<IQueryCompilationContextFactory>();
      var compilationContext = compilationContextFactory.Create(false);

      var preprocessorFactory = _ctx.GetService<IQueryTranslationPreprocessorFactory>();
      var preprocessed = preprocessorFactory.Create(compilationContext).Process(extractedQuery);

      var methodTranslatorFactory = _ctx.GetService<IQueryableMethodTranslatingExpressionVisitorFactory>();
      var methodTranslator = methodTranslatorFactory.Create(compilationContext);
      var translated = methodTranslator.Translate(preprocessed);

      if (translated is not ShapedQueryExpression shapedQuery)
         throw new InvalidOperationException($"Expected ShapedQueryExpression but got {translated?.GetType().Name ?? "null"}");

      return (shapedQuery, compilationContext, methodTranslator);
   }

   private static (Expression TargetShaper, Expression SourceShaper) ExtractEntityShapers<TTarget, TSource>(Expression shaperExpression)
   {
      var shapers = CollectStructuralTypeShapers(shaperExpression);

      Expression? targetShaper = null;
      Expression? sourceShaper = null;

      foreach (var (shaper, clrType) in shapers)
      {
         if (typeof(TTarget).IsAssignableFrom(clrType) && targetShaper is null)
            targetShaper = shaper;
         else if (typeof(TSource).IsAssignableFrom(clrType) && sourceShaper is null)
            sourceShaper = shaper;
      }

      // If TTarget == TSource (self-join), assign based on order
      if (targetShaper is not null && sourceShaper is null && typeof(TTarget) == typeof(TSource) && shapers.Count >= 2)
         sourceShaper = shapers[1].Shaper;

      return (targetShaper ?? throw new InvalidOperationException($"Could not find entity shaper for target type '{typeof(TTarget).Name}'."),
              sourceShaper ?? throw new InvalidOperationException($"Could not find entity shaper for source type '{typeof(TSource).Name}'."));
   }

   private static Expression ExtractSingleEntityShaper<TSource>(Expression shaperExpression)
   {
      var shapers = CollectStructuralTypeShapers(shaperExpression);

      foreach (var (shaper, clrType) in shapers)
      {
         if (typeof(TSource).IsAssignableFrom(clrType))
            return shaper;
      }

      // For non-entity sources (anonymous types, projections), use the shaper expression directly
      if (typeof(TSource).IsAssignableFrom(shaperExpression.Type))
         return shaperExpression;

      throw new InvalidOperationException($"Could not find entity shaper for type '{typeof(TSource).Name}'.");
   }

   private static List<(Expression Shaper, Type ClrType)> CollectStructuralTypeShapers(Expression expression)
   {
      var results = new List<(Expression Shaper, Type ClrType)>();
      CollectStructuralTypeShapers(expression, results);

      return results;
   }

   private static void CollectStructuralTypeShapers(Expression expression, List<(Expression Shaper, Type ClrType)> results)
   {
      while (true)
      {
         switch (expression)
         {
            case StructuralTypeShaperExpression shaper:
               results.Add((shaper, shaper.Type));
               break;

            case NewExpression newExpr:
               foreach (var arg in newExpr.Arguments)
               {
                  CollectStructuralTypeShapers(arg, results);
               }

               // Also add the NewExpression itself so non-entity types
               // (e.g., anonymous type projections) can be matched by CLR type.
               results.Add((newExpr, newExpr.Type));
               break;

            case MemberInitExpression memberInit:
               CollectStructuralTypeShapers(memberInit.NewExpression, results);

               foreach (var binding in memberInit.Bindings)
               {
                  if (binding is not MemberAssignment assignment)
                     continue;

                  var countBefore = results.Count;
                  CollectStructuralTypeShapers(assignment.Expression, results);

                  // If no structural type shapers were found in this binding,
                  // collect the binding expression itself (e.g., anonymous type projection)
                  if (results.Count == countBefore)
                     results.Add((assignment.Expression, assignment.Expression.Type));
               }

               break;

            case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary:
               expression = unary.Operand;
               continue;

            case MemberExpression { Expression: not null } member:
               expression = member.Expression;
               continue;
         }

         break;
      }
   }

   private static Expression RemapTwoParameterLambda(LambdaExpression lambda, Expression targetShaper, Expression sourceShaper)
   {
      return ReplacingExpressionVisitor.Replace([lambda.Parameters[0], lambda.Parameters[1]],
                                                [targetShaper, sourceShaper],
                                                lambda.Body);
   }

   private static Expression RemapSingleParameterLambda(LambdaExpression lambda, Expression sourceShaper)
   {
      return ReplacingExpressionVisitor.Replace(lambda.Parameters[0], sourceShaper, lambda.Body);
   }

   private Expression FlattenClosures(Expression expression)
   {
      var finder = new EvaluatableSubtreeFinder(_ctx.GetService<IEvaluatableExpressionFilter>(), _ctx.Model);
      finder.Visit(expression);

      return new ClosureFlatteningVisitor(finder.EvaluatableNodes).Visit(expression);
   }

   private static SqlExpression TranslateValueExpression(
      LambdaExpression valueExpr,
      Expression flattenedValue,
      RelationalSqlTranslatingExpressionVisitor sqlTranslator,
      ISqlExpressionFactory sqlExprFactory,
      RelationalTypeMapping targetTypeMapping)
   {
      // Constants of types without a built-in type mapping (e.g. properties with a value converter)
      // cannot be translated by EF Core; create the constant with the target column's type mapping directly.
      if (flattenedValue is ConstantExpression constant)
         return sqlExprFactory.Constant(constant.Value, constant.Type, targetTypeMapping);

      var sqlValue = sqlTranslator.TranslateProjection(flattenedValue)
                     ?? throw new InvalidOperationException($"Could not translate value expression: {valueExpr}");

      if (sqlValue is not SqlExpression sqlExpr)
         throw new InvalidOperationException($"Translated expression is not a SqlExpression: {sqlValue.GetType().Name}");

      // Apply type mapping from target column
      return sqlExprFactory.ApplyTypeMapping(sqlExpr, targetTypeMapping);
   }

   private static string GetTargetTableAlias(SelectExpression selectExpression)
   {
      return selectExpression.Tables[0].GetRequiredAlias();
   }

   private static ColumnExpression CreateColumnExpression(
      IProperty property,
      string columnName,
      string tableAlias)
   {
      var typeMapping = property.GetRelationalTypeMapping();

      return new ColumnExpression(columnName,
                                  tableAlias,
                                  property.ClrType,
                                  typeMapping,
                                  property.IsNullable);
   }

   /// <summary>
   /// Pass 1: marks every node whose whole subtree can be evaluated on the client.
   /// </summary>
   private sealed class EvaluatableSubtreeFinder(
      IEvaluatableExpressionFilter evaluatableExpressionFilter,
      IModel model) : ExpressionVisitor
   {
      public HashSet<Expression> EvaluatableNodes { get; } = new(ReferenceEqualityComparer.Instance);

      private bool _childrenEvaluatable = true;

      [return: NotNullIfNotNull(nameof(node))]
      public override Expression? Visit(Expression? node)
      {
         if (node is null)
            return null;

         var parentChildrenEvaluatable = _childrenEvaluatable;
         _childrenEvaluatable = true;

         base.Visit(node);

         // Extension nodes are the entity shapers that replace the lambda parameters, so they belong to the server side.
         // EF.Functions methods have no client implementation, and evaluating a query would execute it during translation.
         var nodeEvaluatable = _childrenEvaluatable
                               && node is not ParameterExpression
                               && node is not LambdaExpression
                               && node.NodeType != ExpressionType.Extension
                               && !IsDbFunctionsCall(node)
                               && !typeof(IQueryable).IsAssignableFrom(node.Type)
                               && evaluatableExpressionFilter.IsEvaluatableExpression(node, model);

         if (nodeEvaluatable)
            EvaluatableNodes.Add(node);

         _childrenEvaluatable = parentChildrenEvaluatable && nodeEvaluatable;

         return node;
      }

      private static bool IsDbFunctionsCall(Expression node)
      {
         return node is MethodCallExpression call
                && call.Method.GetParameters() is [{ ParameterType: var firstParameterType }, ..]
                && firstParameterType == typeof(DbFunctions);
      }
   }

   /// <summary>
   /// Pass 2: replaces every maximal evaluatable subtree with a constant.
   /// </summary>
   private sealed class ClosureFlatteningVisitor(HashSet<Expression> evaluatableNodes) : ExpressionVisitor
   {
      [return: NotNullIfNotNull(nameof(node))]
      public override Expression? Visit(Expression? node)
      {
         if (node is null)
            return null;

         if (!evaluatableNodes.Contains(node))
            return base.Visit(node);

         return node is ConstantExpression ? node : Evaluate(node);
      }

      // Like EF Core's funcletizer, a client-evaluatable test selects the branch, so a null guard protects a branch that contains a column.
      protected override Expression VisitConditional(ConditionalExpression node)
      {
         if (!evaluatableNodes.Contains(node.Test))
            return base.VisitConditional(node);

         var test = (bool)Evaluate(node.Test).Value!;

         return ConvertIfNeeded(Visit(test ? node.IfTrue : node.IfFalse), node.Type);
      }

      protected override Expression VisitBinary(BinaryExpression node)
      {
         if (node.NodeType is not (ExpressionType.AndAlso or ExpressionType.OrElse) || node.Method is not null || !evaluatableNodes.Contains(node.Left))
            return base.VisitBinary(node);

         var left = Evaluate(node.Left);

         // A lifted operator with a null left operand needs the right operand to decide the result.
         if (left.Value is not bool leftValue)
            return node.Update(left, node.Conversion, Visit(node.Right));

         var isShortCircuit = leftValue == (node.NodeType == ExpressionType.OrElse);

         return isShortCircuit
                   ? Expression.Constant(leftValue, node.Type)
                   : ConvertIfNeeded(Visit(node.Right), node.Type);
      }

      private static Expression ConvertIfNeeded(Expression expression, Type type)
      {
         return expression.Type == type ? expression : Expression.Convert(expression, type);
      }

      private static ConstantExpression Evaluate(Expression node)
      {
         try
         {
            return Expression.Constant(EvaluateValue(node), node.Type);
         }
         catch (Exception ex)
         {
            // Reading Nullable<T>.Value of a null value throws InvalidOperationException, not NullReferenceException.
            var member = ex switch
            {
               NullReferenceException => FindMemberOfNullInstance(node, nullableValueOnly: false),
               InvalidOperationException => FindMemberOfNullInstance(node, nullableValueOnly: true),
               _ => null
            };

            if (member is not null)
               throw new ArgumentException($"The member '{member.Member.Name}' of '{member.Member.DeclaringType?.Name}' cannot be read because the captured value is null. Provide a non-null value. Expression: {node}", ex);

            if (ex is NullReferenceException)
               throw new ArgumentException($"The expression cannot be evaluated because a captured value is null. Expression: {node}", ex);

            throw new InvalidOperationException($"The expression '{node}' cannot be evaluated on the client: {ex.Message}", ex);
         }
      }

      private static object? EvaluateValue(Expression node)
      {
         return Expression.Lambda<Func<object?>>(Expression.Convert(node, typeof(object))).Compile(preferInterpretation: true)();
      }

      private static MemberExpression? FindMemberOfNullInstance(Expression node, bool nullableValueOnly)
      {
         var collector = new InstanceMemberCollector();
         collector.Visit(node);

         foreach (var member in collector.Members)
         {
            if (nullableValueOnly && (member.Member.Name != nameof(Nullable<>.Value) || Nullable.GetUnderlyingType(member.Expression!.Type) is null))
               continue;

            try
            {
               if (EvaluateValue(member.Expression!) is null)
                  return member;
            }
            catch (Exception)
            {
               // An instance that throws is not the null instance.
            }
         }

         return null;
      }

      /// <summary>
      /// Collects the instance member accesses in post-order, so the innermost member comes first.
      /// </summary>
      private sealed class InstanceMemberCollector : ExpressionVisitor
      {
         public List<MemberExpression> Members { get; } = [];

         protected override Expression VisitMember(MemberExpression node)
         {
            var result = base.VisitMember(node);

            if (node.Expression is not null)
               Members.Add(node);

            return result;
         }
      }
   }
}
