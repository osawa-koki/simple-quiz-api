using System.Security.Cryptography.X509Certificates;

// Configure the cert and the key


var builder = WebApplication.CreateBuilder(args);

// 証明書
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        var certPath = Path.Combine(builder.Environment.ContentRootPath, "/etc/letsencrypt/live/api.simple-quiz.org/cert.pem");
        var keyPath = Path.Combine(builder.Environment.ContentRootPath, "/etc/letsencrypt/live/api.simple-quiz.org/privkey.pem");

        httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
    });
});

// CORS許可
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "simple-quiz",
        builder =>
        {
            builder.WithOrigins("https://simple-quiz.org", "https://www.simple-quiz.org");
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
app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

// Cookieの使用
app.UseSession();

app.MapControllers();

app.MapGet("/test/{id}", async (int id) => {
    return "a";
});

app.Run();
