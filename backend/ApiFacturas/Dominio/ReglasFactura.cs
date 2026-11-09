namespace ApiFacturas.Dominio;
public enum TipoError
{
    Validacion,
    NoEncontrado,
    Conflicto
}

public sealed class ErrorNegocio(TipoError tipo, string mensaje) : Exception(mensaje)
{
    public TipoError Tipo { get; } = tipo;
}

public static class Dinero
{
    public const decimal Maximo = 1_000_000_000_000m;
    public static bool EsValido(decimal valor) => valor >= 0 && valor <= Maximo && decimal.Truncate(valor) == valor;
    public static void ExigirMontoCredito(decimal monto)
    {
        if (!EsValido(monto) || monto <= 0)
            throw new ErrorNegocio(TipoError.Validacion, "El monto debe ser positivo, en pesos enteros y dentro del limite admitido.");
    }
}

public sealed record EstadoCalculado(string EstadoFactura, string EstadoPago, decimal SaldoPendiente, int DiasVencidos);
public static class ReglasFactura
{
    public static bool EsConsistente(decimal total, decimal sumaDetalle) => total == sumaDetalle;
    public static EstadoCalculado Calcular(decimal total, decimal acreditado, int cantidadNotas, DateOnly? fechaPago, DateOnly vencimiento, DateOnly hoy)
    {
        string estadoFactura = cantidadNotas == 0 ? "Issued" : acreditado == total ? "Cancelled" : "Partial";
        string estadoPago = fechaPago.HasValue ? "Paid" : vencimiento < hoy ? "Overdue" : "Pending";
        return new(estadoFactura, estadoPago, total - acreditado, Math.Max(0, hoy.DayNumber - vencimiento.DayNumber));
    }

    public static void ExigirSaldo(decimal monto, decimal total, decimal acreditado)
    {
        Dinero.ExigirMontoCredito(monto);
        if (monto > total - acreditado)
            throw new ErrorNegocio(TipoError.Conflicto, "El monto supera el saldo disponible para notas de credito.");
    }

    public static bool IncluirEnVencidas(EstadoCalculado estado, bool tienePago, int cantidadNotas) => !tienePago && cantidadNotas == 0 && estado.DiasVencidos > 30;
}
