using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SpeedTest;
using SpeedTestLogger.Models;

namespace SpeedTestLogger
{
    class Program
    {

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello SpeedTestLogger");

            var config = new LoggerConfiguration();
            var speedTestRunner = new SpeedTestRunner(config.LoggerLocation);

            var testData = speedTestRunner.RunSpeedTest();
            var results = new TestResult
            {
                SessionId = new Guid(),
                User = config.UserId,
                Device = config.LoggerId,
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Data = testData
            };

            var success = false;
            using (var client = new SpeedTestApiClient(config.ApiUrl))
            {
                success = await client.PublishTestResult(results);
            }

            Console.WriteLine(success ? "Speedtest complete!" : "Speedtest failed!");
        }
    }
}
