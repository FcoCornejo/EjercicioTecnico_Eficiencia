import React, { useEffect, useState } from "react";
import { solicitar } from "./api/clienteApi";
import TablaFacturas from "./features/facturas/TablaFacturas";
import DetalleFactura from "./features/facturas/DetalleFactura";
import FiltrosFacturas from "./features/facturas/FiltrosFacturas";
import ImportadorFacturas from "./features/importacion/ImportadorFacturas";
import ResumenPagos from "./features/reportes/ResumenPagos";
const filtrosVacios = {
  numero: "",
  estadoFactura: "",
  estadoPago: "",
};
const rutasReportes = {
  overdue: "/reports/overdue-without-payment-or-credit",
  inconsistent: "/reports/inconsistent-invoices",
};
export default function Aplicacion() {
  const [pestana, establecerPestana] = useState("invoices"),
    [filtros, establecerFiltros] = useState(filtrosVacios),
    [aplicados, establecerAplicados] = useState(filtrosVacios);
  const [pagina, establecerPagina] = useState(1),
    [datos, establecerDatos] = useState({
      elementos: [],
      total: 0,
    }),
    [resumen, establecerResumen] = useState({
      elementos: [],
      total: 0,
    });
  const [reporte, establecerReporte] = useState([]),
    [cargando, establecerCargando] = useState(true),
    [error, establecerError] = useState(""),
    [aviso, establecerAviso] = useState(""),
    [seleccionada, establecerSeleccionada] = useState(null),
    [revision, establecerRevision] = useState(0);
  useEffect(() => {
    const controlador = new AbortController();
    establecerCargando(true);
    establecerError("");
    const parametros = new URLSearchParams({
      pagina,
      tamanoPagina: 10,
    });
    Object.entries(aplicados).forEach(([clave, valor]) => {
      if (valor !== "") parametros.set(clave, valor);
    });
    Promise.all([
      solicitar("/reports/payment-status", {
        signal: controlador.signal,
      }),
      solicitar(
        pestana === "invoices"
          ? `/invoices?${parametros}`
          : rutasReportes[pestana],
        {
          signal: controlador.signal,
        },
      ),
    ])
      .then(([nuevoResumen, resultado]) => {
        establecerResumen(nuevoResumen);
        if (pestana === "invoices") establecerDatos(resultado);
        else establecerReporte(resultado);
      })
      .catch((errorRecibido) => {
        if (errorRecibido.name !== "AbortError") {
          establecerError(errorRecibido.message);
          establecerAviso("");
        }
      })
      .finally(() => {
        if (!controlador.signal.aborted) establecerCargando(false);
      });
    return () => controlador.abort();
  }, [pestana, aplicados, pagina, revision]);
  function limpiar() {
    establecerFiltros(filtrosVacios);
    establecerAplicados(filtrosVacios);
    establecerPagina(1);
  }
  function recargar() {
    establecerRevision((valor) => valor + 1);
  }
  function importada(resultado) {
    establecerAviso(
      `${resultado.importadas} importadas · ${resultado.inconsistentes} inconsistentes · ${resultado.duplicadasOmitidas} duplicadas omitidas. ${resultado.advertencias?.length ?? 0} advertencias de origen.`,
    );
    establecerPagina(1);
    recargar();
  }
  const filas = pestana === "invoices" ? datos.elementos : reporte;
  return (
    <>
      <main>
        <div className="page-heading">
          <div>
            <h1>
              Gestión de facturas<span className="accent">.</span>
            </h1>
          </div>
          <ImportadorFacturas
            alImportar={importada}
            alError={(mensaje) => {
              establecerError(mensaje);
              establecerAviso("");
            }}
          />
        </div>
        {error && (
          <div className="notice error" role="alert">
            {error}{" "}
            <button className="text-button" onClick={recargar}>
              Reintentar
            </button>
          </div>
        )}
        {aviso && (
          <div className="notice success" role="status">
            {aviso}
          </div>
        )}
        {!error && <ResumenPagos resumen={resumen} />}
        <section className="panel">
          <div className="panel-top">
            <nav aria-label="Secciones">
              {[
                ["invoices", "Todas las facturas"],
                ["overdue", "Vencidas +30 días"],
                ["inconsistent", "Inconsistencias"],
              ].map(([clave, titulo]) => (
                <button
                  key={clave}
                  className={pestana === clave ? "tab active" : "tab"}
                  onClick={() => {
                    establecerPestana(clave);
                    establecerPagina(1);
                  }}
                >
                  {titulo}
                </button>
              ))}
            </nav>
          </div>
          {pestana === "invoices" ? (
            <FiltrosFacturas
              filtros={filtros}
              alEditar={establecerFiltros}
              alBuscar={() => {
                establecerPagina(1);
                establecerAplicados({
                  ...filtros,
                });
              }}
              alLimpiar={limpiar}
            />
          ) : (
            <p className="report-description">
              {pestana === "overdue"
                ? "Consistentes, con más de 30 días vencidas, sin pago y sin ninguna NC."
                : "Documentos conservados fuera de la gestión activa. Diferencia = suma de productos menos total declarado."}
            </p>
          )}
          {cargando ? (
            <div className="empty" role="status">
              Cargando facturas…
            </div>
          ) : error ? (
            <div className="empty">No fue posible cargar los datos.</div>
          ) : (
            <TablaFacturas
              filas={filas}
              alSeleccionar={establecerSeleccionada}
              inconsistentes={pestana === "inconsistent"}
            />
          )}
          <div className="panel-footer">
            <span>
              {cargando
                ? "Actualizando…"
                : `${pestana === "invoices" ? datos.total : reporte.length} documentos encontrados`}
            </span>
            {pestana === "invoices" && (
              <div className="pagination">
                <button
                  aria-label="Página anterior"
                  onClick={() => establecerPagina((valor) => valor - 1)}
                  disabled={cargando || pagina === 1}
                >
                  ←
                </button>
                <span>
                  Página {pagina} de {Math.max(1, Math.ceil(datos.total / 10))}
                </span>
                <button
                  aria-label="Página siguiente"
                  onClick={() => establecerPagina((valor) => valor + 1)}
                  disabled={cargando || pagina * 10 >= datos.total}
                >
                  →
                </button>
              </div>
            )}
          </div>
        </section>
      </main>
      {seleccionada !== null && (
        <DetalleFactura
          key={seleccionada}
          numero={seleccionada}
          alCerrar={() => establecerSeleccionada(null)}
          alCambiar={() => {
            establecerAviso("Nota de crédito guardada.");
            recargar();
          }}
        />
      )}
    </>
  );
}
