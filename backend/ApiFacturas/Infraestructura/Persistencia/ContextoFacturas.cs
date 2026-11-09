using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ApiFacturas.Infraestructura.Persistencia;
public sealed class ContextoFacturas(DbContextOptions<ContextoFacturas> opciones) : DbContext(opciones)
{
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<LineaFactura> Lineas => Set<LineaFactura>();
    public DbSet<NotaCredito> NotasCredito => Set<NotaCredito>();
    public DbSet<FilaFactura> Proyecciones => Set<FilaFactura>();

    protected override void ConfigureConventions(ModelConfigurationBuilder constructor) => constructor.Properties<decimal>().HaveConversion<ConversorPesos>();
    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Factura>().HasIndex(factura => factura.NumeroFactura).IsUnique();
        modelo.Entity<Factura>().HasMany(factura => factura.Lineas).WithOne().HasForeignKey(linea => linea.FacturaId).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<Factura>().HasMany(factura => factura.NotasCredito).WithOne().HasForeignKey(nota => nota.FacturaId).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<NotaCredito>().HasIndex(nota => new { nota.FacturaId, nota.NumeroNotaCredito }).IsUnique();
        modelo.Entity<FilaFactura>().HasNoKey().ToView("v_proyeccion_facturas");
    }
}

public sealed class ConversorPesos() : ValueConverter<decimal, long>(valor => Convertir(valor), valor => valor)
{
    private static long Convertir(decimal valor) => Dinero.EsValido(valor) ? checked((long)valor) : throw new InvalidOperationException("Se intento persistir un monto fuera de la escala CLP entera.");
}
