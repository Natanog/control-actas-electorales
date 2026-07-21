# Estado de implementación del MVP

## Implementado en `feat/actas-electorales-mvp`

### Backend

- Entidad `Acta` con estados, metadatos, SHA-256, GPS opcional y cadena de custodia.
- Resultados estructurados con origen OCR o manual.
- Regla de una sola acta primaria por elección, junta y dignidad.
- Migración PostgreSQL.
- Carga segura de JPG, PNG y WEBP con límite de 10 MB.
- Rechazo de duplicados exactos por hash.
- Listado filtrable para revisión.
- Dashboard de estados.
- Flujo de revisión: guardar, validar, observar, duplicar y marcar conflicto.
- Validación aritmética de candidatos + blancos + nulos frente al total.
- Integración OCR desacoplada.

### OCR

- Microservicio FastAPI con PaddleOCR.
- Health check.
- Corrección documental y orientación mediante PaddleOCR.
- Texto bruto, confianza y extracción inicial de filas con números.
- Derivación automática a revisión manual cuando no hay estructura o confianza suficiente.
- Docker integrado al stack local.

### Aplicación móvil

- Pestaña de Actas.
- Selección controlada de junta asignada.
- Captura con cámara trasera.
- Vista previa y repetición de fotografía.
- Dignidad y observaciones.
- Cola persistente en AsyncStorage.
- Sincronización automática al recuperar conexión.
- Reintento manual.
- Confirmación con código, estado y hash corto.

### Dashboard web

- Nueva pestaña Actas dentro de la elección.
- Métricas de recibidas, validadas, revisión y observadas/conflictos.
- Tabla de actas con junta, dignidad, estado, confianza OCR y hash.
- Advertencia visible sobre resultados no oficiales.

## Pendiente para cierre de producción

1. Ejecutar build y pruebas en un runner con acceso a Docker, .NET, Node y Expo.
2. Corregir cualquier incompatibilidad heredada que aparezca en ese build.
3. Cargar una muestra real del acta ecuatoriana y definir zonas/campos.
4. Crear catálogo oficial de dignidades y candidaturas, reemplazando `ContestCode` libre.
5. Incorporar visor con zoom y formulario completo de revisión en el dashboard.
6. Añadir exportaciones CSV y pruebas automatizadas específicas.
7. Configurar almacenamiento S3/MinIO de producción, backups y retención.
8. Revisar protección de datos, permisos territoriales y política de publicación.

## Comandos de verificación previstos

```bash
cp .env.example .env
docker compose build
docker compose up -d
docker compose ps
curl http://localhost:8000/health
curl http://localhost:5000/health

cd web
npm install
npm run lint
npm run test
npm run build

cd ../mobile
npm install
npm run lint
npx expo export --platform web
```

La verificación no se ha ejecutado desde ChatGPT porque el entorno de ejecución de esta conversación no puede resolver ni clonar GitHub. Los cambios se realizaron directamente mediante la integración autenticada de GitHub.
