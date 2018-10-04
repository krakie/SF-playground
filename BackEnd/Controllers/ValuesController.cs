using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IReliableStateManager stateManager;

        public ValuesController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        // GET api/values
        [HttpGet]
        public async Task<IEnumerable<string>> GetAsync()
        {
            var ct = new CancellationToken();

            var myDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("items");

            using (var tx = stateManager.CreateTransaction())
            {
                var list = await myDictionary.CreateEnumerableAsync(tx);
                var enumerator = list.GetAsyncEnumerator();
                var result = new List<string>();
                while (await enumerator.MoveNextAsync(ct))
                {
                    result.Add(enumerator.Current.Value);
                }
                return result;
            }
        }

        [HttpPut("{value}")]
        public async Task<IActionResult> PutAsync(string value)
        {
            var myDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("items");

            using (var tx = stateManager.CreateTransaction())
            {
                await myDictionary.AddOrUpdateAsync(tx, value, value, (k, v) => v);
                await tx.CommitAsync();
            }
            return new OkResult();
        }

        // DELETE api/values/5
        [HttpDelete("{value}")]
        public async Task<IActionResult> DeleteAsync(string value)
        {
            var myDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("items");

            using (var tx = stateManager.CreateTransaction())
            {
                if (await myDictionary.ContainsKeyAsync(tx, value))
                {
                    await myDictionary.TryRemoveAsync(tx, value);
                    await tx.CommitAsync();
                    return new OkResult();
                }
                else
                {
                    return new NotFoundResult();
                }
            }
        }
    }
}
