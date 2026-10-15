# Sistema de Pedidos y Control de Inventario

Aplicación web interna para la administración de productos, registro de pedidos y control de existencias. Desarrollada como proyecto de portafolio para postulación a prácticas preprofesionales de desarrollo de software, priorizando código limpio, arquitectura por capas, transacciones atómicas y buenas prácticas con .NET y Angular.

---

## 1. Stack Tecnológico

### Backend
- **Lenguaje:** C# (.NET 10 LTS)
- **Framework Web:** ASP.NET Core Web API
- **ORM:** Entity Framework Core 10 (Code-First con Migraciones)
- **Base de Datos:** Microsoft SQL Server
- **Consultas:** LINQ y Fluent API
- **Documentación API:** Swagger / OpenAPI (Swashbuckle)
- **Manejo de Errores:** `IExceptionHandler` con especificación ProblemDetails (RFC 7807)
- **Pruebas:** xUnit, Microsoft.EntityFrameworkCore.Sqlite In-Memory

### Frontend
- **Framework:** Angular (Standalone Components)
- **Lenguaje:** TypeScript
- **Estilos y Maquetación:** Bootstrap 5 y Bootstrap Icons
- **Formularios:** Angular Reactive Forms con validaciones síncronas
- **Comunicación HTTP:** `HttpClient` con tipado estricto
- **Enrutamiento:** Angular Router con títulos dinámicos y navegación SPA

---

## 2. Arquitectura del Sistema

El backend mantiene una arquitectura por capas desacoplada y orientada a la mantenibilidad:

```
Angular ──────► Controllers ──────► Services ──────► EF Core DbContext ──────► SQL Server
(Frontend SPA)   (HTTP / DTOs)     (Reglas Negocio)   (Mapeo y Transacciones)   (Persistencia)
```

### Organización del Proyecto Backend
- `Controllers/`: Controladores REST (`ProductosController`, `PedidosController`, `InventarioController`). Exponen endpoints HTTP, validan el modelo y delegan la ejecución.
- `Services/`: Capa de negocio (`ProductosService`, `PedidosService`, `InventarioService`). Contiene las validaciones, cálculos financieros y transacciones atómicas.
- `Data/`: `ApplicationDbContext` con configuración Fluent API, token de concurrencia optimista y `DbInitializer` con datos semilla reproducibles.
- `Entities/`: Entidades persistentes del dominio (`Product`, `Order`, `OrderDetail`, `StockMovement`).
- `DTOs/`: Objetos de transferencia de entrada y salida (`ProductoDto`, `CrearPedidoDto`, `MovimientoStockDto`, etc.).
- `Exceptions/`: Excepciones de dominio (`BusinessRuleException`, `NotFoundException`) y middleware global `GlobalExceptionHandler`.

> **Decisión de Diseño:** No se implementó un patrón repositorio genérico sobre Entity Framework Core, dado que `DbContext` y `DbSet` ya proveen abstracción de repositorio y unidad de trabajo (*Unit of Work*). Esto evita sobreingeniería innecesaria en proyectos de este alcance.

---

## 3. Modelo de Entidades y Base de Datos

### Entidades Principales
1. **Product**: Identificador, nombre, descripción, precio unitario (`decimal(18,2)`), stock actual, estado activo y token de concurrencia (`RowVersion`).
2. **Order**: Identificador, fecha de creación (`DateTime.UtcNow`), total calculado en el servidor y estado (`Completado`).
3. **OrderDetail**: Identificador, clave foránea de pedido, clave foránea de producto, cantidad solicitada, precio unitario congelado y subtotal.
4. **StockMovement**: Auditoría de cada entrada y salida de mercancía con cantidad (+/-), tipo de movimiento (`INICIAL`, `VENTA_PEDIDO`, `AJUSTE`), fecha y pedido relacionado.

### Reglas de Integridad y Restricciones
- `DeleteBehavior.Restrict` en la relación Producto-Detalle para impedir el borrado físico de productos con pedidos históricos.
- Token de concurrencia optimista (`IsRowVersion()`) en `Product` para proteger contra sobreventas simultáneas.

---

## 4. Reglas de Negocio Implementadas

Todas las reglas de negocio residen en la capa `Services`:

