const archivos = require("fs"),
  rutas = require("path"),
  { spawn: iniciarProceso } = require("child_process");
const { JSDOM } = require("../frontend/node_modules/jsdom");
const esbuild = require("../frontend/node_modules/esbuild");
(async () => {
  const raiz = rutas.resolve(__dirname, ".."),
    direccionApi = "http://127.0.0.1:5080",
    direccionInterfaz = "http://127.0.0.1:5173";
  const procesos = [];
  let entornoDom;
  const rutaTemporal = rutas.join(
    require("os").tmpdir(),
    "eficiencia-recorrido-" + Date.now() + ".db",
  );
  const esperar = async (condicion, mensaje) => {
    for (let intento = 0; intento < 200; intento++) {
      if (await condicion()) return;
      await new Promise((resolver) => setTimeout(resolver, 30));
    }
    throw Error(mensaje);
  };
  try {
    procesos.push(
      iniciarProceso(
        process.env.DOTNET_EJECUTABLE || "dotnet",
        [
          rutas.join(
            raiz,
            "backend/ApiFacturas/bin/Debug/net8.0/ApiFacturas.dll",
          ),
        ],
        {
          cwd: raiz,
          env: {
            ...process.env,
            ASPNETCORE_URLS: direccionApi,
            ConnectionStrings__Facturas:
              "Data Source=" + rutaTemporal + ";Default Timeout=5",
          },
          stdio: "ignore",
        },
      ),
    );
    procesos.push(
      iniciarProceso(
        process.execPath,
        [
          rutas.join(raiz, "frontend/node_modules/vite/bin/vite.js"),
          "--host",
          "127.0.0.1",
        ],
        {
          cwd: rutas.join(raiz, "frontend"),
          stdio: "ignore",
        },
      ),
    );
    await esperar(async () => {
      try {
        return (await fetch(direccionInterfaz + "/api/health")).ok;
      } catch {
        return false;
      }
    }, "Servidor o proxy no arranco");
    const compilado = await esbuild.build({
      entryPoints: [rutas.join(raiz, "frontend/src/principal.jsx")],
      bundle: true,
      write: false,
      format: "iife",
      loader: {
        ".css": "empty",
      },
      define: {
        "process.env.NODE_ENV": '"production"',
      },
    });
    const errores = [];
    entornoDom = new JSDOM('<!doctype html><div id="root"></div>', {
      url: direccionInterfaz,
      runScripts: "dangerously",
      pretendToBeVisual: true,
      beforeParse(ventana) {
        ventana.fetch = (ruta, opciones) =>
          ventana.__fallarRed
            ? Promise.reject(new TypeError("Fallo de red simulado"))
            : fetch(direccionInterfaz + ruta, opciones);
        ventana.AbortController = global.AbortController;
        ventana.FormData = global.FormData;
        ventana.File = global.File;
        ventana.HTMLDialogElement.prototype.showModal = function () {
          this.open = true;
        };
        ventana.addEventListener("error", (evento) =>
          errores.push(evento.message),
        );
      },
    });
    entornoDom.window.eval(compilado.outputFiles[0].text);
    const documento = entornoDom.window.document;
    await esperar(
      () =>
        documento
          .querySelector(".empty")
          ?.textContent.includes("No hay facturas"),
      "No muestra vacio inicial",
    );
    const selector = documento.querySelector("input[type=file]");
    function cargar() {
      Object.defineProperty(selector, "files", {
        value: [
          new File(
            [
              archivos.readFileSync(
                rutas.join(raiz, "data/bd_exam_invoices.json"),
              ),
            ],
            "datos.json",
            {
              type: "application/json",
            },
          ),
        ],
        configurable: true,
      });
      selector.dispatchEvent(
        new entornoDom.window.Event("change", {
          bubbles: true,
        }),
      );
    }
    cargar();
    await esperar(
      () => documento.querySelectorAll("tbody tr").length === 10,
      "No importa por UI",
    );
    if (!documento.querySelector(".metric.total").textContent.includes("48"))
      throw Error("Universo incorrecto");
    if (
      !documento
        .querySelector(".resumen-contexto")
        .textContent.includes("America/Santiago")
    )
      throw Error("Falta contexto de reporte");
    cargar();
    await esperar(
      () =>
        documento
          .querySelector(".notice.success")
          ?.textContent.includes("50 duplicadas"),
      "No reimporta sin duplicados",
    );
    await esperar(
      () => documento.querySelector('button[aria-label="Ver factura 2"]'),
      "No termina recarga tras reimportar",
    );
    documento.querySelector('button[aria-label="Ver factura 2"]').click();
    await esperar(
      () => documento.querySelector("dialog .credit-form"),
      "Falta NC para pagada",
    );
    if (!documento.querySelector("dialog .Paid"))
      throw Error("Fixture 2 no aparece pagada");
    const antes = (await (await fetch(direccionApi + "/api/invoices/2")).json())
      .factura.montoAcreditado;
    const escribir = (valor) => {
      const campo = documento.querySelector("dialog input");
      Object.getOwnPropertyDescriptor(
        entornoDom.window.HTMLInputElement.prototype,
        "value",
      ).set.call(campo, valor);
      campo.dispatchEvent(
        new entornoDom.window.Event("input", {
          bubbles: true,
        }),
      );
    };
    escribir("10");
    documento.querySelector("dialog input").dispatchEvent(
      new entornoDom.window.Event("change", {
        bubbles: true,
      }),
    );
    documento.querySelector("dialog form").dispatchEvent(
      new entornoDom.window.Event("submit", {
        bubbles: true,
        cancelable: true,
      }),
    );
    await esperar(async () => {
      const detalle = await (
        await fetch(direccionApi + "/api/invoices/2")
      ).json();
      return detalle.factura.montoAcreditado === antes + 10;
    }, "No crea NC pagada desde formulario");
    await esperar(
      () =>
        !documento.querySelector('button[aria-label="Cerrar detalle"]')
          .disabled,
      "No termina guardado",
    );
    documento.querySelector('button[aria-label="Cerrar detalle"]').click();
    const boton = (texto) =>
      [...documento.querySelectorAll("nav button")].find((elemento) =>
        elemento.textContent.includes(texto),
      );
    boton("Inconsistencias").click();
    await esperar(
      () => documento.querySelectorAll("tbody tr").length === 2,
      "Falla inconsistencias",
    );
    if (!documento.querySelector("tbody").textContent.includes("#0041"))
      throw Error("Falta 41");
    boton("Todas las facturas").click();
    await esperar(
      () => documento.querySelectorAll("tbody tr").length === 10,
      "Falla vuelta al listado",
    );
    documento.querySelector('button[aria-label="Página siguiente"]').click();
    await esperar(
      () => documento.querySelector("tbody").textContent.includes("#0011"),
      "Falla paginacion",
    );
    entornoDom.window.__fallarRed = true;
    boton("Vencidas").click();
    await esperar(
      () => documento.querySelector(".notice.error"),
      "Falta fallo de red",
    );
    if (documento.querySelector(".notice.success"))
      throw Error("Un error de red conserva confirmacion de exito");
    if (errores.length) throw Error(errores.join("\n"));
    console.log(
      "PASS: vacio -> importar -> 48 activas -> reimportar -> detalle de pagada -> NC por UI -> inconsistencias -> paginacion -> error de red sin exito aparente. API y proxy Vite reales; interfaz JSDOM.",
    );
  } finally {
    if (entornoDom) entornoDom.window.close();
    for (const proceso of procesos) proceso.kill();
    await Promise.all(
      procesos.map((proceso) =>
        proceso.exitCode !== null
          ? Promise.resolve()
          : new Promise((resolver) => proceso.once("exit", resolver)),
      ),
    );
    for (const sufijo of ["", "-wal", "-shm"]) {
      try {
        archivos.unlinkSync(rutaTemporal + sufijo);
      } catch {}
    }
  }
})().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
