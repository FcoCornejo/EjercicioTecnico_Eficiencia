import React, { useEffect, useRef, useState } from "react";
import { solicitar, formatearDinero } from "../../api/clienteApi";
import Insignia from "../../componentes/Insignia";
import FormularioNotaCredito from "../notasCredito/FormularioNotaCredito";
export default function DetalleFactura({ numero, alCerrar, alCambiar }) {
  const [datos, establecerDatos] = useState(null),
    [error, establecerError] = useState(""),
    [guardando, establecerGuardando] = useState(false);
  const referenciaDialogo = useRef(null);
  useEffect(() => {
    referenciaDialogo.current.showModal();
    const controlador = new AbortController();
    solicitar(`/invoices/${numero}`, {
      signal: controlador.signal,
    })
      .then(establecerDatos)
      .catch((errorRecibido) => {
        if (errorRecibido.name !== "AbortError")
          establecerError(errorRecibido.message);
      });
    return () => controlador.abort();
  }, [numero]);
  async function recargar() {
    alCambiar();
    try {
      establecerDatos(await solicitar(`/invoices/${numero}`));
    } catch {
      establecerError(
        "La NC fue guardada, pero no se pudo recargar el detalle. Cierra y vuelve a abrir la factura.",
      );
    }
  }
  return (
    <dialog
      ref={referenciaDialogo}
      onCancel={(evento) => {
        if (guardando) evento.preventDefault();
        else alCerrar();
      }}
      aria-labelledby="detail-title"
    >
      <div className="modal-heading">
        <div>
          <p className="eyebrow">DETALLE DE FACTURA</p>
          <h2 id="detail-title">Factura #{numero}</h2>
        </div>
        <button
          className="close"
          autoFocus
          disabled={guardando}
          aria-label="Cerrar detalle"
          onClick={alCerrar}
        >
          ×
        </button>
      </div>
      {error && (
        <p className="notice error" role="alert">
          {error}
        </p>
      )}
      {!datos && !error && <p role="status">Cargando detalle…</p>}
      {datos && (
        <>
          <div className="customer">
            <strong>{datos.factura.nombreCliente}</strong>
            <span>
              {datos.factura.runCliente} · {datos.factura.correoCliente}
            </span>
          </div>
          <div className="detail-metrics">
            <div>
              <span>Total</span>
              <strong>{formatearDinero(datos.factura.montoTotal)}</strong>
            </div>
            <div>
              <span>NC acumuladas</span>
              <strong>{formatearDinero(datos.factura.montoAcreditado)}</strong>
            </div>
            <div>
              <span>Saldo para NC</span>
              <strong>{formatearDinero(datos.factura.saldoPendiente)}</strong>
            </div>
          </div>
          <p>
            <Insignia valor={datos.factura.estadoFactura} />{" "}
            <Insignia valor={datos.factura.estadoPago} />
          </p>
          <p className="muted">
            Emisión: {datos.factura.fechaFactura} · Vencimiento:{" "}
            {datos.factura.fechaVencimiento}
          </p>
          {datos.factura.fechaPago && (
            <p className="muted">
              Pago registrado: {datos.factura.fechaPago} ·{" "}
              {datos.factura.metodoPago || "Sin método"}
            </p>
          )}
          <h3>Productos</h3>
          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Producto</th>
                  <th>Cantidad</th>
                  <th>Precio unitario</th>
                  <th>Subtotal</th>
                </tr>
              </thead>
              <tbody>
                {datos.lineas.map((linea) => (
                  <tr key={linea.id}>
                    <td>{linea.nombreProducto}</td>
                    <td>{linea.cantidad}</td>
                    <td>{formatearDinero(linea.precioUnitario)}</td>
                    <td>{formatearDinero(linea.subtotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <h3>
            Notas de crédito{" "}
            <span className="count">{datos.notasCredito.length}</span>
          </h3>
          {datos.notasCredito.length ? (
            <ul className="notes">
              {datos.notasCredito.map((nota) => (
                <li key={nota.id}>
                  <span>
                    NC #{nota.numeroNotaCredito}{" "}
                    <small>{nota.fechaCreacion}</small>
                  </span>
                  <strong>{formatearDinero(nota.monto)}</strong>
                </li>
              ))}
            </ul>
          ) : (
            <p className="muted">Sin notas de crédito.</p>
          )}
          <FormularioNotaCredito
            numero={numero}
            saldo={datos.factura.saldoPendiente}
            alGuardar={recargar}
            alOcupar={establecerGuardando}
          />
        </>
      )}
    </dialog>
  );
}
