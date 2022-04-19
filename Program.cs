using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var dbString = 

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddMySql(connectionString: string.Format("server={0};Database={1};Uid={2};Pwd={3};", 
                                                builder.Configuration["DB_HOST"], 
                                                builder.Configuration["DB_NAME"],
                                                builder.Configuration["DB_USERNAME"],
                                                builder.Configuration["DB_PASSWORD"])
            , name: "MySQL")
    .AddRedis(redisConnectionString: builder.Configuration["REDIS_CACHE_HOST"] + ":" + builder.Configuration["REDIS_CACHE_PORT"], name: "Redis Dados")
    .AddRedis(redisConnectionString: builder.Configuration["REDIS_HOST"] + ":" + builder.Configuration["REDIS_PORT"], name: "Redis Session")
    .AddElasticsearch ( elasticsearchUri: builder.Configuration["ELASTICSEARCH_HOST"], name: "Elasticsearch") 
    .AddDynamoDb(db => {
                 db.AccessKey = builder.Configuration["AWS_DYNAMO_KEY"];
                 db.SecretKey = builder.Configuration["AWS_DYNAMO_SECRET"];
                 db.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["AWS_DYNAMO_REGION"]);
    }
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(5);
    options.SetMinimumSecondsBetweenFailureNotifications(5);
    options.MaximumHistoryEntriesPerEndpoint(100);
    //Workaround para rodar no docker do mac
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