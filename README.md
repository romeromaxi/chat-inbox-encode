# Chat Inbox Encode

Bandeja conversacional para recibir mensajes de texto enviados a un bot de Telegram y mostrarlos en tiempo real a un operador.

## Estado actual

El circuito entrante está implementado:

1. La API registra automáticamente el webhook de Telegram al iniciar.
2. `POST /webhooks/telegram` valida `X-Telegram-Bot-Api-Secret-Token`.
3. Sólo acepta mensajes de texto de chats privados; otros tipos de update se ignoran con respuesta `200`.
4. La API publica el mensaje en RabbitMQ con publisher confirms, publicación mandatory y mensaje persistente.
5. El Worker consume con acknowledgement manual, crea o actualiza contacto y conversación, y persiste el mensaje en PostgreSQL.
6. El Worker publica una actualización de conversación en RabbitMQ.
7. La API consume esa actualización y emite `conversationUpdated` mediante SignalR en `/hubs/inbox`.
8. Angular recibe el evento y actualiza el listado visible en tiempo real.

También están implementados los Dockerfiles de API, Worker, Migrator y frontend, la migración EF Core `InitialCreate`, su ejecución one-shot desde Compose, los endpoints de health y 14 tests unitarios.

El backend expone listado de conversaciones, filtro unread, historial y marcado como leído. Angular carga las conversaciones persistidas al iniciar, abre su historial y aplica el marcado optimista con reversión ante error.

Todavía no están implementados:

- el saludo automático aprobado;
- el envío manual de respuestas desde la bandeja;
- los estados salientes `Pending`, `Sent` y `Failed`;
- reintentos y DLQ para mensajes salientes;

## Contratos implementados

### HTTP y SignalR

| Contrato | Comportamiento actual |
| --- | --- |
| `POST /webhooks/telegram` | Valida el secret header, filtra texto privado y publica el evento entrante. |
| `/hubs/inbox` | Hub SignalR consumido por Angular. |
| `conversationUpdated` | Evento backend → frontend con conversación, contacto, último mensaje y contador no leído. |
| `GET /health/live` | Liveness del proceso API. |
| `GET /health/ready` | Readiness de PostgreSQL y RabbitMQ. |
| `GET /api/conversations` | Lista conversaciones; acepta `unreadOnly=true|false`, default `false`. |
| `GET /api/conversations/{conversationId}/messages` | Devuelve historial ascendente o `404`. |
| `POST /api/conversations/{conversationId}/read` | Marca leído sin body; devuelve `204` o `404`. |

El webhook responde:

- `401` si falta el header secreto, aparece más de una vez o no coincide;
- `400` si el JSON o los datos obligatorios son inválidos;
- `200` para texto privado válido después de confirmar la publicación en RabbitMQ;
- `200` para updates ignorados, incluidos chats no privados, mensajes sin texto y mensajes editados.

### RabbitMQ

La topología actual usa un exchange topic durable y colas durables:

| Tipo | Nombre | Routing key |
| --- | --- | --- |
| Exchange | `chat-inbox` | — |
| Entrada Telegram | `chat-inbox.telegram.incoming-text` | `telegram.message.received` |
| Actualización para API | `chat-inbox.api.conversation-updates` | `conversation.message.received` |

Los mensajes publicados son persistentes, usan `MessageId`, publisher confirms y `mandatory: true`. Los consumidores trabajan con prefetch `1` y acknowledgement manual. Esta topología implementa recepción y actualización realtime; la topología saliente hacia Telegram aún no existe.

### Registro del webhook

La API llama a Telegram `setWebhook` durante el arranque con:

- `${NGROK_RESERVED_URL}/webhooks/telegram`;
- `secret_token`;
- `allowed_updates: ["message"]`.

El registro intenta hasta cinco veces con demoras de `1s`, `2s`, `4s` y `8s`. Si no puede registrarse, el arranque de la API falla. No existe todavía reconciliación periódica después de un arranque exitoso.

## Requisitos previos

- Docker Engine o Docker Desktop con Docker Compose.
- .NET SDK `10.0.402`; `src/backend/global.json` deshabilita roll-forward.
- Node.js compatible con el lockfile de Angular: `^22.22.3`, `^24.15.0` o `>=26.0.0`.
- npm.
- Un bot de Telegram creado mediante BotFather.
- Una cuenta ngrok con authtoken y URL HTTPS reservada.

