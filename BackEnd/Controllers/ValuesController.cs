using System;
using System.Collections.Generic;
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

            var answers = await stateManager.GetOrAddAsync<IReliableDictionary<long, string>>("answers");

            using (var tx = stateManager.CreateTransaction())
            {
                var list = await answers.CreateEnumerableAsync(tx);
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
            var answers = await stateManager.GetOrAddAsync<IReliableDictionary<long, string>>("answers");

            using (var tx = stateManager.CreateTransaction())
            {
                var count = await answers.GetCountAsync(tx);
                await answers.AddOrUpdateAsync(tx, count, value, (k, v) => v);
                await tx.CommitAsync();
            }
            return new OkResult();
        }

        // DELETE api/values/5
        [HttpDelete]
        public async Task<IActionResult> DeleteAsync()
        {
            var answers = await stateManager.GetOrAddAsync<IReliableDictionary<long, string>>("answers");

            using (var tx = stateManager.CreateTransaction())
            {
                var count = await answers.GetCountAsync(tx);

                if (await answers.ContainsKeyAsync(tx, count - 1))
                {
                    await answers.TryRemoveAsync(tx, count - 1);
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
