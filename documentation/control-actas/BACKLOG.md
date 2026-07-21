# Backlog inicial — Control de Actas Electorales

## P0 — Línea base

### CAE-001 Verificar ejecución local

**Objetivo:** confirmar que el fork compila y funciona antes de introducir cambios.

- Levantar Docker Compose.
- Confirmar PostgreSQL y migraciones.
- Configurar y ejecutar seed del administrador.
- Ejecutar pruebas del backend.
- Ejecutar build y pruebas web.
- Ejecutar lint/build móvil.
- Documentar fallos heredados.

**Aceptación:** existe una guía reproducible con comandos y resultados.

### CAE-002 Documentar arquitectura heredada

- Identificar bounded contexts y módulos existentes.
- Identificar entidades de elecciones, rondas, observadores, recintos, formularios y adjuntos.
- Mapear almacenamiento, autenticación, auditoría y trabajos en segundo plano.
- Identificar puntos de extensión para Actas y OCR.

**Aceptación:** documento de arquitectura con dependencias y decisiones de integración.

## P1 — Dominio de actas

### CAE-010 Crear entidad Acta

- Relación con elección, junta, dignidad, veedor y adjunto.
- Estados del ciclo de vida.
- Hash SHA-256.
- Metadatos de captura y carga.
- Restricciones de integridad.

### CAE-011 Crear resultados OCR y entradas estructuradas

- `ActaOcrResult`.
- `ActaResultEntry`.
- Fuente OCR o manual.
- Confianza y errores.
- Respuesta bruta del proveedor.

### CAE-012 Crear migración y configuración EF Core

- Configuraciones de entidades.
- Índices por junta, dignidad, estado y hash.
- Restricción lógica para resultado principal validado.
- Pruebas de persistencia.

### CAE-013 Crear máquina de estados

- Transiciones permitidas.
- Reglas de autorización.
- Auditoría en cada transición.
- Pruebas unitarias.

## P1 — API y archivos

### CAE-020 Registrar acta desde un adjunto

- Endpoint para iniciar registro.
- Validar usuario, elección, junta y dignidad.
- Vincular archivo original.
- Calcular SHA-256.
- Detectar duplicado exacto.

### CAE-021 Consultar actas

- Listado paginado.
- Filtros territoriales, estado, veedor y fecha.
- Detalle con metadatos, OCR y resultados.
- Autorización por rol y organización.

### CAE-022 Proteger archivo original

- No permitir reemplazo ni borrado desde la interfaz.
- Acceso autorizado mediante endpoint o URL temporal.
- Registrar consultas y exportaciones críticas.

## P1 — Captura móvil

### CAE-030 Pantalla de asignaciones

- Mostrar elección, recinto, junta y dignidad.
- Estados pendiente, capturada y sincronizada.
- Búsqueda y filtros mínimos.

### CAE-031 Capturar fotografía del acta

- Cámara trasera.
- Vista previa.
- Repetir fotografía.
- Fecha/hora.
- GPS opcional.
- Observación opcional.

### CAE-032 Cola offline

- Guardar archivo y metadatos localmente.
- Estado pendiente de sincronización.
- Reintento manual y automático.
- Evitar envíos duplicados.

### CAE-033 Confirmación de subida

- Código interno.
- Junta y dignidad.
- Hash corto.
- Estado de procesamiento.

## P1 — OCR

### CAE-040 Crear contrato IActaOcrProvider

- Contrato independiente del proveedor.
- Resultado tipado.
- Manejo de timeout, cancelación y errores.
- Implementación falsa para pruebas.

### CAE-041 Crear servicio PaddleOCR

- Servicio Python separado.
- Endpoint de salud.
- Endpoint de procesamiento.
- Dockerfile.
- Respuesta con texto, confianza y regiones.
- Licencia y atribución documentadas.

### CAE-042 Integrar cola de procesamiento

- Procesar actas archivadas.
- Estados OcrProcessing, OcrSuccess y ManualReview.
- Reintentos controlados.
- Idempotencia.

### CAE-043 Validar resultados extraídos

- Enteros no negativos.
- Suma de votos.
- Total frente a electores registrados.
- Candidato frente a dignidad y territorio.
- Umbral de confianza configurable.

## P1 — Revisión manual

### CAE-050 Crear bandeja de revisión

- Filtros y paginación.
- Estados ManualReview, Observed, Duplicate y Conflict.
- Prioridad por territorio y antigüedad.

### CAE-051 Crear detalle de revisión

- Visor de imagen con zoom.
- Texto OCR bruto.
- Errores de validación.
- Campos editables por candidatura.
- Blancos, nulos y total.

### CAE-052 Crear acciones del revisor

- Guardar borrador.
- Validar.
- Marcar ilegible.
- Marcar duplicada.
- Marcar conflicto.
- Solicitar nueva fotografía.

### CAE-053 Resolver unicidad del acta principal

- Solo una validada por junta y dignidad.
- Conservar todas las versiones.
- Selección explícita de versión principal.
- Auditoría de resolución.

## P2 — Dashboard

### CAE-060 Crear resumen operativo

- Esperadas, recibidas, validadas y pendientes.
- OCR exitoso.
- Observadas, duplicadas y conflictos.
- Cobertura porcentual.

### CAE-061 Crear filtros territoriales

- Provincia, cantón, parroquia, recinto y junta.
- Dignidad, estado, fecha y veedor.

### CAE-062 Crear consolidado interno

- Resultados por dignidad y candidatura.
- Desglose territorial.
- Solo actas principales validadas.
- Advertencia visible de resultados no oficiales.

### CAE-063 Crear exportaciones

- Actas CSV.
- Resultados CSV.
- Pendientes CSV.
- Observadas y conflictos CSV.

## P2 — Seguridad y operación

### CAE-070 Revisar autorización por rol

- Administrador.
- Coordinador.
- Veedor.
- Revisor.
- Comprobaciones en servidor.

### CAE-071 Revisar auditoría

- Captura.
- Carga.
- OCR.
- Correcciones.
- Validación.
- Cambios de estado.
- Exportaciones.

### CAE-072 Estrategia de respaldo y retención

- Base de datos.
- Archivos originales.
- Respuestas OCR.
- Pruebas de restauración.

## Dependencias externas pendientes

- Modelo oficial de acta.
- Catálogo territorial y juntas.
- Candidaturas y dignidades.
- Territorio piloto.
- Infraestructura de producción.
- Política legal de conservación y acceso.
