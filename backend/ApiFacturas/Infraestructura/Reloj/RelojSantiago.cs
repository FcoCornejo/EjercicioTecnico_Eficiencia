using ApiFacturas.Aplicacion.Contratos;

namespace ApiFacturas.Infraestructura.Reloj;
public sealed class RelojSantiago(TimeProvider proveedor, TimeZoneInfo zona) : IRelojNegocio
{
    public DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(proveedor.GetUtcNow(), zona).DateTime);
}