## 1. Verificar el toolchain

```powershell
docker compose version
Push-Location .\src\backend
dotnet --version
Pop-Location
node --version
npm --version
```

Dentro de `src/backend`, `dotnet --version` debe informar `10.0.402`.

## 2. Crear y completar `.env`

```powershell
Copy-Item .env.example .env
```

En bash:

```bash
cp .env.example .env
```

| Variable | Obligatoria | Propósito |
| --- | --- | --- |
| `WEB_PORT` | Sí | Puerto local de la aplicación web. |
| `API_PORT` | Sí | Puerto local de API para health, diagnóstico y clientes como APIDog; publicado sólo en loopback. |
| `POSTGRES_IMAGE` | Sí | Imagen PostgreSQL. |
| `POSTGRES_DATABASE` | Sí | Nombre de la base de datos. |
| `POSTGRES_USER` | Sí | Usuario de PostgreSQL. |
| `POSTGRES_PASSWORD` | Sí | Contraseña local de PostgreSQL. |
| `POSTGRES_PORT` | Desarrollo local | Puerto PostgreSQL publicado por `docker-compose.dev.yml` sólo en loopback. |
| `RABBITMQ_IMAGE` | Sí | Imagen RabbitMQ. |
| `RABBITMQ_USER` | Sí | Usuario de RabbitMQ. |
| `RABBITMQ_PASSWORD` | Sí | Contraseña de RabbitMQ. |
| `RABBITMQ_PORT` | Desarrollo local | Puerto AMQP publicado sólo en loopback. |
| `RABBITMQ_MANAGEMENT_PORT` | Sí | Puerto de RabbitMQ Management publicado sólo en loopback por el Compose base. |
| `NGINX_IMAGE` | Sí | Imagen del ingreso exclusivo del webhook. |
| `NGROK_IMAGE` | Sí | Imagen del agente ngrok. |
| `NGROK_AUTHTOKEN` | Sí | Authtoken de la cuenta ngrok. |
| `NGROK_RESERVED_URL` | Sí | URL HTTPS reservada completa, sin `/` final. |
| `TELEGRAM_BOT_TOKEN` | Sí | Token generado por BotFather. |
| `TELEGRAM_WEBHOOK_SECRET_TOKEN` | Sí | Secreto para `setWebhook`; sólo admite `A-Z`, `a-z`, `0-9`, `_` y `-`. |

El `.env` real está ignorado por Git. Nunca subir tokens, contraseñas ni cadenas de conexión.

## 3. Crear el bot con BotFather

1. Abrir el chat oficial `@BotFather`.
2. Ejecutar `/newbot`.
3. Elegir un nombre visible y un username disponible terminado en `bot`.
4. Guardar el token entregado en `TELEGRAM_BOT_TOKEN`.
5. No exponer ese token en commits, capturas, issues ni logs.

## 4. Configurar ngrok

1. Reservar un dominio HTTPS estable desde la cuenta ngrok.
2. Copiar el authtoken en `NGROK_AUTHTOKEN`.
3. Copiar la URL completa, por ejemplo `https://example.ngrok.app`, en `NGROK_RESERVED_URL`.
4. No agregar `/webhooks/telegram`; Compose deriva la URL completa.

ngrok apunta a `webhook-ingress`, no directamente a la API. El ingreso sólo reenvía `POST /webhooks/telegram` y rechaza el resto de las rutas públicas.

## 5. Restaurar, compilar y ejecutar tests

### Backend

```powershell
dotnet restore .\src\backend\ChatInbox.sln
dotnet build .\src\backend\ChatInbox.sln --configuration Release --no-restore
dotnet test .\src\backend\ChatInbox.sln --configuration Release --no-build
```

El suite actual contiene 14 tests: siete casos del mapper de updates de Telegram, cuatro del validador de secret, uno del registro del webhook y dos del dominio de conversaciones.

### Frontend

```powershell
Push-Location .\src\frontend\chat-inbox-web
npm ci
npm run build
Pop-Location
```

No existe actualmente un comando de tests de frontend en `package.json`.

## 6. Validar Compose

Topología base:

