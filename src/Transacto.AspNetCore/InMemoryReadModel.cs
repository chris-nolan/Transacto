using System;
using System.Collections;
using System.Collections.Concurrent;

namespace Transacto {
	public class InMemoryReadModel {
		private readonly ConcurrentDictionary<string, object?> _readModels;

		public InMemoryReadModel() {
			_readModels = new ConcurrentDictionary<string, object?>();
		}

		public void AddOrUpdate<T>(string key, Action<T> action, Func<T> factory) where T : class {
			var maybeTarget = _readModels.GetOrAdd(key, _ => factory());

			if (!(maybeTarget is T target)) {
				return;
			}

			lock (target) {
				action(target);
			}
		}

		public bool TryGet<T>(string key, out T? value) where T : class {
			value = null;
			if (!_readModels.TryGetValue(key, out var maybeValue)) {
				return false;
			}

			if (!(maybeValue is T v)) {
				return false;
			}

			value = v;
			return true;
		}

		public bool TryGet<T>(string key, Func<T, T> clone, out T target) => TryGet<T, T>(key, clone, out target);

		public bool TryGet<T, TTransformed>(string key, Func<T, TTransformed> transform, out TTransformed target) {
			if (!_readModels.TryGetValue(key, out var maybeTarget) || !(maybeTarget is T value)) {
				target = default!;
				return false;
			}

			lock (value) {
				target = transform(value);
			}

			return true;
		}
	}
}
