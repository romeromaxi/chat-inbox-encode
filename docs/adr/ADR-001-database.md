# ADR-001: Persistencia relacional con PostgreSQL

**Estado:** Aceptado

## Contexto

La bandeja necesita guardar conversaciones y mensajes. También debe mantener estados de entrega, contadores de mensajes no leídos, orden temporal e identificadores únicos de Telegram.

Estos datos están relacionados y varios cambios deben realizarse juntos. Por ejemplo, al recibir un mensaje se debe guardar el mensaje, actualizar la conversación y aumentar el contador de no leídos de manera consistente.

## Alternativas consideradas

- **Base de datos SQL:** ofrece relaciones claras, validaciones, transacciones y consultas adecuadas para listados e historiales. Como desventaja, los cambios de estructura requieren migraciones y una administración más planificada.
- **Base de datos NoSQL:** permite cambiar la forma de los documentos con mayor libertad y podría guardar una conversación completa en un solo documento. Como desventaja, puede duplicar información y vuelve más difíciles la consistencia, los contadores, el orden y las consultas entre contactos, conversaciones y mensajes.

## Decisión

Usar PostgreSQL como base de datos SQL, con Entity Framework Core y Npgsql para acceder a los datos.

Los contactos, conversaciones y mensajes se guardan en tablas relacionadas. Los identificadores expuestos por la API son GUID, mientras que las claves internas pueden ser numéricas. Las restricciones e índices de la base garantizan que los identificadores públicos y los identificadores de Telegram no se repitan.

Los cambios de estructura se aplican mediante migraciones ejecutadas por `ChatInbox.Migrator` antes de iniciar la API y el Worker.

## Consecuencias

- Se obtiene consistencia para operaciones que modifican varios datos relacionados.
- Los listados, historiales, filtros y órdenes se pueden consultar de forma directa.
- Cada cambio de estructura necesita una migración versionada.
- PostgreSQL requiere un volumen persistente, mantenimiento y una política de backup.
- Se elige consistencia y facilidad de consulta por encima de la flexibilidad de esquema de una alternativa NoSQL.
