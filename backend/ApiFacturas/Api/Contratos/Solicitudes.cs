using System.ComponentModel.DataAnnotations;

namespace ApiFacturas.Api.Contratos;
public sealed record SolicitudCredito([Required] decimal? Monto);
