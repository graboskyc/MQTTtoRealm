using System;
using System.Threading.Tasks;
using Nito.AsyncEx;
using MongoDB.Bson;
using Realms;
using Realms.Sync;
using MQTTnet;
using MQTTnet.Server;
using System.Text;

namespace MQTTtoRealm
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Opening...");

            // parse cli args
            if (args.Length != 2)
            {
                Console.WriteLine("Need 2 args: realm app ID followed by API key for auth in that app.");
                return;
            }

            var realmAppId = args[0];
            var apiKey = args[1];

            StartAsync(realmAppId, apiKey).Wait();
        }

        private static async Task StartAsync(string realmAppId, string apiKey)
        {
            // realm setup
            var app = App.Create(realmAppId);
            var user = await app.LogInAsync(Credentials.ApiKey(apiKey));
            var partition = $"user={user.Id}";
            RealmConfiguration.DefaultConfiguration = new SyncConfiguration(partition, user);

            // mqtt setup
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithConnectionBacklog(100)
                .WithDefaultEndpointPort(1883)
                .WithApplicationMessageInterceptor(context =>
                {
                    context.AcceptPublish = true;
                    HandleNewMessage(context);
                });

            var mqttServer = new MqttFactory().CreateMqttServer();
            await mqttServer.StartAsync(optionsBuilder.Build());

            // wait forever
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();

            // cleanup
            await mqttServer.StopAsync();

            // wait for Realm uploads
            AsyncContext.Run(async () =>
            {
                using var realm = Realm.GetInstance();
                await realm.GetSession().WaitForUploadAsync();
            });
        }

        private static void HandleNewMessage(MqttApplicationMessageInterceptorContext context)
        {
            Console.WriteLine("Got message!");

            using var realm = Realm.GetInstance();
            var payload = context.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(context.ApplicationMessage?.Payload);

            realm.Write(() =>
            {
                var msg = new IOTDataPoint
                { 
                    Reading = Int32.Parse(payload),
                    DeviceName = context.ClientId
                };

                realm.Add(msg);
            });

            Console.WriteLine("\tWritten!");
        }

        public class IOTDataPoint : RealmObject {
            [PrimaryKey]
            [MapTo("_id")]
            public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
            [MapTo("_pk")]
            public string Partition { get; set; }
            [MapTo("device")]
            public string DeviceName { get; set; }
            [MapTo("reading")]
            public int Reading { get; set; }
        }
    }
}
