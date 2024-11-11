using Azure;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace plsensors
{

    class Program
    {
        private static List<KerbsideSensor> sensorList = new List<KerbsideSensor>();
        private static Timer? stateUpdateTimer;
        private static int fileDelay;
        private static int eventFrequency;
        private static int ParkingTimeMin;
        private static int ParkingTimeMax;
        private static Random random = new Random();
        private static string blobStorageAccount = "";
        private static string blobStorageContainer = "";
        private static string filenamePrefix = "";
        private static bool writeFile = true;
        private static bool writeBlob = false;
        private static bool writeStream = false;
        private static string eventHubName = "";
        private static string eventHubNamespace = "";

        static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            if (!int.TryParse(configuration["FileDelay"], out fileDelay))
            {
                fileDelay = 5;
            }
            if (!int.TryParse(configuration["ParkingEventFrequency"], out eventFrequency))
            {
                eventFrequency = 5;
            }
            if (!int.TryParse(configuration["ParkingTimeMin"], out ParkingTimeMin))
            {
                ParkingTimeMin = 5;
            }
            if (!int.TryParse(configuration["ParkingTimeMax"], out ParkingTimeMax))
            {
                ParkingTimeMax = 30;
            }
            string jsonFilePath = configuration["JsonFilePath"] ?? "sensors.json";
            if (!bool.TryParse(configuration["WriteFile"], out writeFile))
            {
                writeFile = false;
            }
            if (!bool.TryParse(configuration["WriteBlob"], out writeBlob))
            {
                writeBlob = false;
            }
            if (!bool.TryParse(configuration["WriteStream"], out writeStream))
            {
                writeStream = false;
            }
            // Program.writeStream = bool.TryParse(configuration["WriteStream"], out bool writeStream);
            blobStorageAccount = configuration["BlobStorageAccount"] ?? "";
            blobStorageContainer = configuration["BlobStorageContainer"] ?? "";
            filenamePrefix = configuration["FileNamePrefix"] ?? "parking_sensor";
            eventHubName = configuration["EventHubName"] ?? "";
            eventHubNamespace = configuration["EventHubNamespace"] + ".servicebus.windows.net" ?? "";

            // Read the JSON data from the file
            string jsonData = File.ReadAllText(jsonFilePath);
            if (jsonData != null)
            {
                sensorList = JsonConvert.DeserializeObject<List<KerbsideSensor>>(jsonData) ?? new List<KerbsideSensor>();
            }

            foreach (var sensor in sensorList)
            {
                sensor.UpdateStatus(random.Next(2) == 0 ? KerbsideSensor.Status.Unoccupied : KerbsideSensor.Status.Present);
            }
            WriteBatchState(null);

            while (true)
            {
                RunSimulation();
            }
        }

        private static async Task<Response<BlobContentInfo>?> UploadFileToBlobStorage(string filename)
        {
            if (writeBlob == false)
            {
                return null;
            }
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }
            try
            {
                var blobServiceClient = new BlobServiceClient(new Uri($"https://{blobStorageAccount}.blob.core.windows.net"), new DefaultAzureCredential());
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobStorageContainer);
                var blobClient = blobContainerClient.GetBlobClient(filename);

                Console.WriteLine($"Uploading file to blob storage: {filename}");
                var response = await blobClient.UploadAsync(filename, true);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file to blob storage: {ex.Message}");
                return null;
            }

        }

        private static void WriteBatchState(object? state)
        {
            string filename = WriteStateToFile();
            var result = UploadFileToBlobStorage(filename).Result;
            if ((result != null) && (writeFile == false))
            {
                File.Delete(filename);
            }
            stateUpdateTimer = new Timer(WriteBatchState, null, fileDelay * 1000, Timeout.Infinite);
        }

        private static string WriteStateToFile()
        {
            if ((writeFile == false) && (writeBlob == false))
            {
                return "";
            }
            var filename = $"{filenamePrefix}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.json";
            try
            {
                string jsonData = JsonConvert.SerializeObject(sensorList, Formatting.Indented);
                File.WriteAllText(filename, jsonData);
                Console.WriteLine($"State written to file: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing state to file: {ex.Message}");
            }
            return filename;
        }

        private static void RunSimulation()
        {
            int randomDelay = random.Next(0, eventFrequency);
            Console.WriteLine($"Waiting for {randomDelay} seconds to park a car...");
            Thread.Sleep(randomDelay * 1000);

            // Select a random KerbsideSensor
            KerbsideSensor? randomSensor = SelectRandomParkingSpace();
            if (randomSensor == null)
            {
                Console.WriteLine("No parking spaces available");
                return;
            }

            // Simulate the car being parked
            randomSensor.UpdateStatus(KerbsideSensor.Status.Present);
            StreamKerbsideData(randomSensor);

            // Get a random parking duration
            int parkingDurationMinutes = random.Next(ParkingTimeMin, ParkingTimeMax);

            // Simulate parking duration by scheduling a callback to mark the spot as unoccupied after the random parking duration
            Timer unoccupiedTimer = new Timer(state =>
            {
                randomSensor.UpdateStatus(KerbsideSensor.Status.Unoccupied);
                StreamKerbsideData(randomSensor);
            }, null, parkingDurationMinutes * 1000, Timeout.Infinite);
        }

        private static async void StreamKerbsideData(KerbsideSensor sensorData)
        {
            // Always write the sensor data to the console for monitoring
            string json = JsonConvert.SerializeObject(sensorData, Formatting.None);
            Console.WriteLine(json);

            if (writeStream == false)
            {
                return;
            }
            if (string.IsNullOrEmpty(eventHubName) || string.IsNullOrEmpty(eventHubNamespace))
            {
                Console.WriteLine("Event Hub Namespace or Event Hub Name is not set");
                return;
            }

            // Write to the configured Event Hub
            if (writeStream)
            {
                var producer = new EventHubProducerClient(eventHubNamespace, eventHubName, new DefaultAzureCredential());
                var eventBatch = await producer.CreateBatchAsync();
                var eventData = new EventData(Encoding.UTF8.GetBytes(json));
                try
                {
                    eventBatch.TryAdd(eventData);
                    await producer.SendAsync(eventBatch);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    await producer.CloseAsync();
                }
            }
        }

        private static KerbsideSensor? SelectRandomParkingSpace()
        {
            var availableParkingSpaces = sensorList.Where(sensor => sensor.StatusDescription == "Unoccupied").ToList();
            if (availableParkingSpaces.Count == 0)
            {
                return null;
            }
            return availableParkingSpaces[random.Next(availableParkingSpaces.Count)];
        }
    }
}
