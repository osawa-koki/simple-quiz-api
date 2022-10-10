using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DBMod;
using MailMod;
using Microsoft.AspNetCore.Http.Json;

Env.Init();


DBClient.Init(Env.CONNECTION_STRING);
MailClient.Init(Env.SMTPSERVER, Env.SMTP_PORT, Env.SMTPSERVER_USER, Env.SMTPSERVER_PASSWORD);


const string CORS_RULE_NAME = "simple-quiz";

var builder = WebApplication.CreateBuilder(args);


// CORS許可
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CORS_RULE_NAME,
        builder =>
        {
            builder.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();
        }
    );
});


// Configure JSON options.
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.IncludeFields = true;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cookieの設定
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// CORS許可
app.UseCors(CORS_RULE_NAME);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();


// ***** ***** ***** ***** *****
// *****  ルーティング設定  *****
// ***** ***** ***** ***** *****

app.MapGet("/auth/session_id", Auth.GenerateToken);
app.MapPost("/auth/is_login", Auth.IsLogin);

app.MapPost("/auth/signup", Auth.SignUp);



app.Run();
