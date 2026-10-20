# ADR-002: Comunicación en tiempo real con SignalR

**Estado:** Aceptado

## Contexto

El operador debe ver nuevos mensajes y cambios de estado sin recargar manualmente la página. Al mismo tiempo, la aplicación necesita HTTP para cargar la lista de conversaciones, consultar historiales y ejecutar acciones.

## Alternativas consideradas

- **Polling periódico:** es sencillo y funciona con HTTP tradicional. Como desventaja, genera consultas aunque no haya novedades y siempre agrega una demora entre el cambio y su visualización.
- **Server-Sent Events:** permite enviar eventos desde el servidor al navegador con poca complejidad. Como desventaja, la comunicación es principalmente en una sola dirección y requiere resolver por separado parte de la reconexión y coordinación del estado.
- **WebSocket administrado directamente:** ofrece comunicación en ambas direcciones y control completo. Como desventaja, obliga a implementar más lógica de conexión, reconexión, errores y formato de mensajes.
- **SignalR:** se integra de forma natural con ASP.NET Core, administra la conexión y permite reconexión automática. Como desventaja, agrega una dependencia específica y requiere configurar correctamente el proxy del frontend.

## Decisión

Usar SignalR para enviar actualizaciones desde la API hacia Angular.

La conexión se publica mediante `/hubs/inbox` y el evento principal es `conversationUpdated`. La API recibe las actualizaciones desde RabbitMQ y las comunica a los navegadores conectados.

HTTP continúa siendo la fuente para cargar conversaciones e historiales, marcar una conversación como leída y enviar respuestas. SignalR comunica cambios recientes, pero no reemplaza la información persistida ni las consultas HTTP.

## Consecuencias

- Los cambios se muestran con baja demora y sin consultas periódicas.
- SignalR administra la negociación del transporte y la reconexión de la conexión.
- nginx debe reenviar correctamente las solicitudes de `/hubs/`.
- Un navegador puede perder eventos mientras está desconectado, por lo que debe volver a consultar el estado persistido cuando necesite resincronizarse.
- Los contratos de los eventos deben mantenerse coordinados entre backend y frontend.
