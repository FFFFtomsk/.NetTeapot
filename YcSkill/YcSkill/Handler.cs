using Microsoft.Extensions.Logging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Yandex.Cloud.Credentials;
using Yandex.Cloud.Functions;
using YcFunction;
using Ydb.Sdk;
using Ydb.Sdk.Auth;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;
using Ydb.Sdk.Yc;
public class Handler : YcFunction<string, Task<bool>>
{
    public async Task<bool> FunctionHandler(string message, Context c)
    {
        const string endpoint = "grpcs://ydb.serverless.yandexcloud.net:2135";
        const string database = "your_db_path";
        var token = JsonSerializer.Deserialize<JsonToken>(c.TokenJson);

        //Console.WriteLine($"message: {message}");

        EventObject msgEvent = JsonSerializer.Deserialize<EventObject>(message);
        if (msgEvent != null)
        {
            float temperature = 0;
            int waterLevel = 0;
            bool isOn = false;

            var config = new DriverConfig(
                endpoint: endpoint,
                database: database,
                credentials: new TokenProvider(token?.access_token)
            );

            await using var driver = await Driver.CreateInitialized(config);

            Console.WriteLine($"Driver");

            using var tableClient = new TableClient(driver, new TableClientConfig());
            Console.WriteLine($"InittableClientialize");

            foreach (var msg in msgEvent.messages)
            {
                byte[] data = Convert.FromBase64String(msg.details.payload);
                string decodedString = System.Text.Encoding.UTF8.GetString(data);
                var payload = decodedString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (payload != null)
                {
                    temperature = float.Parse(payload[0]);
                    waterLevel = int.Parse(payload[1]);
                    isOn = bool.Parse(payload[2]);

                    var response = await tableClient.SessionExec(async session =>
                    {
                        var query = @$"
                    DECLARE $event_date AS Timestamp;
                    DECLARE $temperature AS Float;
                    DECLARE $water_level AS Int32;
                    DECLARE $is_on AS Bool;

                    UPSERT INTO teapot_state (event_date, temperature, water_level, is_on) VALUES
                        ($event_date, $temperature, $water_level, $is_on);
                ";

                        var settings = new ExecuteDataQuerySettings
                        {
                            OperationTimeout = TimeSpan.FromSeconds(1),
                            TransportTimeout = TimeSpan.FromSeconds(5),
                            KeepInQueryCache = false
                        };
                        settings.KeepInQueryCache = true;

                        return await session.ExecuteDataQuery(
                            query: query,
                            txControl: TxControl.BeginSerializableRW().Commit(),
                            parameters: new Dictionary<string, YdbValue>
                                {
                            { "$event_date", YdbValue.MakeTimestamp(DateTime.Now) },
                            { "$temperature", YdbValue.MakeFloat(temperature) },
                            { "$water_level", YdbValue.MakeInt32(waterLevel) },
                            { "$is_on", YdbValue.MakeBool(isOn)}
                                },
                            settings: settings
                        );
                    });

                    response.Status.EnsureSuccess();
                }
            }

            Console.WriteLine($"Function name: {c.FunctionId}");
            Console.WriteLine($"Function version: {c.FunctionVersion}");
            return true;
        }
        return false;
    }


    class JsonToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }

    }
}
