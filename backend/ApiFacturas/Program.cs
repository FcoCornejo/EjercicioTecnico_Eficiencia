using Microsoft.EntityFrameworkCore.Infrastructure;
using ApiFacturas.Api;
using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Aplicacion.Importacion;
using ApiFacturas.Aplicacion.Consultas;
using ApiFacturas.Aplicacion.Creditos;
using ApiFacturas.Infraestructura.Lectura;
using ApiFacturas.Infraestructura.Persistencia;
using ApiFacturas.Infraestructura.Reloj;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var constructor = WebApplication.CreateBuilder(args);
constructor.Services.AddControllers().ConfigureApiBehaviorOptions(opciones => opciones.InvalidModelStateResponseFactory = contexto => new BadRequestObjectResult(new ValidationProblemDetails(contexto.ModelState) { Status = 400, Title = "Solicitud invalida.", Instance = contexto.HttpContext.Request.Path }));
constructor.Services.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = contexto => contexto.ProblemDetails.Extensions["traceId"] = contexto.HttpContext.TraceIdentifier);
constructor.Services.AddExceptionHandler<ManejadorErrores>();
constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddSwaggerGen();
constructor.Services.AddDbContext<ContextoFacturas>(opciones => opciones.UseSqlite(constructor.Configuration.GetConnectionString("Facturas") ?? "Data Source=eficiencia.db;Default Timeout=30"));
constructor.Services.AddSingleton(TimeProvider.System);
constructor.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(constructor.Configuration["ZonaNegocio"] ?? "America/Santiago"));
constructor.Services.AddSingleton<IRelojNegocio, RelojSantiago>();
constructor.Services.AddScoped<ILectorFacturasJson, LectorFacturasJson>();
constructor.Services.AddScoped<IRepositorioFacturas, RepositorioFacturas>();
constructor.Services.AddScoped<ValidadorImportacion>();
constructor.Services.AddScoped<FachadaImportacion>();
constructor.Services.AddScoped<ServicioConsultas>();
constructor.Services.AddScoped<RegistrarNotaCredito>();
constructor.Services.AddCors(opciones => opciones.AddDefaultPolicy(politica => politica.WithOrigins(constructor.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"]).AllowAnyHeader().AllowAnyMethod()));
var aplicacion = constructor.Build();
aplicacion.UseExceptionHandler();
aplicacion.UseStatusCodePages();
aplicacion.UseCors();
aplicacion.UseSwagger();
aplicacion.UseSwaggerUI();
aplicacion.MapControllers();
aplicacion.MapGet("/api/health", (IRelojNegocio reloj) => Results.Ok(new { estado = "ok", fecha = reloj.Hoy, moneda = "CLP", decimales = 0 }));
using (var alcance = aplicacion.Services.CreateScope())
{
    var baseDatos = alcance.ServiceProvider.GetRequiredService<ContextoFacturas>();
    if (args.Contains("--script-bd"))
    {
        var migrador = baseDatos.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        Console.WriteLine(migrador.GenerateScript());
        return;
    }

    await baseDatos.Database.MigrateAsync();
}

aplicacion.Run();
public partial class Program
{
}
