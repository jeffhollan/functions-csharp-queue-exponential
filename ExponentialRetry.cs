using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Hollan.Function
{
    public static class ExponentialRetry
    {
        private static int retryCount = 5;

        [FunctionName("ExponentialRetry")]
        public static async Task Run(
            [ServiceBusTrigger("queue", Connection = "ServiceBusConnectionString")]Message message,
            string lockToken,
            MessageReceiver MessageReceiver,
            [ServiceBus("queue", Connection = "ServiceBusConnectionString")] MessageSender sender,
            ILogger log)
        {
            try
            {
                log.LogInformation($"C# ServiceBus queue trigger function processed message sequence #{message.SystemProperties.SequenceNumber}");
                throw new Exception("Some exception");
                await MessageReceiver.CompleteAsync(lockToken);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                log.LogInformation("Calculating exponential retry");

                // If the message doesn't have a retry-count, set as 0
                if(!message.UserProperties.ContainsKey("retry-count"))
                {
                    message.UserProperties["retry-count"] = 0;
                    message.UserProperties["original-SequenceNumber"] = message.SystemProperties.SequenceNumber;
                }

                // If there are more retries available
                if((int)message.UserProperties["retry-count"] < retryCount)
                {
                    var retryMessage = message.Clone();
                    var retryCount = (int)message.UserProperties["retry-count"] + 1;
                    var interval = 5 * retryCount;
                    var scheduledTime = DateTimeOffset.Now.AddSeconds(interval);

                    retryMessage.UserProperties["retry-count"] = retryCount;
                    await sender.ScheduleMessageAsync(retryMessage, scheduledTime);
                    await MessageReceiver.CompleteAsync(lockToken);

                    log.LogInformation($"Scheduling message retry {retryCount} to wait {interval} seconds and arrive at {scheduledTime.UtcDateTime}");
                }

                // If there are no more retries, deadletter the message (note the host.json config that enables this)
                else 
                {
                    log.LogCritical($"Exhausted all retries for message sequence # {message.UserProperties["original-SequenceNumber"]}");
                    await MessageReceiver.DeadLetterAsync(lockToken, "Exhausted all retries");
                }
            }
        }
    }
}