1. **Precios y existencias no negativas:** No se permite registrar productos con precios o existencias menores o iguales a cero.
2. **Productos inactivos protegidos:** No se permite incluir productos inactivos en nuevos pedidos. Si un producto se discontinúa, pasa a baja lógica (`Active = false`) conservando su historial.
3. **Validación estricta de stock disponible:** Si un pedido solicita más unidades que las disponibles en almacén, la operación se detiene de inmediato.
4. **Cálculo obligatorio en servidor:** El total del pedido y los subtotales son calculados exclusivamente en el backend; no se aceptan montos monetarios calculados por el frontend.
5. **Precios históricos congelados:** Cada línea de pedido almacena el precio vigente al momento de la transacción. Si el precio del catálogo cambia en el futuro, los pedidos anteriores mantienen su valor real original.
6. **Transaccionalidad atómica (Rollback):** El registro del pedido, sus líneas de detalle, el descuento de existencias y los movimientos de inventario se ejecutan dentro de una transacción (`BeginTransactionAsync`). Si ocurre cualquier fallo, se revierte la operación completa.
7. **Control de concurrencia:** Si dos usuarios intentan comprar las últimas unidades en el mismo milisegundo, la base de datos detecta el conflicto y evita que el stock resulte negativo.

---

## 5. Endpoints de la API REST

### Productos (`/api/productos`)
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| `GET` | `/api/productos` | Obtiene el catálogo completo (filtro opcional `?soloActivos=true`). |
| `GET` | `/api/productos/{id}` | Obtiene el detalle de un producto específico por ID. |
| `POST` | `/api/productos` | Registra un nuevo producto y genera movimiento inicial de inventario. |
| `PUT` | `/api/productos/{id}` | Actualiza datos de un producto y registra ajuste si cambia el stock. |
| `PATCH` | `/api/productos/{id}/inactivar` | Realiza la baja lógica del producto (`Active = false`). |

### Pedidos (`/api/pedidos`)
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| `GET` | `/api/pedidos` | Obtiene el historial completo de pedidos ordenados cronológicamente. |
| `GET` | `/api/pedidos/{id}` | Obtiene el detalle completo de un pedido con sus líneas de compra. |
| `POST` | `/api/pedidos` | Registra atómicamente un nuevo pedido, descuenta existencias y crea auditoría. |

### Inventario (`/api/inventario`)
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| `GET` | `/api/inventario/movimientos` | Consulta el historial de movimientos de inventario (filtro opcional por `productoId` y `limite`). |
| `GET` | `/api/inventario/bajo-stock` | Lista los productos activos con existencias menores o iguales al umbral configurado (`?umbral=10`). |

---

## 6. Frontend Angular

La interfaz de usuario interna está organizada por módulos funcionales:

- **Dashboard:** Panel principal con tarjetas de métricas operativas (total productos, stock crítico, total pedidos, ventas totales), alerta de existencias bajas y últimos pedidos.
- **Catálogo de Productos:** Listado con buscador en tiempo real, filtro por estado (activos/inactivos), insignias de color según nivel de existencias y acciones rápidas.
- **Formulario de Producto:** Formulario reactivo (`ReactiveFormsModule`) para crear y editar productos con validaciones instantáneas.
- **Registrar Pedido:** Interfaz con selector interactivo de productos, validación reactiva de cantidades contra existencias disponibles, tabla canasta y confirmación transaccional.
- **Historial y Detalle de Pedidos:** Tabla de pedidos registrados con enlace a vista detallada donde se exhiben los precios congelados históricos y subtotales.
- **Control de Inventario:** Pestañas para monitorear productos con bajo stock con umbrales configurables (5, 10, 15, 20 unid.) y tabla de auditoría cronológica de movimientos (+/-).

---

## 7. Pruebas Automatizadas (xUnit)

El proyecto incluye 19 casos de prueba automatizados en `backend/SistemaPedidosInventario.Tests`, diferenciados por motor y propósito técnico:

