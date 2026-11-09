import React from "react";
import { etiquetas } from "../../api/clienteApi";
export default function ResumenPagos({ resumen }) {
  return (
    <section className="metrics" aria-label="Resumen por estado de pago">
      <article className="metric total">
        <span>Facturas activas</span>
        <strong>{resumen.total}</strong>
      </article>
      {["Paid", "Pending", "Overdue"].map((estado) => {
        const elemento = resumen.elementos.find(
          (fila) => fila.estado === estado,
        );
        return (
          <article key={estado} className="metric">
            <span>
              <span className={`dot ${estado}`} />
              {etiquetas[estado]}s
            </span>
            <strong>{elemento?.cantidad ?? 0}</strong>
            <small>{elemento?.porcentaje ?? 0}%</small>
          </article>
        );
      })}
    </section>
  );
}
