using System.Text;
using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Aplicacion.Importacion;
using ApiFacturas.Dominio;
using ApiFacturas.Infraestructura.Lectura;
using Xunit;

namespace ApiFacturas.Pruebas;
public sealed class PruebasFachada
{
    [Fact]
    public async Task FachadaDelegaLectorValidadorYRepositorio()
    {
        var repositorio = new RepositorioEspia();
        var fachada = new FachadaImportacion(new LectorFacturasJson(), new ValidadorImportacion(), repositorio);
        using var archivo = new MemoryStream(Encoding.UTF8.GetBytes(DatosPrueba.Documento(DatosPrueba.Factura())));
        var resultado = await fachada.ImportarAsync(archivo, default);
        Assert.Equal(1, resultado.Importadas);
        Assert.Equal(1, repositorio.Invocaciones);
        Assert.Equal(1, repositorio.Lote!.Facturas[0].NumeroFactura);
        Assert.True(repositorio.Lote.Facturas[0].EsConsistente);
    }

    [Fact]
    public async Task NoPersisteSiFallaValidacion()
    {
        var repositorio = new RepositorioEspia();
        var fachada = new FachadaImportacion(new LectorFacturasJson(), new ValidadorImportacion(), repositorio);
        var factura = DatosPrueba.Factura();
        factura.Remove("total_amount");
        using var archivo = new MemoryStream(Encoding.UTF8.GetBytes(DatosPrueba.Documento(factura)));
        await Assert.ThrowsAsync<ErrorNegocio>(() => fachada.ImportarAsync(archivo, default));
        Assert.Equal(0, repositorio.Invocaciones);
    }

    private sealed class RepositorioEspia : IRepositorioFacturas
    {
        public int Invocaciones { get; private set; }
        public LoteValidado? Lote { get; private set; }

        public Task<ResultadoImportacion> ImportarLoteAsync(LoteValidado lote, CancellationToken cancelacion)
        {
            Invocaciones++;
            Lote = lote;
            return Task.FromResult(new ResultadoImportacion(lote.Facturas.Count, 0, 0, [], []));
        }

        public Task<IReadOnlyList<FilaFactura>> LeerActivasAsync(CancellationToken cancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<FilaFactura>> LeerInconsistentesAsync(CancellationToken cancelacion) => throw new NotSupportedException();
        public Task<DetallePersistido?> LeerDetalleAsync(int numero, CancellationToken cancelacion) => throw new NotSupportedException();
        public Task<NotaCredito> AgregarNotaAsync(int numero, decimal monto, DateOnly fecha, CancellationToken cancelacion) => throw new NotSupportedException();
    }
}
