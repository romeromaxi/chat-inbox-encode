# ADR-005: Entrega saliente, reintentos y orden temporal

**Estado:** Aceptado

## Contexto

Cuando el operador responde, la interfaz necesita mostrar inmediatamente que la API aceptó y encoló el mensaje, aunque Telegram todavía no haya confirmado el envío.

También se necesita una política limitada de reintentos para evitar tanto la pérdida inmediata como los intentos infinitos ante un fallo permanente.

La bandeja también necesita un criterio estable para ordenar mensajes y conversaciones, incluso cuando una confirmación o un fallo llega más tarde.

## Alternativas consideradas

- **Enviar directamente desde la API:** es sencillo y permite devolver el resultado de Telegram en la misma solicitud. Como desventaja, el operador debe esperar y una demora de Telegram mantiene ocupada la API.
- **Publicar el mensaje sin guardarlo ni mostrar estado:** responde rápido, pero el operador no sabe si el mensaje sigue pendiente o falló y existe mayor riesgo de perderlo.
- **Guardar el mensaje y enviarlo de forma asíncrona:** permite mostrar el progreso y reintentar sin bloquear la API. Como desventaja, agrega estados intermedios y consistencia eventual.

## Decisión

Guardar cada respuesta primero con estado `Pending` y publicarla en RabbitMQ. El Worker consume el trabajo y envía el texto mediante Telegram Bot API.

Si Telegram acepta el mensaje, el estado cambia a `Sent`. `Sent` significa que Telegram aceptó el envío, no que el usuario lo leyó.

Ante un fallo se realizan dos reintentos, aproximadamente después de `1` y `3` segundos. Las demoras tienen una variación aleatoria de `±20%` para evitar que varios mensajes se reintenten exactamente al mismo tiempo.

Si todos los intentos fallan, el mensaje cambia a `Failed` y el evento se conserva en la cola de fallos. La creación del mensaje `Pending` actualiza `LastMessageAt`; los cambios posteriores a `Sent` o `Failed` no vuelven a ordenar la conversación.

Todos los timestamps se guardan en UTC. Para los mensajes entrantes se conservan tanto el momento informado por Telegram como el momento de recepción del backend. El timestamp de Telegram define su orden visible.

Las conversaciones se ordenan por `LastMessageAt` de forma descendente. Si dos conversaciones tienen el mismo valor, se desempatan por su GUID público de forma ascendente.

## Consecuencias

- El operador obtiene una respuesta inmediata de la API y puede ver `Pending`, `Sent` o `Failed`.
- Los fallos breves de red pueden recuperarse automáticamente.
- Los reintentos están limitados y no bloquean la cola de manera indefinida.
- Las confirmaciones tardías no cambian inesperadamente la posición de una conversación.
- La interfaz y el backend deben manejar estados que cambian después de la respuesta HTTP inicial.
- Telegram no informa si el destinatario leyó un mensaje, por lo que no se puede mostrar un estado de lectura real.
- Si Telegram acepta un mensaje pero su respuesta se pierde, existe el riesgo de repetir el envío durante un reintento.
