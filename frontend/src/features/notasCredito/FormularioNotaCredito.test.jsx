import React, { act } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import FormularioNotaCredito from "./FormularioNotaCredito";
let contenedor, raiz;
beforeEach(() => {
  globalThis.IS_REACT_ACT_ENVIRONMENT = true;
  contenedor = document.createElement("div");
  document.body.append(contenedor);
  raiz = createRoot(contenedor);
});
afterEach(async () => {
  await act(async () => raiz.unmount());
  contenedor.remove();
  vi.unstubAllGlobals();
});
async function mostrar(saldo = 100) {
  await act(async () =>
    raiz.render(
      <FormularioNotaCredito
        numero={1}
        saldo={saldo}
        alGuardar={vi.fn()}
        alOcupar={vi.fn()}
      />,
    ),
  );
}
async function escribir(valor) {
  const campo = contenedor.querySelector("input");
  await act(async () => {
    Object.getOwnPropertyDescriptor(
      HTMLInputElement.prototype,
      "value",
    ).set.call(campo, valor);
    campo.dispatchEvent(
      new Event("input", {
        bubbles: true,
      }),
    );
  });
}
async function enviar() {
  await act(async () =>
    contenedor.querySelector("form").dispatchEvent(
      new Event("submit", {
        bubbles: true,
        cancelable: true,
      }),
    ),
  );
}
it("rechazo de fraccion sin llamar a la API", async () => {
  const enviarHttp = vi.fn();
  vi.stubGlobal("fetch", enviarHttp);
  await mostrar();
  await escribir("1.5");
  await enviar();
  expect(contenedor.textContent).toContain("sin decimales");
  expect(enviarHttp).not.toHaveBeenCalled();
});
it("error del servidor visible sin confirmacion falsa", async () => {
  const enviarHttp = vi.fn().mockResolvedValue({
    ok: false,
    status: 409,
    json: async () => ({
      title: "Saldo cambiado",
    }),
  });
  vi.stubGlobal("fetch", enviarHttp);
  await mostrar();
  await escribir("20");
  await enviar();
  expect(contenedor.textContent).toContain("Saldo cambiado");
  expect(enviarHttp).toHaveBeenCalledTimes(1);
});
it("saldo cero no ofrece una nueva NC", async () => {
  await mostrar(0);
  expect(contenedor.querySelector("form")).toBeNull();
  expect(contenedor.textContent).toContain("No hay saldo");
});
