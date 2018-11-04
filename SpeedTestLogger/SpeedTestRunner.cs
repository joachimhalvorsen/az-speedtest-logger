using System;
using System.Globalization;
using System.Linq;
using SpeedTest;
using SpeedTest.Models;
using SpeedTestLogger.Models;

namespace SpeedTestLogger
{
    public class SpeedTestRunner
    {
        private readonly SpeedTestClient _client;
        private readonly Settings _settings;
        private readonly RegionInfo _location;

        public SpeedTestRunner(RegionInfo location)
        {
            _client = new SpeedTestClient();
            _settings = _client.GetSettings();
            _location = location;
        }

        public TestData RunSpeedTest()
        {
            Console.WriteLine("Finding the best test servers");
            var server = FindBestTestServer();

            Console.WriteLine("Testing download speed");
            var downloadSpeed = TestDownloadSpeed(server);
            Console.WriteLine("Download speed was: {0} Mbps", downloadSpeed);

            Console.WriteLine("Testing upload speed");
            var uploadSpeed = TestUploadSpeed(server);
            Console.WriteLine("Upload speed was: {0} Mbps", uploadSpeed);

            return new TestData
            {
                Speeds = new TestSpeeds
                {
                    Download = downloadSpeed,
                    Upload = uploadSpeed
                },
                Client = new TestClient
                {
                    Ip = _settings.Client.Ip,
                    Latitude = _settings.Client.Latitude,
                    Lat = _settings.Client.Latitude,
                    Longitude = _settings.Client.Longitude,
                    Lon = _settings.Client.Longitude,
                    Isp = _settings.Client.Isp,
                    Country = _location.TwoLetterISORegionName
                },
                Server = new TestServer
                {
                    Host = server.Host,
                    Latitude = server.Latitude,
                    Lat = server.Latitude,
                    Longitude = server.Longitude,
                    Lon = server.Longitude,
                    Country = GetISORegionNameFromEnglishName(server.Country),
                    Distance = (int)Math.Round(server.Distance),
                    Ping = server.Latency,
                    Id = server.Id
                }
            };
        }

        private string GetISORegionNameFromEnglishName(string englishName)
        {
            // Wondering why this culture isn't supported? https://stackoverflow.com/a/41879861/840453
            var unsupportedCultureLCID = 4096;

            var allRegions = CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                .Select(culture => culture.LCID)
                .Where(lcid => lcid != unsupportedCultureLCID)
                .Select(lcid => new RegionInfo(lcid));

            var region = allRegions.FirstOrDefault(c =>
            {
                return String.Equals(c.EnglishName, englishName, StringComparison.OrdinalIgnoreCase);
            });

            if (region == null)
            {
                var unknownISORegionName = "XX";
                return unknownISORegionName;
            }

            return region.TwoLetterISORegionName;
        }

        private Server FindBestTestServer()
        {
            var tenLocalServers = _settings.Servers.Where(s => s.Country == _location.EnglishName).Take(10);

            var serversOrderByLatency = tenLocalServers
                .Select(s => { s.Latency = _client.TestServerLatency(s); return s; })
                .OrderBy(s => s.Latency).ToList();

            var server = serversOrderByLatency.First();
            return server;
        }

        private double TestDownloadSpeed(Server server)
        {
            var downloadSpeed = _client.TestDownloadSpeed(server, _settings.Download.ThreadsPerUrl);

            return ConvertToSpeedPerMbps(downloadSpeed);
        }

        private double TestUploadSpeed(Server server)
        {
            var uploadSpeed = _client.TestUploadSpeed(server, _settings.Upload.ThreadsPerUrl);

            return ConvertToSpeedPerMbps(uploadSpeed);
        }

        private double ConvertToSpeedPerMbps(double speed)
        {
            return Math.Round(speed / 1024, 2);
        }
    }
}
