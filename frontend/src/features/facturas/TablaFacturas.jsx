import React from "react";
import Insignia from "../../componentes/Insignia";
import { formatearDinero } from "../../api/clienteApi";
export default function TablaFacturas({
  filas,
  alSeleccionar,
  inconsistentes = false,
}) {
  return (
    <div className="table-scroll">
      <table>
        <thead>
          <tr>
            <th>Factura / cliente</th>
            <th>Vencimiento</th>
            <th>Total</th>
            {inconsistentes ? (
              <>
                <th>Suma de productos</th>
                <th>Diferencia</th>
              </>
            ) : (
              <>
                <th>Factura</th>
                <th>Pago</th>
                <th>Saldo</th>
                <th>
                  <span className="sr-only">Acción</span>
                </th>
              </>
            )}
          </tr>
        </thead>
        <tbody>
          {filas.map((filaFactura) => (
            <tr key={filaFactura.numeroFactura}>
              <td>
                <strong>
                  #{String(filaFactura.numeroFactura).padStart(4, "0")}
                </strong>
                <span className="subcell">{filaFactura.nombreCliente}</span>
              </td>
              <td>{filaFactura.fechaVencimiento}</td>
              <td className="numeric">
                {formatearDinero(filaFactura.montoTotal)}
              </td>
              {inconsistentes ? (
                <>
                  <td className="numeric">
                    {formatearDinero(filaFactura.totalDetalle)}
                  </td>
                  <td className="difference">
                    {formatearDinero(
                      filaFactura.totalDetalle - filaFactura.montoTotal,
                    )}
                  </td>
                </>
              ) : (
                <>
                  <td>
                    <Insignia valor={filaFactura.estadoFactura} />
                  </td>
                  <td>
                    <Insignia valor={filaFactura.estadoPago} />
                  </td>
                  <td className="numeric">
                    {formatearDinero(filaFactura.saldoPendiente)}
                  </td>
                  <td>
                    <button
                      className="text-button"
                      onClick={() => alSeleccionar(filaFactura.numeroFactura)}
                      aria-label={`Ver factura ${filaFactura.numeroFactura}`}
                    >
                      Ver detalle ↗
                    </button>
                  </td>
                </>
              )}
            </tr>
          ))}
        </tbody>
      </table>
      {filas.length === 0 && (
        <div className="empty">No hay facturas para mostrar.</div>
      )}
    </div>
  );
}
