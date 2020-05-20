using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto;

namespace SomeCompany.Inventory {
	public class Inventory : IPlugin {
		public string Name { get; } = nameof(Inventory);

		public void Configure(IEndpointRouteBuilder builder)
			=> builder.UseInventory();

		public void ConfigureServices(IServiceCollection services)
			=> services.AddNpgSqlProjection<InventoryLedger>();

		public IEnumerable<Type> MessageTypes { get { yield return typeof(InventoryItemDefined); } }
	}
}
