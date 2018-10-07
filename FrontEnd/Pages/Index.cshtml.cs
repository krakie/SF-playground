using System;
using System.Collections.Generic;
using System.Fabric;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

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
            using (var response = await SendToBackEnd(async (url) => await httpClient.GetAsync(url), (part1, part2) => $"{part1}{part2}"))
            {
                Values = await response.Content.ReadAsAsync<IEnumerable<string>>();
            }
        }

        public async Task OnPostNextAsync()
        {
            await SendToBackEnd(async (url) => await httpClient.PutAsJsonAsync(url, Value), (part1, part2) => $"{part1}/{Value}{part2}");
            await OnGetAsync();
            Value = string.Empty;
        }


        public async Task OnPostBackAsync()
        {
            await OnGetAsync();
            Value = Values.Last();
            await SendToBackEnd(async (url) => await httpClient.DeleteAsync(url), (part1, part2) => $"{part1}{part2}");
            await OnGetAsync();
        }

        private async Task<HttpResponseMessage> SendToBackEnd(Func<string, Task<HttpResponseMessage>> httpMethod, Func<string, string, string> buildProxyUrl)
        {
            var partitions = await fabricClient.QueryManager.GetPartitionListAsync(backEndServiceUri);

            foreach (var partition in partitions)
            {
                var proxyUrl = buildProxyUrl($"{proxyAddress}/api/Values", $"?PartitionKey={((Int64RangePartitionInformation)partition.PartitionInformation).LowKey}&PartitionKind=Int64Range");

                var response = await httpMethod(proxyUrl);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return response;
                }
            }
            return new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.NotFound };
        }
    }
}

