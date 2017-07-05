namespace Sitecore.Support.ContentSearch.SolrProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Linq.Common;
    using Sitecore.ContentSearch.Linq.Parsing;
    using Sitecore.ContentSearch.Linq.Solr;
    using Sitecore.ContentSearch.SolrProvider;

    public class LinqToSolrIndex<TItem> : Sitecore.ContentSearch.SolrProvider.LinqToSolrIndex<TItem>
    {
        public LinqToSolrIndex(Sitecore.ContentSearch.SolrProvider.SolrSearchContext context, IExecutionContext executionContext) : base(context, executionContext)
        {
            this.context = context;
            this.executionContexts = new[] { executionContext };
        }

        public LinqToSolrIndex(Sitecore.ContentSearch.SolrProvider.SolrSearchContext context, IExecutionContext[] executionContexts) : base(context, executionContexts)
        {
            this.context = context;
            this.executionContexts = executionContexts;
        }

        private readonly Sitecore.ContentSearch.SolrProvider.SolrSearchContext context;
        private readonly IExecutionContext[] executionContexts;

        private readonly FieldInfo queryMapperFieldInfo =
            typeof(Sitecore.ContentSearch.Linq.Solr.SolrIndex<TItem>).GetField("queryMapper",
                BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo parametersFieldInfo =
            typeof(Sitecore.ContentSearch.Linq.Solr.SolrIndex<TItem>).GetField("parameters",
                BindingFlags.NonPublic | BindingFlags.Instance);

        public override IQueryable<TItem> GetQueryable()
        {
            var fieldNameTranslator = new SolrFieldNameTranslator(context.Index as Sitecore.ContentSearch.SolrProvider.SolrSearchIndex);
            CultureExecutionContext cultureExecutionContext = base.Parameters.ExecutionContexts.FirstOrDefault((IExecutionContext c) => c is CultureExecutionContext) as CultureExecutionContext;
            System.Globalization.CultureInfo cultureInfo = (cultureExecutionContext == null) ? System.Globalization.CultureInfo.GetCultureInfo(Sitecore.Configuration.Settings.DefaultLanguage) : cultureExecutionContext.Culture;
            fieldNameTranslator.AddCultureContext(cultureInfo);
            var parameters = new SolrIndexParameters(context.Index.Configuration.IndexFieldStorageValueFormatter,
                context.Index.Configuration.VirtualFieldProcessors, fieldNameTranslator, executionContexts);
            var queryMapper = new SolrQueryMapper(parameters);
            parametersFieldInfo.SetValue(this, parameters);
            queryMapperFieldInfo.SetValue(this, queryMapper);

            IQueryable<TItem> queryable = new GenericQueryable<TItem, SolrCompositeQuery>(this, this.QueryMapper, this.QueryOptimizer, fieldNameTranslator);
            (queryable as IHasTraceWriter).TraceWriter = ((IHasTraceWriter)this).TraceWriter;
            List<IPredefinedQueryAttribute> list = this.GetTypeInheritance(typeof(TItem)).SelectMany((Type t) => t.GetCustomAttributes(typeof(IPredefinedQueryAttribute), true).Cast<IPredefinedQueryAttribute>()).ToList<IPredefinedQueryAttribute>();
            foreach (IPredefinedQueryAttribute current in list)
            {
                queryable = current.ApplyFilter<TItem>(queryable, this.ValueFormatter);
            }
            return queryable;
        }

        private TResult ApplyScalarMethods<TResult, TDocument>(SolrCompositeQuery compositeQuery, object processedResults, object results)
        {
            System.Type type = typeof(TResult).GetGenericArguments()[0];
            System.Reflection.MethodInfo method = this.GetType().BaseType.GetMethod("ApplyScalarMethods", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            System.Reflection.MethodInfo methodInfo = method.MakeGenericMethod(new System.Type[]
            {
                typeof(TResult),
                type
            });
            return (TResult) ((object) methodInfo.Invoke(this, new object[]
            {
                compositeQuery,
                processedResults,
                results
            }));
        }
        private IEnumerable<Type> GetTypeInheritance(Type type)
        {
            yield return type;
            Type baseType = type.BaseType;
            while (baseType != null)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
            yield break;
        }
    }
}