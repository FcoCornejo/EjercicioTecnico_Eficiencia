import React from "react";
import { etiquetas } from "../../api/clienteApi";
export default function FiltrosFacturas({
  filtros,
  alEditar,
  alBuscar,
  alLimpiar,
}) {
  return (
    <form
      className="filters"
      onSubmit={(evento) => {
        evento.preventDefault();
        alBuscar();
      }}
    >
      <label>
        N.º de factura
        <input
          aria-label="Número de factura"
          type="number"
          min="1"
          step="1"
          placeholder="Ej. 13"
          value={filtros.numero}
          onChange={(evento) =>
            alEditar({
              ...filtros,
              numero: evento.target.value,
            })
          }
        />
      </label>
      <label>
        Estado de factura
        <select
          value={filtros.estadoFactura}
          onChange={(evento) =>
            alEditar({
              ...filtros,
              estadoFactura: evento.target.value,
            })
          }
        >
          <option value="">Todos los estados</option>
          {["Issued", "Partial", "Cancelled"].map((estado) => (
            <option key={estado} value={estado}>
              {etiquetas[estado]}
            </option>
          ))}
        </select>
      </label>
      <label>
        Estado de pago
        <select
          value={filtros.estadoPago}
          onChange={(evento) =>
            alEditar({
              ...filtros,
              estadoPago: evento.target.value,
            })
          }
        >
          <option value="">Todos los pagos</option>
          {["Paid", "Pending", "Overdue"].map((estado) => (
            <option key={estado} value={estado}>
              {etiquetas[estado]}
            </option>
          ))}
        </select>
      </label>
      <button className="secondary">Buscar</button>
      <button type="button" className="text-button" onClick={alLimpiar}>
        Limpiar
      </button>
    </form>
  );
}
