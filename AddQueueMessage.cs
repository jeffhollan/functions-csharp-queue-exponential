using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Hollan.Function
{
    public static class AddQueueMessage
    {
        [FunctionName("AddQueueMessage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [ServiceBus("queue", Connection = "ServiceBusConnectionString")] IAsyncCollector<string> message,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await message.AddAsync("Test");

            return new OkResult();
        }
    }
}