```powershell
docker compose --env-file .env config --quiet
```

Topología con puertos de infraestructura publicados sólo en `127.0.0.1`:

```powershell
docker compose --env-file .env -f docker-compose.yml -f docker-compose.dev.yml config --quiet
```

## 7. Primera ejecución

Después de completar `.env`, toda la solución se construye, migra y levanta con un único comando:

```powershell
docker compose up --build
```

Durante el primer arranque ocurre esta secuencia:

1. Docker descarga las imágenes base y compila `web`, `api`, `worker` y `migrator`.
2. PostgreSQL y RabbitMQ esperan hasta estar saludables.
3. El servicio one-shot `migrator` ejecuta `Database.MigrateAsync()` y aplica `InitialCreate`.
4. `migrator` finaliza con código `0`; verlo como `Exited (0)` es normal.
5. API y Worker arrancan sólo después de esa finalización exitosa.
6. API registra el webhook en Telegram y queda healthy cuando PostgreSQL y RabbitMQ responden.
7. Web e ingreso público esperan el health de API; ngrok conecta la URL reservada con el ingreso.

En ejecuciones posteriores, EF Core detecta las migraciones ya aplicadas y `migrator` finaliza sin modificar el esquema. Si `migrator` falla, API y Worker no arrancan.

La cadena interna usa `Host=postgres;Port=5432`: API, Worker y Migrator se conectan al contenedor `postgres` de este Compose, no a una instancia PostgreSQL instalada en Windows. `POSTGRES_USER`, `POSTGRES_PASSWORD` y `POSTGRES_DATABASE` se aplican dentro de ese contenedor cuando se inicializa el volumen. Si se configura `POSTGRES_USER=postgres`, ese es el usuario que se crea o utiliza dentro del contenedor.

Las variables de inicialización sólo actúan al crear un volumen PostgreSQL vacío. Cambiar usuario, contraseña o base en `.env` no reconfigura un volumen ya inicializado.

Para dejar los contenedores en segundo plano:

```powershell
docker compose up --build --detach
docker compose ps --all
```

No hace falta instalar `dotnet-ef`, construir una cadena de conexión en la terminal ni ejecutar una migración manual para el flujo Docker Compose.

## 8. URLs

| Recurso | URL |
| --- | --- |
| Bandeja web | `http://127.0.0.1:${WEB_PORT}`; con `.env.example`, `http://127.0.0.1:8080` |
| API local | `http://127.0.0.1:${API_PORT}`; con `.env.example`, `http://127.0.0.1:5080` |
| Webhook público | `${NGROK_RESERVED_URL}/webhooks/telegram` |
| SignalR interno | `/hubs/inbox`, proxificado por la web |
| RabbitMQ Management | `http://127.0.0.1:${RABBITMQ_MANAGEMENT_PORT}`; con `.env.example`, `http://127.0.0.1:15672` |

La consola RabbitMQ Management se autentica con el valor de `RABBITMQ_USER` como usuario y `RABBITMQ_PASSWORD` como contraseña. Compose toma ambos valores de `.env`; no es necesario imprimirlos en logs ni copiarlos a otro archivo.

En `Queues and Streams` se pueden inspeccionar `chat-inbox.telegram.incoming-text` y `chat-inbox.api.conversation-updates`. Es normal ver `0` mensajes listos: Worker y API consumen y confirman los eventos casi inmediatamente. RabbitMQ muestra colas, consumidores y tasas de publicación/entrega; el historial funcional queda persistido en PostgreSQL.

La API local y Management están ligados a `127.0.0.1`, por lo que no escuchan en interfaces externas. RabbitMQ usa una red dedicada no interna para publicar exclusivamente Management; el tráfico AMQP permanece en su red interna. PostgreSQL, el puerto AMQP de RabbitMQ y la interfaz local de inspección de ngrok no publican puertos al host en la topología base.

Una instalación PostgreSQL de Windows no participa del flujo base. Sólo puede existir un conflicto de puerto al usar `docker-compose.dev.yml`; en ese caso, elegir otro `POSTGRES_PORT` en `.env` o detener el servicio local que ocupa ese puerto.

Para diagnóstico local que requiera PostgreSQL o AMQP, usar el override opcional:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

