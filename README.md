# Chat Inbox

Bandeja web para recibir, visualizar y responder en tiempo real mensajes de texto enviados a un bot de Telegram.

La solución se ejecuta con Docker Compose e incluye Angular, una API y un Worker en .NET, PostgreSQL, RabbitMQ, SignalR, ngrok y un proxy de ingreso para el webhook.

```text
Telegram → ngrok → API → RabbitMQ → Worker → PostgreSQL
                                      ↓
Angular ← SignalR ← API ← RabbitMQ ───┘
```

Los flujos de recepción y respuesta están implementados. 

## 1. Requisitos previos

- Docker Desktop o Docker Engine con Docker Compose v2.
- Una cuenta de Telegram y un bot creado con `@BotFather` mediante `/newbot`.
- Una cuenta de ngrok con authtoken y un dominio HTTPS reservado.
- Los puertos locales `8080`, `5080` y `15672` disponibles, o valores alternativos configurados en `.env`.

No es necesario instalar PostgreSQL, RabbitMQ, .NET ni Node.js para ejecutar la solución con Docker Compose.

Para compilar o ejecutar pruebas fuera de Docker se necesitan, de forma opcional:

- .NET SDK `10.0.402`.
- Una versión de Node.js compatible con el proyecto Angular y npm.

## 2. Configuración del `.env`

Crear el archivo local desde el ejemplo:

```powershell
Copy-Item .env.example .env
```

En bash:

```bash
cp .env.example .env
```

Completar `.env` con valores propios:

| Variable | Descripción |
| --- | --- |
| `COMPOSE_PROJECT_NAME` | Nombre del proyecto Docker Compose. Puede conservarse el valor del ejemplo. |
| `WEB_PORT` | Puerto local de la bandeja web. |
| `API_PORT` | Puerto local de la API. |
| `POSTGRES_IMAGE` | Imagen de PostgreSQL. Puede conservarse el valor del ejemplo. |
| `POSTGRES_DATABASE` | Nombre de la base creada dentro del contenedor. |
| `POSTGRES_USER` | Usuario de PostgreSQL. |
| `POSTGRES_PASSWORD` | Contraseña local elegida para PostgreSQL. |
| `RABBITMQ_IMAGE` | Imagen de RabbitMQ. Puede conservarse el valor del ejemplo. |
| `RABBITMQ_USER` | Usuario de RabbitMQ. |
| `RABBITMQ_PASSWORD` | Contraseña local elegida para RabbitMQ; no es un token. |
| `RABBITMQ_MANAGEMENT_PORT` | Puerto local de la consola de RabbitMQ. |
| `NGINX_IMAGE` | Imagen del proxy que recibe exclusivamente el webhook. |
| `NGROK_IMAGE` | Imagen del agente ngrok. |
| `NGROK_AUTHTOKEN` | Authtoken obtenido desde la cuenta de ngrok. |
| `NGROK_RESERVED_URL` | URL HTTPS reservada completa, sin `/` final ni `/webhooks/telegram`. |
| `TELEGRAM_BOT_TOKEN` | Token entregado por `@BotFather`. |
| `TELEGRAM_WEBHOOK_SECRET_TOKEN` | Secreto aleatorio usado para validar que el webhook proviene de Telegram. |

`POSTGRES_PORT` y `RABBITMQ_PORT` están reservadas en `.env.example`, pero el `docker-compose.yml` actual no publica PostgreSQL ni el puerto AMQP en el host.

El secreto del webhook debe contener únicamente letras, números, guion o guion bajo. Puede generarse en PowerShell con:

```powershell
(1..2 | ForEach-Object { [guid]::NewGuid().ToString("N") }) -join ""
```

Ejemplo de formato, sin credenciales reales:

```dotenv
POSTGRES_DATABASE=chat_inbox
POSTGRES_USER=postgres
POSTGRES_PASSWORD=replace-with-a-local-password

RABBITMQ_USER=chat_inbox
RABBITMQ_PASSWORD=replace-with-a-local-password

NGROK_AUTHTOKEN=replace-with-your-ngrok-authtoken
NGROK_RESERVED_URL=https://your-reserved-domain.ngrok-free.dev

TELEGRAM_BOT_TOKEN=replace-with-your-botfather-token
TELEGRAM_WEBHOOK_SECRET_TOKEN=replace-with-a-random-secret
```

