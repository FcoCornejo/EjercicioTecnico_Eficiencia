CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE Facturas (
 Id INTEGER NOT NULL CONSTRAINT PK_Facturas PRIMARY KEY AUTOINCREMENT,
 NumeroFactura INTEGER NOT NULL, FechaFactura TEXT NOT NULL, FechaVencimiento TEXT NOT NULL,
 MontoTotal INTEGER NOT NULL, EsConsistente INTEGER NOT NULL,
 RunCliente TEXT NOT NULL, NombreCliente TEXT NOT NULL, CorreoCliente TEXT NOT NULL,
 FechaPago TEXT NULL, MetodoPago TEXT NULL, HuellaOrigen TEXT NOT NULL, JsonOrigen TEXT NOT NULL
);
CREATE UNIQUE INDEX IX_Facturas_NumeroFactura ON Facturas (NumeroFactura);
CREATE TABLE Lineas (
 Id INTEGER NOT NULL CONSTRAINT PK_Lineas PRIMARY KEY AUTOINCREMENT,
 FacturaId INTEGER NOT NULL, NombreProducto TEXT NOT NULL, PrecioUnitario INTEGER NOT NULL,
 Cantidad INTEGER NOT NULL, Subtotal INTEGER NOT NULL,
 CONSTRAINT FK_Lineas_Facturas_FacturaId FOREIGN KEY (FacturaId) REFERENCES Facturas (Id) ON DELETE CASCADE
);
CREATE INDEX IX_Lineas_FacturaId ON Lineas (FacturaId);
CREATE TABLE NotasCredito (
 Id INTEGER NOT NULL CONSTRAINT PK_NotasCredito PRIMARY KEY AUTOINCREMENT,
 FacturaId INTEGER NOT NULL, NumeroNotaCredito INTEGER NOT NULL, FechaCreacion TEXT NOT NULL, Monto INTEGER NOT NULL,
 CONSTRAINT FK_NotasCredito_Facturas_FacturaId FOREIGN KEY (FacturaId) REFERENCES Facturas (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_NotasCredito_FacturaId_NumeroNotaCredito ON NotasCredito (FacturaId, NumeroNotaCredito);
CREATE VIEW v_proyeccion_facturas AS
WITH detalle AS (
 SELECT FacturaId, SUM(Subtotal) AS TotalDetalle FROM Lineas GROUP BY FacturaId
), creditos AS (
 SELECT FacturaId, SUM(Monto) AS MontoAcreditado, COUNT(*) AS CantidadNotasCredito FROM NotasCredito GROUP BY FacturaId
)
SELECT factura.Id, factura.NumeroFactura, factura.FechaFactura, factura.FechaVencimiento,
 factura.MontoTotal, factura.EsConsistente, factura.RunCliente, factura.NombreCliente, factura.CorreoCliente,
 factura.FechaPago, factura.MetodoPago, COALESCE(detalle.TotalDetalle,0) AS TotalDetalle,
 COALESCE(creditos.MontoAcreditado,0) AS MontoAcreditado, COALESCE(creditos.CantidadNotasCredito,0) AS CantidadNotasCredito
FROM Facturas factura
LEFT JOIN detalle ON detalle.FacturaId = factura.Id
LEFT JOIN creditos ON creditos.FacturaId = factura.Id;
CREATE VIEW v_facturas_activas AS SELECT * FROM v_proyeccion_facturas WHERE EsConsistente = 1;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261108140000_Inicial', '8.0.22');

COMMIT;