Ese override agrega PostgreSQL y AMQP únicamente en `127.0.0.1`; no duplica el puerto de Management ya publicado por el Compose base.

## 9. Diagnóstico básico

Ver estado de todos los servicios, incluido el proceso one-shot:

```powershell
docker compose ps --all
```

Verificar la migración y seguir los componentes del flujo entrante:

```powershell
docker compose logs migrator
docker compose logs --follow api worker webhook-ingress ngrok
```

Estados esperados:

- `migrator` en `Exited (0)`: migraciones aplicadas correctamente.
- `postgres` y `rabbitmq` en `healthy`.
- `api` en `healthy`.
- `web`, `worker`, `webhook-ingress` y `ngrok` en ejecución.

Si `migrator` termina con un código distinto de `0`, revisar primero `docker compose logs migrator postgres`; API y Worker permanecerán sin iniciar. Si API reinicia, revisar `docker compose logs api rabbitmq postgres`, especialmente configuración Telegram y el registro `setWebhook`.

### Health

La API publica health sólo en loopback:

```powershell
Invoke-WebRequest http://127.0.0.1:5080/health/live
Invoke-WebRequest http://127.0.0.1:5080/health/ready
```

Si se cambió `API_PORT`, reemplazar `5080` por el valor configurado. Ambas solicitudes deben responder `200`. `ready` comprueba PostgreSQL y RabbitMQ.

### Webhook registrado

Para consultar Telegram sin copiar ni imprimir el token configurado en `.env`, ejecutar desde el contenedor API:

```powershell
docker compose exec -T api sh -c 'curl --fail --silent --show-error "https://api.telegram.org/bot${Telegram__BotToken}/getWebhookInfo"'
```

El campo `result.url` debe coincidir con `NGROK_RESERVED_URL` más `/webhooks/telegram`. Revisar también `pending_update_count` y `last_error_message`.

### Importar la API en APIDog

1. Levantar la solución con `docker compose up --build`.
2. En APIDog elegir la importación desde archivo OpenAPI/Swagger.
3. Seleccionar [`docs/api/openapi.yaml`](docs/api/openapi.yaml).
4. Usar `http://127.0.0.1:5080` como server local. Si se cambió `API_PORT`, editar el environment de APIDog con el puerto configurado.
5. Ejecutar primero `/health/live` y `/health/ready`.
6. Generar al menos una conversación enviando texto privado al bot o simulando el webhook.
7. Probar listado, `unreadOnly`, historial y marcado como leído.

Los endpoints de health y conversaciones no requieren autenticación. Para probar `POST /webhooks/telegram`, configurar en APIDog el valor de `TELEGRAM_WEBHOOK_SECRET_TOKEN` como API key del header `X-Telegram-Bot-Api-Secret-Token`; no guardar ni compartir el secreto fuera del environment local. El archivo describe health, webhook y los contratos HTTP de conversaciones ya implementados.

## 10. Simular un webhook local

Abrir primero `http://127.0.0.1:${WEB_PORT}` para observar la actualización SignalR. Luego enviar un update único a través del proxy web local:

PowerShell no exporta `.env` automáticamente. Antes del ejemplo, cargar en la sesión los mismos valores configurados para `WEB_PORT` y `TELEGRAM_WEBHOOK_SECRET_TOKEN`, sin escribir el secret en el historial:

```powershell
$env:WEB_PORT = Read-Host "WEB_PORT"
$env:TELEGRAM_WEBHOOK_SECRET_TOKEN = Read-Host "TELEGRAM_WEBHOOK_SECRET_TOKEN"
```

```powershell
$headers = @{
    "X-Telegram-Bot-Api-Secret-Token" = $env:TELEGRAM_WEBHOOK_SECRET_TOKEN
}

$payload = @{
    update_id = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    message = @{
        message_id = [int]([DateTimeOffset]::UtcNow.ToUnixTimeSeconds() % [int]::MaxValue)
        date = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        chat = @{
            id = 123456789
            type = "private"
            first_name = "Local"
        }
        from = @{
            id = 123456789
            is_bot = $false
            first_name = "Local"
            username = "local_test"
        }
        text = "Mensaje de prueba"
    }
} | ConvertTo-Json -Depth 8

Invoke-WebRequest `
    -Method Post `
    -Uri "http://127.0.0.1:$($env:WEB_PORT)/webhooks/telegram" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $payload
```

