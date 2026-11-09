using ApiFacturas.Aplicacion.Contratos;

namespace ApiFacturas.Aplicacion.Importacion;
public sealed class FachadaImportacion(ILectorFacturasJson lector, ValidadorImportacion validador, IRepositorioFacturas repositorio)
{
    public async Task<ResultadoImportacion> ImportarAsync(Stream archivo, CancellationToken cancelacion)
    {
        var origenes = await lector.LeerAsync(archivo, cancelacion);
        var lote = validador.Validar(origenes);
        return await repositorio.ImportarLoteAsync(lote, cancelacion);
    }
}
