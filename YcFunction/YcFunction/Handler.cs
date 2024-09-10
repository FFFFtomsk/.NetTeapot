using System.Text.Json;
using Yandex.Cloud.Functions;
using Ydb.Sdk.Auth;
using Ydb.Sdk;
using Ydb.Sdk.Services.Table;
public class Request
{
    public string version { get; set; }
    public JsonElement session { get; set; }
    public JsonElement request { get; set; }
}

public class Response
{
    public string version { get; set; }
    public JsonElement session { get; set; }
    public object response { get; set; }

    public Response(string version, JsonElement session, object response)
    {
        this.version = version;
        this.session = session;
        this.response = response;
    }
}

class JsonToken
{
    public string access_token { get; set; }
    public int expires_in { get; set; }
    public string token_type { get; set; }

}

class TeapotState
{
    public float Temperature { get; set; }
    public int WaterLevel { get; set; }
    public bool IsOn { get; set; }
}

public class Handler
{
    public async Task<Response> FunctionHandler(Request r, Context c)
    {
        string version = r.version;
        JsonElement session = r.session;
        JsonElement request = r.request;

        Console.WriteLine($"request: {JsonSerializer.Serialize(r)}");
        Console.WriteLine($"context: {JsonSerializer.Serialize(c)}");


        var token = JsonSerializer.Deserialize<JsonToken>(c.TokenJson);
        
        string text = "Жду указаний.";
        var intents = request.GetProperty("nlu").GetProperty("intents");
        JsonElement intent;
        if (intents.TryGetProperty("Announcement", out intent))
        {
            text = $"Очень просто, приходите на конференцию Dot Next десятого и одиннадцатого сентября. Там мой создатель Александр Бусыгин расскажет как сделать умное устройство с использованием dot NET nano Framework и Yandex I o T Core.";
        }
        if (intents.TryGetProperty("GetState", out intent))
        {
            var state = await GetLastState(token?.access_token);
            text = $"Я {(state.IsOn ? "включен" : "выключен")}. Температура {state.Temperature} градус{GetPostfix((int)state.Temperature)}. Уровень воды {state.WaterLevel} процент{GetPostfix(state.WaterLevel)}.";
        }
        if (intents.TryGetProperty("TurnOn", out intent))
        {
            var temp = intent.GetProperty("slots").GetProperty("temp").GetProperty("value").Deserialize<int>();
            await SendCommand(temp.ToString());
            text = $"Команда отправлена.";
        }


        var responseObj = new
        {
            text = text,
            end_session = false,
        };

        return new Response(version, session, responseObj);
    }

    private string GetPostfix(int num)
    {
        if(num > 10 && num < 15)
            return "ов";

        var remainder = num % 10;
        switch (remainder)
        {
            case 0:
                return "ов";
                break;

            case > 4:
                return "ов";
                break;

            case > 1:
                return "а";
                break;

            default:
                return string.Empty;
                break;
        }
    }

    private async Task<TeapotState> GetLastState(string token)
    {
        const string endpoint = "grpcs://ydb.serverless.yandexcloud.net:2135";
        const string database = "your_db_path";

        var config = new DriverConfig(
                endpoint: endpoint,
                database: database,
                credentials: new TokenProvider(token)
            );

        await using var driver = await Driver.CreateInitialized(config);
        using var tableClient = new TableClient(driver, new TableClientConfig());
        Console.WriteLine($"BeforeQuery");
        var response = await tableClient.SessionExec(async session =>
        {
            var query = @"
        SELECT
            event_date,
            temperature,
            water_level,
            is_on
        FROM teapot_state
        ORDER BY event_date DESC
        LIMIT 1;
    ";

            return await session.ExecuteDataQuery(
                query: query,
                txControl: TxControl.BeginSerializableRW().Commit()
            );
        });
        Console.WriteLine($"QueryFinished");
        response.Status.EnsureSuccess();
        var queryResponse = (ExecuteDataQueryResponse)response;
        var resultSet = queryResponse.Result.ResultSets[0];
        Console.WriteLine($"RowCount: {resultSet.Rows.Count}");

        var row = resultSet.Rows[0];
        var temp = (float?)row[1];
        Console.WriteLine($"Temperature: {temp}");

        var water = (int?)resultSet.Rows[0][2];
        Console.WriteLine($"WaterLevel: {water}");

        var on = (bool?)resultSet.Rows[0][3];
        Console.WriteLine($"IsOn: {on}");
        return new TeapotState()
        {
            Temperature = temp.Value,
            WaterLevel = water.Value,
            IsOn = on.Value
        };
    }

    private async Task SendCommand(string message)
    {
        string RegistryID = "your_registry_id";
        string RegistryPassword = "your_registry_password";
        string DeviceId = "your_device_id";
        string topic = YaClient.TopicName(DeviceId, EntityType.Device, TopicType.Commands);

        using (YaClient regClient = new YaClient())
        {
            regClient.Start(RegistryID, RegistryPassword);


            if (!regClient.WaitConnected())
            {
                return;
            }

            await regClient.Publish(topic, message, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
            Console.WriteLine($"Published data: {message} to: {topic}");

        }
    }

}

