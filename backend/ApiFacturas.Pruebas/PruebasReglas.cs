using ApiFacturas.Dominio;
using ApiFacturas.Infraestructura.Reloj;
using Xunit;

namespace ApiFacturas.Pruebas;
public sealed class PruebasReglas
{
    [Theory]
    [InlineData(100, 100, true)]
    [InlineData(100, 99, false)]
    [InlineData(0, 0, true)]
    public void ConsistenciaExacta(decimal total, decimal detalle, bool esperado) => Assert.Equal(esperado, ReglasFactura.EsConsistente(total, detalle));
    [Theory]
    [InlineData(100, 0, 0, "Issued")]
    [InlineData(100, 40, 1, "Partial")]
    [InlineData(100, 100, 2, "Cancelled")]
    [InlineData(0, 0, 0, "Issued")]
    public void EstadoIndependienteDeEtiqueta(decimal total, decimal credito, int notas, string esperado) => Assert.Equal(esperado, ReglasFactura.Calcular(total, credito, notas, null, new(2026, 11, 8), new(2026, 11, 8)).EstadoFactura);
    [Theory]
    [InlineData(-1, false, "Overdue")]
    [InlineData(0, false, "Pending")]
    [InlineData(1, false, "Pending")]
    [InlineData(-50, true, "Paid")]
    public void PagoYLimitesDeFecha(int desplazamiento, bool pagada, string esperado)
    {
        var hoy = new DateOnly(2026, 11, 8);
        var estado = ReglasFactura.Calcular(100, 0, 0, pagada ? hoy : null, hoy.AddDays(desplazamiento), hoy);
        Assert.Equal(esperado, estado.EstadoPago);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.1)]
    [InlineData(1.5)]
    public void RechazaCeroNegativoOFraccion(decimal monto) => Assert.Equal(TipoError.Validacion, Assert.Throws<ErrorNegocio>(() => ReglasFactura.ExigirSaldo(monto, 100, 0)).Tipo);
    [Fact]
    public void AceptaLimiteExactoYRechazaExceso()
    {
        ReglasFactura.ExigirSaldo(60, 100, 40);
        Assert.Equal(TipoError.Conflicto, Assert.Throws<ErrorNegocio>(() => ReglasFactura.ExigirSaldo(61, 100, 40)).Tipo);
    }

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, false)]
    [InlineData(31, true)]
    public void LimiteVencidas(int dias, bool esperado) => Assert.Equal(esperado, ReglasFactura.IncluirEnVencidas(new("Issued", "Overdue", 100, dias), false, 0));
    [Fact]
    public void ExcluyePagoONotas()
    {
        var estado = new EstadoCalculado("Issued", "Overdue", 100, 31);
        Assert.False(ReglasFactura.IncluirEnVencidas(estado, true, 0));
        Assert.False(ReglasFactura.IncluirEnVencidas(estado, false, 1));
    }

    [Fact]
    public void UsaDiaCivilDeSantiago()
    {
        var proveedor = new ProveedorFijo(new DateTimeOffset(2026, 11, 8, 1, 30, 0, TimeSpan.Zero));
        var reloj = new RelojSantiago(proveedor, TimeZoneInfo.FindSystemTimeZoneById("America/Santiago"));
        Assert.Equal(new DateOnly(2026, 11, 7), reloj.Hoy);
    }

    private sealed class ProveedorFijo(DateTimeOffset instante) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instante;
    }
}
