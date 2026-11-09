import React, { useState } from "react";
import {
  solicitar,
  validarCredito,
  formatearDinero,
} from "../../api/clienteApi";
export default function FormularioNotaCredito({
  numero,
  saldo,
  alGuardar,
  alOcupar,
}) {
  const [monto, establecerMonto] = useState("");
  const [error, establecerError] = useState("");
  const [guardando, establecerGuardando] = useState(false);
  async function enviar(evento) {
    evento.preventDefault();
    const mensaje = validarCredito(monto, saldo);
    if (mensaje) {
      establecerError(mensaje);
      return;
    }
    establecerGuardando(true);
    alOcupar(true);
    establecerError("");
    try {
      await solicitar(`/invoices/${numero}/credit-notes`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          monto: Number(monto),
        }),
      });
      establecerMonto("");
      await alGuardar();
    } catch (errorRecibido) {
      establecerError(errorRecibido.message);
    } finally {
      establecerGuardando(false);
      alOcupar(false);
    }
  }
  if (saldo <= 0)
    return (
      <p className="notice">
        No hay saldo disponible para nuevas notas de crédito.
      </p>
    );
  return (
    <form onSubmit={enviar} className="credit-form">
      {error && (
        <p className="notice error" role="alert">
          {error}
        </p>
      )}
      <label htmlFor="monto">Nueva nota de crédito</label>
      <div className="credit-controls">
        <input
          id="monto"
          type="number"
          min="1"
          step="1"
          max={saldo}
          value={monto}
          onChange={(evento) => establecerMonto(evento.target.value)}
          required
          disabled={guardando}
          placeholder="Monto en CLP"
        />
        <button className="primary" disabled={guardando}>
          {guardando ? "Guardando…" : "Agregar NC"}
        </button>
      </div>
      <small>
        CLP sin decimales. Saldo para NC: {formatearDinero(saldo)}. La fecha la
        asigna el servidor en Santiago. Una factura pagada también puede recibir
        NC.
      </small>
    </form>
  );
}
