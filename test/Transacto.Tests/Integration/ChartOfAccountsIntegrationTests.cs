using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using Xunit;

namespace Transacto.Integration {
	public class ChartOfAccountsIntegrationTests : IntegrationTests {
		[Fact]
		public async Task Somewthing() {
			var accounts = GetChartOfAccounts();

			foreach (var (accountNumber, accountName) in accounts.OrderBy(_ => Guid.NewGuid())) {
				await HttpClient.SendCommand("/chart-of-accounts", new DefineAccount {
					AccountName = accountName.ToString(),
					AccountNumber = accountNumber.ToInt32()
				}, TransactoSerializerOptions.BusinessTransactions());
			}

			await Task.Delay(500);

			using var response = await HttpClient.GetAsync("/chart-of-accounts");

			var chartOfAccounts = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

			using var resultEnumerator = chartOfAccounts.RootElement.EnumerateObject();
			using var expectEnumerator = accounts.OrderBy(x => x.Item1.ToInt32()).GetEnumerator();

			while (expectEnumerator.MoveNext() && resultEnumerator.MoveNext()) {
				Assert.Equal(expectEnumerator.Current.Item1.ToString(), resultEnumerator.Current.Name);
				Assert.Equal(expectEnumerator.Current.Item2.ToString(), resultEnumerator.Current.Value.ToString());
			}

			Assert.False(expectEnumerator.MoveNext());
			Assert.False(resultEnumerator.MoveNext());

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		private static IEnumerable<(AccountNumber, AccountName)> GetChartOfAccounts() {
			yield return (new AccountNumber(1000), new AccountName("Bank Checking Account"));
			yield return (new AccountNumber(1050), new AccountName("Bank Savings Account"));
			yield return (new AccountNumber(1200), new AccountName("Accounts Receivable"));
			yield return (new AccountNumber(2000), new AccountName("Accounts Payable"));
			yield return (new AccountNumber(3000), new AccountName("Opening Balance Equity"));
			yield return (new AccountNumber(3900), new AccountName("Retained Earnings"));
			yield return (new AccountNumber(4000), new AccountName("Sales Income"));
			yield return (new AccountNumber(5000), new AccountName("Cost of Goods Sold"));
		}
	}
}
