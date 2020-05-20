using EventStore.Client;
using Microsoft.AspNetCore.Http;
using Transacto.Framework;

namespace Transacto {
	public class HttpContextCommandContext : ICommandContext {
		private readonly IHttpContextAccessor _accessor;
		private static readonly string Key = nameof(Transacto) + nameof(Position);

		public HttpContextCommandContext(IHttpContextAccessor accessor) {
			_accessor = accessor;
		}

		public Optional<Position> Position =>
			_accessor.HttpContext.Items.TryGetValue(Key, out var position)
				? (Position)position
				: Optional<Position>.Empty;

		public void SetPosition(Position position) => _accessor.HttpContext.Items[Key] = position;
	}
}
