using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ApiFacturas.Infraestructura.Persistencia.Migraciones;
[DbContext(typeof(ContextoFacturas))]
public sealed class ContextoFacturasSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelo) => ModeloInicial20261108.Construir(modelo);
}

internal static class ModeloInicial20261108
{
    internal static void Construir(ModelBuilder modelo)
    {
        modelo.HasAnnotation("ProductVersion", "8.0.22");
        modelo.Entity("ApiFacturas.Dominio.Factura", entidad =>
        {
            entidad.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            entidad.Property<int>("NumeroFactura").HasColumnType("INTEGER");
            entidad.Property<DateOnly>("FechaFactura").HasColumnType("TEXT");
            entidad.Property<DateOnly>("FechaVencimiento").HasColumnType("TEXT");
            entidad.Property<decimal>("MontoTotal").HasConversion<ConversorPesos>().HasColumnType("INTEGER");
            entidad.Property<bool>("EsConsistente").HasColumnType("INTEGER");
            entidad.Property<string>("RunCliente").IsRequired().HasColumnType("TEXT");
            entidad.Property<string>("NombreCliente").IsRequired().HasColumnType("TEXT");
            entidad.Property<string>("CorreoCliente").IsRequired().HasColumnType("TEXT");
            entidad.Property<DateOnly?>("FechaPago").HasColumnType("TEXT");
            entidad.Property<string?>("MetodoPago").HasColumnType("TEXT");
            entidad.Property<string>("HuellaOrigen").IsRequired().HasColumnType("TEXT");
            entidad.Property<string>("JsonOrigen").IsRequired().HasColumnType("TEXT");
            entidad.HasKey("Id");
            entidad.ToTable("Facturas");
            entidad.HasIndex("NumeroFactura").IsUnique();
        });
        modelo.Entity("ApiFacturas.Dominio.LineaFactura", entidad =>
        {
            entidad.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            entidad.Property<int>("FacturaId").HasColumnType("INTEGER");
            entidad.Property<string>("NombreProducto").IsRequired().HasColumnType("TEXT");
            entidad.Property<decimal>("PrecioUnitario").HasConversion<ConversorPesos>().HasColumnType("INTEGER");
            entidad.Property<int>("Cantidad").HasColumnType("INTEGER");
            entidad.Property<decimal>("Subtotal").HasConversion<ConversorPesos>().HasColumnType("INTEGER");
            entidad.HasKey("Id");
            entidad.ToTable("Lineas");
            entidad.HasIndex("FacturaId");
        });
        modelo.Entity("ApiFacturas.Dominio.NotaCredito", entidad =>
        {
            entidad.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            entidad.Property<int>("FacturaId").HasColumnType("INTEGER");
            entidad.Property<long>("NumeroNotaCredito").HasColumnType("INTEGER");
            entidad.Property<DateOnly>("FechaCreacion").HasColumnType("TEXT");
            entidad.Property<decimal>("Monto").HasConversion<ConversorPesos>().HasColumnType("INTEGER");
            entidad.HasKey("Id");
            entidad.ToTable("NotasCredito");
            entidad.HasIndex("FacturaId", "NumeroNotaCredito").IsUnique();
        });
        modelo.Entity("ApiFacturas.Aplicacion.Contratos.FilaFactura", entidad =>
        {
            entidad.Property<int>("Id").HasColumnType("INTEGER");
            entidad.Property<int>("NumeroFactura").HasColumnType("INTEGER");
            entidad.Property<DateOnly>("FechaFactura").HasColumnType("TEXT");
            entidad.Property<DateOnly>("FechaVencimiento").HasColumnType("TEXT");
            entidad.Property<decimal>("MontoTotal").HasConversion<ConversorPesos>().HasColumnType("INTEGER");
            entidad.Property<decimal>("TotalDetalle").HasConversion<ConversorPesos>().HasColumnType("INTEGER");
            entidad.Property<decimal>("MontoAcreditado").HasConversion<ConversorPesos>().HasColumnType("INTEGER");
            entidad.Property<int>("CantidadNotasCredito").HasColumnType("INTEGER");
            entidad.Property<bool>("EsConsistente").HasColumnType("INTEGER");
            entidad.Property<string>("RunCliente").IsRequired().HasColumnType("TEXT");
            entidad.Property<string>("NombreCliente").IsRequired().HasColumnType("TEXT");
            entidad.Property<string>("CorreoCliente").IsRequired().HasColumnType("TEXT");
            entidad.Property<DateOnly?>("FechaPago").HasColumnType("TEXT");
            entidad.Property<string?>("MetodoPago").HasColumnType("TEXT");
            entidad.HasNoKey();
            entidad.ToView("v_proyeccion_facturas");
        });
        modelo.Entity("ApiFacturas.Dominio.LineaFactura", entidad => entidad.HasOne("ApiFacturas.Dominio.Factura", null).WithMany("Lineas").HasForeignKey("FacturaId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelo.Entity("ApiFacturas.Dominio.NotaCredito", entidad => entidad.HasOne("ApiFacturas.Dominio.Factura", null).WithMany("NotasCredito").HasForeignKey("FacturaId").OnDelete(DeleteBehavior.Cascade).IsRequired());
    }
}
