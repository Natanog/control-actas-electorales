# Control de Actas Electorales — Plan del MVP

## Propósito

Adaptar Vote Monitor para operar como un sistema interno de captura, archivo, extracción, revisión y consolidación de actas electorales.

El sistema no reemplaza ni presenta como oficiales los resultados de la autoridad electoral.

> Sistema interno de control y contraste documental. No sustituye los resultados oficiales de la autoridad electoral.

## Principios de diseño

1. **Original inmutable:** la fotografía original del acta nunca se reemplaza ni elimina desde la aplicación.
2. **Trazabilidad completa:** toda captura, procesamiento, corrección y validación queda auditada.
3. **Offline primero:** el veedor debe poder capturar el acta aun sin conexión y sincronizar posteriormente.
4. **OCR asistido:** el OCR propone datos; no inventa campos faltantes y deriva a revisión manual cuando existe baja confianza.
5. **Una fuente validada por junta y dignidad:** pueden existir varias fotografías, pero solo un resultado principal validado por junta y dignidad.
6. **Separación de responsabilidades:** captura, OCR, revisión y consolidación son etapas independientes.

## Componentes existentes que se conservan

- Aplicación móvil Expo/React Native.
- Persistencia offline mediante WatermelonDB y React Query persistente.
- Gestión de observadores.
- Elecciones y rondas electorales.
- Recintos electorales y mesas/juntas.
- Captura de cámara y selección de archivo.
- Adjuntos con almacenamiento local o S3.
- Autenticación y autorización.
- Dashboard web React/Vite.
- PostgreSQL y Entity Framework Core.
- Auditoría y pruebas de integración.
- Docker Compose para ejecución local.

## Nuevos módulos

### 1. Registro de actas

Entidad principal `Acta`:

- Id.
- ElectionRoundId.
- PollingStationId.
- ElectionTypeId o ContestId.
- ObserverId.
- AttachmentId del archivo original.
- Sha256Hash.
- CapturedAt.
- UploadedAt.
- Latitude y Longitude opcionales.
- Status.
- CurrentVersion.
- CreatedAt y UpdatedAt.

Estados iniciales:

- Captured.
- Uploaded.
- Archived.
- OcrProcessing.
- OcrSuccess.
- ManualReview.
- Validated.
- Observed.
- Duplicate.
- Conflict.
- Closed.

### 2. OCR

Crear una interfaz desacoplada:

```csharp
public interface IActaOcrProvider
{
    Task<ActaOcrResult> ProcessAsync(
        Stream image,
        CancellationToken cancellationToken);
}
```

La primera implementación será un adaptador HTTP hacia un servicio independiente basado en PaddleOCR.

Resultado esperado:

- Texto bruto.
- Confianza global.
- Campos detectados.
- Votos detectados.
- Errores.
- Regiones o coordenadas del texto cuando estén disponibles.

El sistema debe conservar la respuesta bruta del proveedor para auditoría y mejora posterior.

### 3. Resultados estructurados

Entidad `ActaResultEntry`:

- ActaId.
- CandidateId opcional.
- Label.
- Value.
- Type: Candidate, Blank, Null, Total, Other.
- Source: OCR o Manual.
- Confidence opcional.
- CreatedAt y UpdatedAt.

### 4. Revisión manual

La bandeja debe permitir:

- Ver la imagen original con zoom.
- Consultar el texto OCR bruto.
- Revisar errores de validación.
- Editar valores por candidato.
- Registrar blancos, nulos y total.
- Guardar borrador.
- Validar.
- Marcar ilegible.
- Marcar duplicada.
- Marcar conflicto.
- Solicitar nueva fotografía.

### 5. Validación

Reglas mínimas:

- Todos los votos deben ser enteros no negativos.
- La suma de candidatos + blancos + nulos debe coincidir con el total cuando todos los campos estén presentes.
- El total no puede superar el número de electores registrados cuando este dato esté disponible.
- La candidatura debe corresponder a la dignidad y territorio.
- Una junta y dignidad no pueden tener más de un resultado principal validado.
- El mismo hash identifica un duplicado exacto.
- Dos actas distintas para la misma junta y dignidad con datos diferentes generan conflicto.

### 6. Dashboard

Indicadores iniciales:

- Actas esperadas.
- Actas recibidas.
- Actas validadas.
- Pendientes de OCR.
- Pendientes de revisión.
- Observadas.
- Duplicadas.
- En conflicto.
- Cobertura porcentual.
- Cobertura por territorio.
- Actas pendientes por veedor.
- Resultados internos por dignidad.

Todos los resultados deben mostrar la advertencia de carácter no oficial.

## Fases

### Fase 0 — Verificación de la base

- Ejecutar Docker Compose.
- Confirmar migraciones.
- Confirmar seed de administrador.
- Ejecutar pruebas del backend.
- Ejecutar pruebas web y móvil disponibles.
- Documentar fallos heredados del fork antes de agregar funcionalidad.

### Fase 1 — Dominio y persistencia

- Crear entidades `Acta`, `ActaOcrResult` y `ActaResultEntry`.
- Crear estados y reglas de transición.
- Crear migración.
- Crear endpoints mínimos de alta, consulta y estado.
- Calcular SHA-256 al confirmar la carga del archivo.
- Registrar auditoría.

### Fase 2 — Captura móvil

- Crear flujo de asignaciones de actas.
- Capturar imagen con cámara trasera.
- Guardar fecha, ubicación opcional y metadatos.
- Mantener captura pendiente en almacenamiento local.
- Sincronizar cuando vuelva la conexión.
- Mostrar confirmación y código de acta.

### Fase 3 — OCR y validación

- Crear servicio PaddleOCR independiente.
- Crear adaptador `IActaOcrProvider`.
- Procesar archivos archivados.
- Guardar texto bruto y confianza.
- Aplicar validaciones matemáticas y territoriales.
- Enviar casos dudosos a revisión manual.

### Fase 4 — Revisión manual

- Crear listado de pendientes.
- Crear visor de imagen.
- Crear formulario de resultados.
- Crear acciones de validación y observación.
- Registrar todas las modificaciones en auditoría.

### Fase 5 — Dashboard y exportación

- Crear métricas de cobertura.
- Crear filtros territoriales.
- Crear consolidado interno.
- Exportar actas y resultados en CSV.
- Crear reporte de pendientes, observadas y conflictos.

## Criterios de aceptación del MVP

1. Un veedor puede capturar una foto de acta sin conexión.
2. La captura se sincroniza cuando existe conexión.
3. El servidor conserva el archivo original y su hash SHA-256.
4. El sistema crea un registro de acta vinculado a elección, junta, dignidad y veedor.
5. El OCR procesa la imagen o registra claramente su fallo.
6. El sistema deriva a revisión manual por baja confianza o inconsistencia.
7. Un revisor puede corregir y validar resultados.
8. El sistema impide dos resultados principales validados para la misma junta y dignidad.
9. El dashboard muestra cobertura y estados con datos reales.
10. El administrador puede exportar actas y resultados.
11. Toda acción crítica queda auditada.

## Decisiones pendientes

- Modelo definitivo de acta del CNE para entrenar extracción por regiones.
- Dignidades incluidas en la primera elección piloto.
- Territorio piloto.
- Proveedor final de almacenamiento en producción.
- Política de retención y respaldo.
- Umbral inicial de confianza OCR.
- Reglas específicas según el formato oficial del acta.
