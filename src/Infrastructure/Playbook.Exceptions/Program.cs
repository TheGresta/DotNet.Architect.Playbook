using Microsoft.OpenApi.Models;
using Playbook.Exceptions;
using Playbook.Exceptions.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Gold Standard API", Version = "v1" });

    // Inject our Global Error Documentation
    options.OperationFilter<GlobalErrorOperationFilter>();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructureErrorHandling();

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true; // MUST be true to see CustomerNumber/TraceId
    options.TimestampFormat = "HH:mm:ss ";
    options.SingleLine = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseInfrastructureErrorHandling();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
