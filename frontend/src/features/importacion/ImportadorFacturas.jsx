import React, { useRef, useState } from "react";
import { solicitar } from "../../api/clienteApi";
export default function ImportadorFacturas({ alImportar, alError }) {
  const referencia = useRef(null);
  const [importando, establecerImportando] = useState(false);
  async function importar(evento) {
    const archivo = evento.target.files?.[0];
    if (!archivo) return;
    if (archivo.size > 10_000_000) {
      alError("El archivo supera 10 MB.");
      evento.target.value = "";
      return;
    }
    const formulario = new FormData();
    formulario.append("archivo", archivo);
    establecerImportando(true);
    alError("");
    try {
      const resultado = await solicitar("/imports", {
        method: "POST",
        body: formulario,
      });
      alImportar(resultado);
    } catch (error) {
      alError(error.message);
    } finally {
      establecerImportando(false);
      evento.target.value = "";
    }
  }
  return (
    <div>
      <input
        ref={referencia}
        type="file"
        className="sr-only"
        accept=".json,application/json"
        onChange={importar}
        aria-label="Seleccionar archivo JSON"
        disabled={importando}
      />
      <button
        className="primary import-button"
        onClick={() => referencia.current.click()}
        disabled={importando}
      >
        {importando ? "Importando…" : "↑ Importar JSON"}
      </button>
    </div>
  );
}
