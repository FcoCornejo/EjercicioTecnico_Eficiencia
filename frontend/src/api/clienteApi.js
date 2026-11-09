async function solicitar(ruta, opciones = {}) {
  const respuesta = await fetch(`/api${ruta}`, opciones);
  const cuerpo = await respuesta.json().catch(() => null);
  if (!respuesta.ok) {
    const validacion = cuerpo?.errors
      ? Object.values(cuerpo.errors).flat().join(" ")
      : null;
    throw new Error(
      validacion || cuerpo?.title || `Error HTTP ${respuesta.status}`,
    );
  }
  return cuerpo;
}
export { solicitar };
function validarCredito(valor, saldoPendiente) {
  const monto = Number(valor);
  if (!valor || !Number.isFinite(monto) || monto <= 0)
    return "Ingresa un monto mayor a cero.";
  if (!Number.isInteger(monto)) return "Usa pesos enteros, sin decimales.";
  if (monto > 1_000_000_000_000) return "El monto supera el limite admitido.";
  if (monto > saldoPendiente) return "El monto supera el saldo pendiente.";
  return "";
}
export { validarCredito };
const formatearDinero = (valor) =>
  new Intl.NumberFormat("es-CL", {
    style: "currency",
    currency: "CLP",
    maximumFractionDigits: 0,
  }).format(valor);
export { formatearDinero };
const etiquetas = {
  Issued: "Emitida",
  Partial: "Parcial",
  Cancelled: "Anulada",
  Paid: "Pagada",
  Pending: "Pendiente",
  Overdue: "Vencida",
};
export { etiquetas };