### A. Pruebas Unitarias y de Reglas de Negocio (SQLite In-Memory)
Diseñadas para ejecución ultrarrápida, portable y sin dependencias externas en pipelines de CI/CD:
1. **`RegistrarPedido_ConDatosValidos_DescuentaStockYCalculaTotalCorrectamente`**: Verifica creación del pedido, descuento de existencias, cálculo del total y registro de auditoría `VENTA_PEDIDO`.
2. **`RegistrarPedido_ConProductoInexistente_LanzaNotFoundException`**: Control de referencias inexistentes.
3. **`RegistrarPedido_ConProductoInactivo_LanzaBusinessRuleException`**: Bloqueo de venta para productos descontinuados.
4. **`RegistrarPedido_ConStockInsuficiente_LanzaBusinessRuleException`**: Rechazo cuando la demanda supera el stock.
5. **`RegistrarPedido_ConCantidadInvalidaMenorOCero_LanzaBusinessRuleException`**: Validación para cantidades `<= 0`.
6. **`RegistrarPedido_CalculaSubtotalesYTotalCorrectamenteEnElServidor`**: Cálculo estricto de subtotales y total ignorando montos del cliente.
7. **`RegistrarPedido_CuandoFallaDespuesDeIniciarEscrituras_AplicaRollbackYRevierteCambiosFisicos`**: Utiliza un interceptor (`SaveChangesInterceptor`) que inyecta un error inmediatamente después de que la BD ejecutó físicamente los comandos SQL de escritura, comprobando que `RollbackAsync()` revierte los cambios y restaura el stock real.
8. **`RegistrarPedido_CuandoFallaEnValidacionPrevia_NoModificaStockNiRegistraDatos`**: Comprueba que errores previos de validación no ejecutan escrituras.
9. **`RegistrarPedido_Concurrente_PrevieneStockNegativo`**: Simula dos solicitudes simultáneas de 4 unidades sobre un stock disponible de 5, exigiendo que **exactamente una tenga éxito, la otra sea rechazada por causa esperada** (regla de negocio / conflicto) y el stock remanente final sea estrictamente 1.
10. **`CrearProducto_ConPrecioNegativoOCero_LanzaBusinessRuleException`**
11. **`CrearProducto_ConStockNegativo_LanzaBusinessRuleException`**
12. **`CrearProducto_Valido_GuardaProductoYRegistraMovimientoInicial`**
13. **`InactivarProducto_Existente_CambiaEstadoAFalso`**
14. **`ObtenerProductosBajoStock_FiltraProductosPorDebajoDeUmbral`**
15. **`ObtenerProductosBajoStock_ConUmbralNegativo_LanzaBusinessRuleException`**

### B. Pruebas de Integración Aisladas contra SQL Server (`RowVersion`)
Ubicadas de forma aislada en `PedidosSqlServerConcurrencyTests.cs`, utilizan una **base de datos exclusiva de pruebas (`SistemaPedidosInventario_TestDb`)** para no alterar la base de datos de desarrollo:
1. **`SqlServer_ConcurrenciaOptimistaConRowVersion_LanzaDbUpdateConcurrencyException`**: Carga el mismo producto en dos contextos separados con idéntico `RowVersion`. Al guardar el primero, SQL Server incrementa el timestamp de fila; al intentar guardar el segundo, la cláusula WHERE detecta el cambio de versión y EF Core arroja `DbUpdateConcurrencyException`.
2. **`SqlServer_PedidosConcurrentes_SoloUnoTieneExitoYOtroEsRechazadoPorConcurrencia`**: Dos pedidos concurrentes de 4 unidades contra 5 disponibles; verifica que la transacción atómica con `RowVersion` previene sobreventas en el motor relacional de producción.

Para ejecutar toda la suite de pruebas:
```bash
dotnet test
```

---

## 8. Topología Git Flow

El repositorio demuestra la estructura estándar de Git Flow:

- `main`: Código probado y listo para producción.
- `develop`: Rama principal de integración continua.
- `feature/fase1-backend-productos`: Entidades, DbContext, migraciones y API de productos.
- `feature/fase2-pedidos-inventario`: Lógica transaccional de pedidos, reglas de negocio y control de inventario.
- `feature/fase3-frontend-angular`: Interfaz de usuario completa en Angular con Bootstrap 5.
- `feature/fase4-pruebas-xunit`: Suite de pruebas unitarias y de integración de reglas de negocio.
- `release/v1.0.0`: Estabilización, documentación y preparación de entrega.

---

## 9. Instrucciones de Ejecución

### Prerrequisitos
- .NET 10 SDK
- Node.js (v20+) y npm
- Microsoft SQL Server (o SQL Server Express / LocalDB)

### Paso 1: Configurar la Base de Datos
Asegúrese de que el servicio de SQL Server esté iniciado.
La cadena de conexión predeterminada en `backend/SistemaPedidosInventario.Api/appsettings.json` es:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=SistemaPedidosInventarioDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```
*Al iniciar la API, Entity Framework Core aplica automáticamente las migraciones pendientes y carga los datos semilla de prueba.*

### Paso 2: Iniciar el Backend
```bash
dotnet run --project backend/SistemaPedidosInventario.Api
```
- Swagger UI disponible en: `http://localhost:5236/swagger`

### Paso 3: Iniciar el Frontend
En una nueva terminal:
```bash
cd frontend
npm start
```
- Aplicación Angular disponible en: `http://localhost:4200`

> "Configuramos una propiedad `RowVersion` con tipo de dato `rowversion` / `byte[]` en la entidad `Product` mediante Fluent API (`IsRowVersion()`). Cuando dos solicitudes concurrentes intentan actualizar el mismo producto simultáneamente, la primera que guarda actualiza la versión del registro. La segunda solicitud falla con `DbUpdateConcurrencyException`, la transacción se revierte y se notifica al usuario sin permitir que el stock baje de cero."
