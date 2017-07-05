using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.Security;

namespace Sitecore.Support.ContentSearch.SolrProvider
{
    public class SolrSearchIndex : Sitecore.ContentSearch.SolrProvider.SolrSearchIndex
    {
        public SolrSearchIndex(string name, string core, IIndexPropertyStore propertyStore) : base(name, core, propertyStore)
        {
        }

        public override IProviderSearchContext CreateSearchContext(SearchSecurityOptions options = SearchSecurityOptions.EnableSecurityCheck)
        {
            return new SolrSearchContext(this, options); 
        }
    }
}