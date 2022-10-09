using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

const CORS_RULE_NAME = "simple-quiz";

// Configure the cert and the key


var builder = WebApplication.CreateBuilder(args);

// クッキー
// builder.Services.AddDistributedMemoryCache();

// builder.Services.AddSession(options =>
// {
//     options.IdleTimeout = TimeSpan.FromSeconds(10);
//     options.Cookie.HttpOnly = true;
//     options.Cookie.IsEssential = true;
//     options.Cookie.Name = ".AdventureWorks.Session";
// });

// // 証明書
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ConfigureHttpsDefaults(httpsOptions =>
//     {
//         var certPath = Path.Combine(builder.Environment.ContentRootPath, "/etc/letsencrypt/live/api.simple-quiz.org/cert.pem");
//         var keyPath = Path.Combine(builder.Environment.ContentRootPath, "/etc/letsencrypt/live/api.simple-quiz.org/privkey.pem");

//         httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
//     });
// });

// CORS許可
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CORS_RULE_NAME,
        builder =>
        {
            builder.WithOrigins("*");
        }
    );
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

// Cookieの使用
// app.UseSession();

app.MapControllers();

app.MapGet("/auth/sessionid", () => {
    return new {
        successed = true,
        error = "",
        sessionid = Guid.NewGuid().ToString(),
    };
});

app.Run();
