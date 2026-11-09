using ApiFacturas.Aplicacion.Contratos;
using ApiFacturas.Dominio;

namespace ApiFacturas.Aplicacion.Creditos;
public sealed class RegistrarNotaCredito(IRepositorioFacturas repositorio, IRelojNegocio reloj)
{
    public Task<NotaCredito> EjecutarAsync(int numero, decimal monto, CancellationToken cancelacion)
    {
        Dinero.ExigirMontoCredito(monto);
        return repositorio.AgregarNotaAsync(numero, monto, reloj.Hoy, cancelacion);
    }
}
