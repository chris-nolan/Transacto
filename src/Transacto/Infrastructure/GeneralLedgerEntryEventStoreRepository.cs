using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure {
	public class GeneralLedgerEntryEventStoreRepository : IGeneralLedgerEntryRepository {
		private readonly EventStoreRepository<GeneralLedgerEntry> _inner;

		public GeneralLedgerEntryEventStoreRepository(EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, UnitOfWork unitOfWork) {
			_inner = new EventStoreRepository<GeneralLedgerEntry>(eventStore, unitOfWork,
				GeneralLedgerEntry.Factory, messageTypeMapper);
		}

		public async ValueTask<GeneralLedgerEntry> Get(GeneralLedgerEntryIdentifier identifier,
			CancellationToken cancellationToken = default) {
			var optionalGeneralLedgerEntry = await _inner.GetById(identifier.ToString(), cancellationToken);
			if (!optionalGeneralLedgerEntry.HasValue) {
				throw new InvalidOperationException();
			}

			return optionalGeneralLedgerEntry.Value;
		}

		public void Add(GeneralLedgerEntry generalLedgerEntry) => _inner.Add(generalLedgerEntry);
	}
}