El `.env` está excluido por `.gitignore`.

PostgreSQL se ejecuta dentro de Docker. API, Worker y Migrator utilizan internamente `postgres:5432`; no necesitan una conexión a una instalación local de PostgreSQL.

## 3. Cómo levantar la solución

Validar primero la configuración:

```powershell
docker compose --env-file .env config --quiet
```

Construir y levantar todos los servicios con un único comando:

```powershell
docker compose --env-file .env up --build
```

Para ejecutarlos en segundo plano:

```powershell
docker compose --env-file .env up --build --detach
```

Durante el arranque:

1. PostgreSQL y RabbitMQ esperan hasta estar saludables.
2. `migrator` aplica automáticamente las migraciones de Entity Framework y finaliza.
3. API y Worker se inician después de una migración exitosa.
4. La API registra `${NGROK_RESERVED_URL}/webhooks/telegram` como webhook del bot.
5. ngrok publica el proxy de ingreso y la web queda disponible.

Verificar el estado:

```powershell
docker compose ps --all
```

Es normal que `migrator` figure como `Exited (0)`: indica que terminó correctamente. El resto de los servicios debe estar en ejecución; PostgreSQL, RabbitMQ y API deben informar estado saludable.

| Recurso | Dirección predeterminada |
| --- | --- |
| Bandeja web | `http://127.0.0.1:8080` |
| API | `http://127.0.0.1:5080` |
| Liveness | `http://127.0.0.1:5080/health/live` |
| Readiness | `http://127.0.0.1:5080/health/ready` |
| RabbitMQ Management | `http://127.0.0.1:15672` |
| Webhook público | `${NGROK_RESERVED_URL}/webhooks/telegram` |

Si se modifican los puertos en `.env`, deben usarse esos valores en lugar de los predeterminados. RabbitMQ Management se abre con `RABBITMQ_USER` y `RABBITMQ_PASSWORD`.

Comandos de diagnóstico:

```powershell
docker compose logs migrator
docker compose logs --follow api worker webhook-ingress ngrok
```

Para detener la solución sin eliminar los datos persistidos:

```powershell
docker compose down
```

## 4. Cómo probarla de punta a punta

1. Levantar la solución y esperar a que los servicios estén listos.
2. Confirmar que `health/live` y `health/ready` responden HTTP `200`.
3. Abrir `http://127.0.0.1:8080`.
4. Desde una cuenta personal de Telegram, abrir un chat privado con el bot y enviar un mensaje de texto.
5. Verificar que la conversación aparece en la bandeja sin recargar la página, con vista previa y contador no leído.
6. Abrir la conversación y comprobar que carga el historial y que sus mensajes entrantes se marcan como leídos.
7. Escribir una respuesta de hasta `4096` caracteres y enviarla.
8. Comprobar que primero aparece como `Pending`, luego como `Sent`, y que llega al chat de Telegram. `Sent` significa que Telegram aceptó el mensaje; no confirma que el usuario lo haya leído.
9. Recargar la web y verificar que la conversación y el historial continúan disponibles.

Si Telegram rechaza una respuesta o no está disponible, el Worker realiza un intento inicial y dos reintentos con espera breve. Después de un fallo definitivo, el mensaje queda en `Failed` y el evento se conserva en la cola de fallos.

Para revisar el webhook registrado sin copiar el token fuera del contenedor:

```powershell
docker compose exec -T api sh -c 'curl --fail --silent --show-error "https://api.telegram.org/bot${Telegram__BotToken}/getWebhookInfo"'
```

`result.url` debe coincidir con `NGROK_RESERVED_URL` más `/webhooks/telegram`.

## 5. Supuestos

- No se implementan autenticación ni gestión de usuarios.
- Sólo se procesan mensajes de texto enviados desde chats privados de Telegram.
- El operador no puede iniciar conversaciones; sólo puede responder a usuarios que hayan enviado previamente un mensaje al bot.
- El bot no envía una respuesta automática inicial. La conversación comienza con el mensaje del usuario y continúa cuando un operador responde desde la bandeja.
- Telegram no informa al bot si el usuario leyó una respuesta; el estado `Sent` sólo confirma que Telegram aceptó el envío.
- La interfaz está orientada exclusivamente a monitores de escritorio; no se contempla su uso en móviles ni tablets.
- PostgreSQL y RabbitMQ conservan sus datos en volúmenes Docker entre reinicios.


