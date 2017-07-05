using System.Linq;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Abstractions;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Pipelines.QueryGlobalFilters;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.SolrProvider;

namespace Sitecore.Support.ContentSearch.SolrProvider
{
    public class SolrSearchContext : Sitecore.ContentSearch.SolrProvider.SolrSearchContext, IProviderSearchContext
    {
        public SolrSearchContext(Sitecore.ContentSearch.SolrProvider.SolrSearchIndex index, SearchSecurityOptions options = SearchSecurityOptions.EnableSecurityCheck) : base(index, options)
        {
        }

        IQueryable<TItem> IProviderSearchContext.GetQueryable<TItem>()
        {
            return (this as IProviderSearchContext).GetQueryable<TItem>(new IExecutionContext[0]);
        }

        IQueryable<TItem> IProviderSearchContext.GetQueryable<TItem>(IExecutionContext executionContext)
        {
            return (this as IProviderSearchContext).GetQueryable<TItem>(new IExecutionContext[]
            {
                executionContext
            });
        }

        IQueryable<TItem> IProviderSearchContext.GetQueryable<TItem>(params IExecutionContext[] executionContexts)
        {
            Sitecore.ContentSearch.SolrProvider.LinqToSolrIndex<TItem> linqToSolrIndex = new LinqToSolrIndex<TItem>(this, executionContexts);
            if (Configuration.Settings.GetBoolSetting("ContentSearch.EnableSearchDebug", false))
            {
                ((IHasTraceWriter)linqToSolrIndex).TraceWriter = new LoggingTraceWriter(SearchLog.Log);
            }
            QueryGlobalFiltersArgs queryGlobalFiltersArgs = new QueryGlobalFiltersArgs(linqToSolrIndex.GetQueryable(), typeof(TItem), executionContexts.ToList<IExecutionContext>());
            this.Index.Locator.GetInstance<ICorePipeline>().Run("contentSearch.getGlobalLinqFilters", queryGlobalFiltersArgs);
            return (IQueryable<TItem>)queryGlobalFiltersArgs.Query;
        }
    }
}