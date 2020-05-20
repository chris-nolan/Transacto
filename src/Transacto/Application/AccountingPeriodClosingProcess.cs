using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class AccountingPeriodClosingProcess {
		private readonly IGeneralLedgerRepository _generalLedger;
		private readonly IGeneralLedgerEntryRepository _generalLedgerEntries;
		private readonly IChartOfAccountsRepository _chartOfAccounts;

		public AccountingPeriodClosingProcess(IGeneralLedgerRepository generalLedger,
			IGeneralLedgerEntryRepository generalLedgerEntries,
			IChartOfAccountsRepository chartOfAccounts) {
			_generalLedger = generalLedger;
			_generalLedgerEntries = generalLedgerEntries;
			_chartOfAccounts = chartOfAccounts;
		}

	}
}
