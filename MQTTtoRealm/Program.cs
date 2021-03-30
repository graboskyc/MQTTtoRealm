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
            Message m = new Message();

            Console.WriteLine("Opening...");

            // parse cli args
            if (args.Length != 2)
            {
                Console.WriteLine("Need 2 args: realm app ID followed by API key for auth in that app.");
            } else {
                string realmAppId = args[0];
                string apiKey = args[1];

                // mqtt setup
                var optionsBuilder = new MqttServerOptionsBuilder()
                    .WithConnectionBacklog(100)
                    .WithDefaultEndpointPort(1883)
                    .WithApplicationMessageInterceptor(context =>
                    {
                        context.AcceptPublish = true;
                        AsyncContext.Run(async () => await NewMessageAsync(realmAppId, apiKey, context));
                    });
                var mqttServer = new MqttFactory().CreateMqttServer();
                mqttServer.StartAsync(optionsBuilder.Build());

                // wait forever
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();

                // cleanup
                mqttServer.StopAsync();
            }
            
        }

        private static async Task NewMessageAsync(string realmAppId, string apiKey, MqttApplicationMessageInterceptorContext context)
        {
            Console.WriteLine("Got message!");
            var app = App.Create(realmAppId);
            var user = await app.LogInAsync(Credentials.ApiKey(apiKey));
            string partition = $"user={ user.Id }";
            var config = new SyncConfiguration(partition, user);
            var realm = await Realm.GetInstanceAsync(config);
            var payload = context.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(context.ApplicationMessage?.Payload);

            realm.Write(() =>
            {
                var msg = new Message { 
                    ApplicationMessage = "From .NET", 
                    Partition = partition,
                    Payload = payload,
                    ClientId = context.ClientId,
                    Topic = context.ApplicationMessage.Topic
                };
                realm.Add(msg);
            });
            Console.WriteLine("\tWritten!");
        }

        public class Message : RealmObject
        {
            [PrimaryKey]
            [MapTo("_id")]
            public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
            [MapTo("_pk")]
            [Required]
            public string Partition { get; set; }
            [MapTo("applicationMessage")]
            [Required]
            public string ApplicationMessage { get; set; }
            [MapTo("clientId")]
            public string ClientId { get; set; }
            [MapTo("topic")]
            public string Topic { get; set; }
            [MapTo("payload")]
            public string Payload { get; set; }
        }

        
    }
}
