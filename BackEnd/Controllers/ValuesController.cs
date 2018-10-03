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
        public async Task<IEnumerable<string>> Get()
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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] string value)
        {
            var myDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("items");

            using (var tx = stateManager.CreateTransaction())
            {
                await myDictionary.AddAsync(tx, Guid.NewGuid().ToString(), value);
                await tx.CommitAsync();
            }

            return new OkResult();
        }

        // DELETE api/values/5
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            var myDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("counts");

            using (var tx = stateManager.CreateTransaction())
            {
                if (await myDictionary.ContainsKeyAsync(tx, name))
                {
                    await myDictionary.TryRemoveAsync(tx, name);
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
