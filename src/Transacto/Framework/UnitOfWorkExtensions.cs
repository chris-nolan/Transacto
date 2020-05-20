using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework {
	public interface ICommandContext {
		Optional<Position> Position { get; }
		void SetPosition(Position position);
	}

	public static class UnitOfWorkExtensions {
		public static ICommandHandlerBuilder<(UnitOfWork, TCommand)> UnitOfWork<TCommand>(
			this ICommandHandlerBuilder<TCommand> builder, EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, JsonSerializerOptions eventSerializerOptions,
			ICommandContext? commandContext = null)
			where TCommand : class =>
			builder.Transform<(UnitOfWork, TCommand)>(next => async (message, ct) => {
				var unitOfWork = new UnitOfWork();

				await next((unitOfWork, message), ct);

				if (!unitOfWork.HasChanges) {
					return;
				}

				var (streamName, aggregateRoot, expectedVersion) = unitOfWork.GetChanges().Single();

				var eventData = aggregateRoot.GetChanges().Select(e => new EventData(Uuid.NewUuid(),
					messageTypeMapper.Map(e.GetType()) ?? throw new InvalidOperationException(),
					JsonSerializer.SerializeToUtf8Bytes(e, eventSerializerOptions)));

				var result = await Append();
				commandContext?.SetPosition(result.LogPosition);

				aggregateRoot.MarkChangesAsCommitted();

				Task<IWriteResult> Append() => expectedVersion.HasValue
					? eventStore.AppendToStreamAsync(streamName,
						new StreamRevision(Convert.ToUInt64(expectedVersion.Value)),
						eventData,
						cancellationToken: ct)
					: eventStore.AppendToStreamAsync(streamName,
						StreamState.NoStream,
						eventData,
						cancellationToken: ct);
			});
	}
}