Ejemplo con curl:

```bash
curl -i -X POST "http://127.0.0.1:${WEB_PORT}/webhooks/telegram" \
  -H "Content-Type: application/json" \
  -H "X-Telegram-Bot-Api-Secret-Token: ${TELEGRAM_WEBHOOK_SECRET_TOKEN}" \
  --data '{"update_id":1000001,"message":{"message_id":1001,"date":1700000000,"chat":{"id":123456789,"type":"private","first_name":"Local"},"from":{"id":123456789,"is_bot":false,"first_name":"Local","username":"local_test"},"text":"Mensaje de prueba"}}'
```

Resultado esperado:

1. HTTP `200` después del publisher confirm.
2. Un mensaje en `chat-inbox.telegram.incoming-text` consumido por Worker.
3. Persistencia de contacto, conversación y mensaje en PostgreSQL.
4. Publicación en `chat-inbox.api.conversation-updates`.
5. Evento SignalR `conversationUpdated`.
6. Conversación visible en la web si estaba conectada antes del evento.

Para verificar persistencia:

```powershell
docker compose --env-file .env -f docker-compose.yml -f docker-compose.dev.yml exec -T postgres `
    psql -U $env:POSTGRES_USER -d $env:POSTGRES_DATABASE `
    -c 'SELECT "Text", "TelegramSentAt", "ReceivedAt" FROM messages ORDER BY "Id" DESC LIMIT 5;'
```

Usar un `update_id` diferente en cada prueba. El backend trata `TelegramUpdateId` como idempotency key y la base posee un índice único.

## 11. Probar de punta a punta con Telegram

1. Abrir la web antes de enviar el mensaje.
2. Enviar texto desde un chat privado al bot.
3. Confirmar HTTP sin errores en logs de API.
4. Confirmar consumo y persistencia en logs de Worker.
5. Verificar que la conversación aparece en tiempo real en Angular.
6. Comprobar que `TelegramSentAt` y `ReceivedAt` se almacenan por separado.
7. Repetir un update simulado con el mismo `update_id` y comprobar que no se duplica en PostgreSQL.

No intentar todavía responder desde la bandeja ni validar el saludo automático: esos flujos permanecen pendientes. El listado, historial y marcado como leído pueden probarse tanto desde la interfaz como mediante APIDog.

## Decisiones aprobadas todavía pendientes de implementación

- El saludo inicial será exactamente: “Hola, ¿en qué podemos ayudarte?”.
- El saludo se enviará una sola vez ante el primer texto válido de un contacto nuevo.
- Las respuestas del operador usarán `Pending`, `Sent` y `Failed`.
- `LastMessageAt` se actualizará al crear `Pending`; `Sent` sólo cambiará estado y `Failed` conservará la posición.
- Los reintentos salientes serán a `1s` y `3s`, ambos con jitter `±20%`; después se usará DLQ y `Failed`.
- La conversación activa leída permanecerá visible con contador `0` en el filtro unread hasta cambiar de conversación.

Los ADR aceptados se enumeran en [docs/decisions/README.md](docs/decisions/README.md). Las decisiones aún sin resolver están en [docs/architecture/open-decisions.md](docs/architecture/open-decisions.md).

## Estructura del repositorio

```text
src/backend/                 Solución .NET y proyectos de Clean Architecture
src/frontend/chat-inbox-web/ Aplicación Angular y proxy nginx
infra/ingress/               Proxy público exclusivo para el webhook
docs/architecture/           Arquitectura y decisiones pendientes
docs/decisions/              ADR aceptados y plantilla
docs/api/openapi.yaml         Contrato OpenAPI importable en APIDog
tests/backend/               Tests unitarios del webhook
```

## Persistencia local

PostgreSQL y RabbitMQ usan volúmenes Docker con nombre. Reiniciar o reconstruir contenedores conserva datos, usuarios y credenciales iniciales del volumen.

`docker compose down --volumes` elimina de forma irreversible los datos locales, el esquema migrado, los usuarios de PostgreSQL y el estado del broker. En el siguiente `docker compose up --build`, PostgreSQL se inicializa nuevamente con el `.env` vigente y `migrator` vuelve a aplicar `InitialCreate`.

