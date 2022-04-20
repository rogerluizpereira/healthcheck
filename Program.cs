using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var timeout = new TimeSpan(0,0,int.Parse(builder.Configuration["TIMEOUT"]));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddMySql(connectionString: string.Format("server={0};Database={1};Uid={2};Pwd={3};", 
                                                builder.Configuration["DB_HOST"], 
                                                builder.Configuration["DB_NAME"],
                                                builder.Configuration["DB_USERNAME"],
                                                builder.Configuration["DB_PASSWORD"])
            , name: "MySQL", timeout: timeout)
    .AddRedis(redisConnectionString: builder.Configuration["REDIS_CACHE_HOST"] + ":" + builder.Configuration["REDIS_CACHE_PORT"], name: "Redis Dados", timeout: timeout)
    .AddRedis(redisConnectionString: builder.Configuration["REDIS_HOST"] + ":" + builder.Configuration["REDIS_PORT"], name: "Redis Session", timeout: timeout)
    .AddElasticsearch ( elasticsearchUri: builder.Configuration["ELASTICSEARCH_HOST"], name: "Elasticsearch", timeout: timeout) 
    .AddDynamoDb(db => {
                 db.AccessKey = builder.Configuration["AWS_DYNAMO_KEY"];
                 db.SecretKey = builder.Configuration["AWS_DYNAMO_SECRET"];
                 db.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["AWS_DYNAMO_REGION"]);               
                 }, name: "DynamoDB", timeout: timeout)
   .AddUrlGroup(new Uri(builder.Configuration["ROTA_API_HEALTH"]), name: "Rota API", failureStatus: HealthStatus.Degraded, timeout: timeout);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

builder.Services.AddHealthChecksUI(options =>
{

    options.SetEvaluationTimeInSeconds(int.Parse(builder.Configuration["EVALUATION_TIME"]));
    options.SetMinimumSecondsBetweenFailureNotifications(int.Parse(builder.Configuration["SEC_BETWEEN_FAILURE_NOTIFICATIONS"]));
    options.MaximumHistoryEntriesPerEndpoint(int.Parse(builder.Configuration["HISTORY_ENTRIES"]));

    //Workaround para rodar no docker do mac (caminho relativo no docker mac não funcionou)
    //options.AddHealthCheckEndpoint("Questões", "/health");
    options.AddHealthCheckEndpoint("Questões", "http://localhost/health");
    //Ignorar certificados HTTPS
    options.UseApiEndpointHttpMessageHandler(sp =>
		{
			return new HttpClientHandler
			{
				ClientCertificateOptions = ClientCertificateOption.Manual,
				ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
			};
		});
})
.AddInMemoryStorage();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
//app.UseAuthorization();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseRouting();
app.UseHealthChecks("/health", new HealthCheckOptions {
    Predicate = p => true,
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});
app.UseHealthChecksUI(options => { options.UIPath = "/dashboard";});

app.MapControllers();

app.Run();