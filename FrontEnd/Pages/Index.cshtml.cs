using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient httpClient;
        private readonly StatelessServiceContext serviceContext;
        private readonly FabricClient fabricClient;
        private readonly string reverseProxyBaseUri;
        private readonly Uri backEndServiceUri;
        private readonly Uri proxyAddress;

        public IndexModel(HttpClient httpClient, StatelessServiceContext serviceContext, FabricClient fabricClient)
        {
            this.httpClient = httpClient;
            this.serviceContext = serviceContext;
            this.fabricClient = fabricClient;
            reverseProxyBaseUri = Environment.GetEnvironmentVariable("ReverseProxyBaseUri");

            backEndServiceUri = new Uri($"{serviceContext.CodePackageActivationContext.ApplicationName}/BackEnd");
            proxyAddress = new Uri($"{reverseProxyBaseUri}{backEndServiceUri.AbsolutePath}");

            Values = new List<string>();
        }

        [BindProperty]
        public IEnumerable<string> Values { get; private set; }

        [BindProperty]
        public string Value { get; set; }

        public async Task OnGetAsync()
        {
            var partitions = await fabricClient.QueryManager.GetPartitionListAsync(backEndServiceUri);

            foreach (Partition partition in partitions)
            {
                var proxyUrl =
                    $"{proxyAddress}/api/Values?PartitionKey={((Int64RangePartitionInformation)partition.PartitionInformation).LowKey}&PartitionKind=Int64Range";

                using (var response = await httpClient.GetAsync(proxyUrl))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        continue;
                    }

                    Values = await response.Content.ReadAsAsync<IEnumerable<string>>();
                }
            }

        }

        public async Task OnPostAsync()
        {
            var partitions = await fabricClient.QueryManager.GetPartitionListAsync(backEndServiceUri);

            foreach (var partition in partitions)
            {
                var proxyUrl =
                    $"{proxyAddress}/api/Values/{Value}?PartitionKey={((Int64RangePartitionInformation)partition.PartitionInformation).LowKey}&PartitionKind=Int64Range";

                using (var response = await httpClient.PutAsJsonAsync(proxyUrl, Value))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        continue;
                    }
                }

                await OnGetAsync();

            }
        }


        public async Task OnPostRemoveAsync()
        {
            var partitions = await fabricClient.QueryManager.GetPartitionListAsync(backEndServiceUri);

            foreach (var partition in partitions)
            {
                var proxyUrl =
                    $"{proxyAddress}/api/Values/{Value}?PartitionKey={((Int64RangePartitionInformation)partition.PartitionInformation).LowKey}&PartitionKind=Int64Range";

                using (var response = await httpClient.DeleteAsync(proxyUrl))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        continue;
                    }
                }

                await OnGetAsync();

            }
        }
    }
}

