using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PicStoneFotoAPI.Data;
using PicStoneFotoAPI.Services;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURAÇÃO DE LOGS COM SERILOG ==========
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/picstone-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ========== CONFIGURAÇÃO DO BANCO DE DADOS ==========
// Detecta automaticamente o tipo de banco com base nas variáveis de ambiente
var databaseUrl = builder.Configuration["DATABASE_URL"]; // Railway PostgreSQL
var useSqlite = builder.Configuration["USE_SQLITE"]?.ToLower() == "true";
var sqlConnectionString = builder.Configuration["SQL_CONNECTION_STRING"];

if (!string.IsNullOrEmpty(databaseUrl))
{
    // PostgreSQL no Railway (formato: postgres://user:pass@host:port/db)
    // Converter URI para formato ADO.NET que o Npgsql entende
    var connectionString = ConvertPostgresUrlToConnectionString(databaseUrl);
    Log.Information("Usando PostgreSQL (Railway): {Host}", new Uri(databaseUrl).Host);
    Log.Information("Connection String convertida: {ConnectionString}", MaskConnectionString(connectionString));

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else if (useSqlite || builder.Environment.IsDevelopment())
{
    // SQLite para desenvolvimento local
    var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "picstone.db");
    Log.Information("Usando SQLite: {DbPath}", dbPath);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}
else if (!string.IsNullOrEmpty(sqlConnectionString))
{
    // SQL Server (se configurado)
    Log.Information("Usando SQL Server");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(sqlConnectionString));
}
else
{
    // Fallback para SQLite
    var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "picstone.db");
    Log.Information("Nenhum banco configurado, usando SQLite: {DbPath}", dbPath);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}

// ========== CONFIGURAÇÃO DE AUTENTICAÇÃO JWT ==========
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "ChaveSecretaPadraoParaDesenvolvimento123!@#";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "PicStoneFotoAPI",
        ValidAudience = "PicStoneFotoApp",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// ========== CONFIGURAÇÃO DE CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========== REGISTRO DE SERVIÇOS ==========
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FotoService>();

// ========== CONTROLLERS E SWAGGER ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PicStone Foto API",
        Version = "v1",
        Description = "API para captura de fotos via mobile com integração SQL Server"
    });

    // Configuração para JWT no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Exemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ========== INICIALIZAÇÃO DO BANCO E USUÁRIO PADRÃO ==========
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();

        // Para SQLite e PostgreSQL, cria automaticamente as tabelas
        if (useSqlite || !string.IsNullOrEmpty(databaseUrl))
        {
            var dbType = !string.IsNullOrEmpty(databaseUrl) ? "PostgreSQL" : "SQLite";
            Log.Information("Criando/atualizando banco de dados {DbType}...", dbType);
            await context.Database.EnsureCreatedAsync();
            Log.Information("Banco de dados {DbType} pronto!", dbType);
        }

        // Verifica se consegue acessar o banco
        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            Log.Information("Conexao com banco de dados estabelecida");

            // Cria usuário admin padrão (admin/admin123)
            try
            {
                await authService.CriarUsuarioInicialAsync();
                Log.Information("Usuario admin verificado/criado com sucesso");
            }
            catch (Exception exUser)
            {
                Log.Warning("Nao foi possivel criar usuario inicial: {Message}", exUser.Message);
                if (!useSqlite)
                {
                    Log.Information("Para SQL Server, execute o script criar-tabelas.sql no banco de dados");
                }
            }
        }
        else
        {
            Log.Error("Nao foi possivel conectar ao banco de dados");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Erro ao inicializar banco de dados");
        Log.Information("A aplicacao continuara rodando, mas pode haver erros ao acessar o banco");
    }
}

// ========== MIDDLEWARE ==========
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PicStone Foto API v1");
    });
}

// Serve arquivos estáticos do frontend
app.UseStaticFiles();
app.UseDefaultFiles();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Rota raiz
app.MapGet("/", () => Results.Redirect("/index.html"));

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

Log.Information("Iniciando PicStone Foto API na porta {Port}", port);
app.Run();

// ========== HELPER METHODS ==========
static string ConvertPostgresUrlToConnectionString(string databaseUrl)
{
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]}";

        // Adicionar SSL Mode se necessário
        if (!connectionString.Contains("SSL Mode"))
        {
            connectionString += ";SSL Mode=Prefer";
        }

        return connectionString;
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Erro ao converter DATABASE_URL para connection string");
        throw;
    }
}

static string MaskConnectionString(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return "null";

    // Mascarar senha no formato ADO.NET
    var parts = connectionString.Split(';');
    var masked = new List<string>();

    foreach (var part in parts)
    {
        if (part.StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
        {
            masked.Add("Password=******");
        }
        else
        {
            masked.Add(part);
        }
    }

    return string.Join(";", masked);
}
