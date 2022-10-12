using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DBMod;
using MailMod;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using System.Reflection;


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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "simple-quiz API",
        Description = "API for simple-quiz",
        TermsOfService = new Uri($"{Env.DOMAIN}/terms"),
        Contact = new OpenApiContact
        {
            Name = "Example Contact",
            Url = new Uri($"{Env.DOMAIN}/contact")
        },
        License = new OpenApiLicense
        {
            Name = "Example License",
            Url = new Uri($"{Env.DOMAIN}/license")
        }
    });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

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

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseAuthorization();

app.MapControllers();


// ***** ***** ***** ***** *****
// *****  ルーティング設定  *****
// ***** ***** ***** ***** *****

// ==============================
// =====       認証系       =====
// ==============================

app.MapGet("/auth/session_id", Auth.GenerateToken);
app.MapGet("/auth/is_signin", Auth.IsLogin);
app.MapPost("/auth/pre_signup", Auth.PreSignUp);
app.MapPost("/auth/signup", Auth.SignUp);
app.MapDelete("/auth/signout", Auth.SignOut);
app.MapPost("/auth/signin", Auth.SignIn);


// ==============================
// =====     テンプレート     =====
// ==============================

app.MapGet("/template/{template_id}", Template.Detail);
app.MapGet("/template/list", Template.List);
app.MapGet("/template/search", Template.Search);
app.MapPost("/template", Template.Create);
app.MapPut("/template/{template_id}", Template.Update);
app.MapDelete("/template/{template_id}", Template.Delete);


// ==============================
// =====       ルーム       =====
// ==============================





app.Run();
