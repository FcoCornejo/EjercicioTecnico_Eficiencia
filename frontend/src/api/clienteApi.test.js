import { afterEach, describe, expect, it, vi } from "vitest";
import { solicitar, validarCredito } from "./clienteApi";
afterEach(() => vi.unstubAllGlobals());
describe("validacion CLP entera", () => {
  it.each(["", "abc", "0", "-1", "0.1", "1.5", "101", "1000000000001"])(
    "rechaza %s",
    (valor) => expect(validarCredito(valor, 100)).not.toBe(""),
  );
  it.each(["1", "20", "100"])("acepta %s", (valor) =>
    expect(validarCredito(valor, 100)).toBe(""),
  );
});
describe("Errores HTTP y red", () => {
  it("devuelve JSON cuando hay exito", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          total: 1,
        }),
      }),
    );
    expect(await solicitar("/invoices")).toEqual({
      total: 1,
    });
  });
  it("muestra ProblemDetails del servidor", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 409,
        json: async () => ({
          title: "Conflicto de importacion",
        }),
      }),
    );
    await expect(solicitar("/imports")).rejects.toThrow(
      "Conflicto de importacion",
    );
  });
  it("muestra errores de validacion", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 400,
        json: async () => ({
          errors: {
            Monto: ["Monto requerido"],
          },
        }),
      }),
    );
    await expect(solicitar("/x")).rejects.toThrow("Monto requerido");
  });
  it("no convierte un fallo de red en exito", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockRejectedValue(new TypeError("Fallo de red")),
    );
    await expect(solicitar("/imports")).rejects.toThrow("Fallo de red");
  });
  it("tolera un cuerpo no JSON y conserva HTTP", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 503,
        json: async () => {
          throw new Error();
        },
      }),
    );
    await expect(solicitar("/x")).rejects.toThrow("503");
  });
});
