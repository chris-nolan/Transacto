using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace Transacto.Modules {
	public class GeneralLedgerEntryModule : CommandHandlerModule {
		public GeneralLedgerEntryModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			JsonSerializerOptions eventSerializerOptions, ICommandContext commandContext) {
			Build<PostGeneralLedgerEntry>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, eventSerializerOptions, commandContext)
				.Handle((_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new GeneralLedgerEntryHandlers(
						new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
						new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					return handlers.Handle(command, ct);
				});
		}
	}
}
