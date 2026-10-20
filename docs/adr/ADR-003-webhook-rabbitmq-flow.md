# ADR-003: Recepción del webhook y mensajería con RabbitMQ

**Estado:** Aceptado

## Contexto

Telegram necesita un endpoint HTTPS público para entregar mensajes. La API debe responder rápidamente y no conviene mantener la solicitud abierta mientras se guarda el mensaje, se actualiza la conversación o se envía una respuesta.

También es necesario conservar temporalmente el trabajo cuando alguno de los procesos está ocupado o se reinicia.

## Alternativas consideradas

- **Usar RabbitMQ sólo para mensajes entrantes:** desacopla la recepción del webhook, pero deja los mensajes salientes y las actualizaciones en tiempo real con mecanismos diferentes.
- **Usar RabbitMQ para entrada, salida y actualizaciones:** ofrece un flujo uniforme y permite separar la API del Worker. Como desventaja, RabbitMQ se vuelve una dependencia operativa importante y la información se actualiza de forma asíncrona.

## Decisión

Exponer públicamente sólo `POST /webhooks/telegram` mediante ngrok y `webhook-ingress`. La API valida el header `X-Telegram-Bot-Api-Secret-Token` y acepta únicamente mensajes de texto enviados desde chats privados.

La API registra automáticamente el webhook al iniciar, usando `secret_token` y solicitando sólo updates de tipo `message`.

Usar el exchange durable de tipo `topic` llamado `chat-inbox` y las siguientes colas, también durables:

- `chat-inbox.telegram.incoming-text`, asociada a `telegram.message.received`.
- `chat-inbox.telegram.outgoing-text`, asociada a `telegram.message.send`.
- `chat-inbox.telegram.outgoing-text.failed`, asociada a `telegram.message.failed`.
- `chat-inbox.api.conversation-updates`, asociada a `conversation.updated`.

La API publica mensajes entrantes, mensajes salientes y la actualización inicial del mensaje `Pending`. El Worker procesa los trabajos y publica las actualizaciones posteriores o los fallos. La API consume las actualizaciones y las comunica al frontend mediante SignalR.

Los mensajes de RabbitMQ son persistentes, usan `MessageId` y se publican con `mandatory: true`. El publicador espera la confirmación del broker. Los consumidores procesan un mensaje por vez y lo confirman manualmente después de completarlo.

## Consecuencias

- El webhook puede responder después de que RabbitMQ acepta el mensaje, sin esperar todo el procesamiento.
- API y Worker quedan separados y pueden recuperarse de forma independiente.
- RabbitMQ pasa a formar parte de los controles de disponibilidad de la solución.
- Los cambios son asíncronos y pueden tardar unos instantes en reflejarse.
- Los mensajes entrantes repetidos se detectan mediante el identificador único del update de Telegram.
- La confirmación de RabbitMQ no garantiza que una escritura en PostgreSQL y una publicación ocurran como una única operación. Actualmente no existe un patrón outbox para cubrir ese riesgo.
- La cola de fallos conserva envíos definitivos fallidos, pero necesita supervisión y un procedimiento de recuperación.
