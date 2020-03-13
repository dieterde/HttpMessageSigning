using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dalion.HttpMessageSigning.Verification {
    /// <summary>
    /// Represents an in-memory store that the server can query to obtain client-specific settings for request signature verification.
    /// </summary>
    public class InMemoryClientStore : IClientStore {
        private readonly IList<Client> _entries;

        /// <summary>
        /// Create a new empty instance of the <see cref="InMemoryClientStore"/> class.
        /// </summary>
        public InMemoryClientStore() {
            _entries = new List<Client>();
        }
        
        /// <summary>
        /// Create a new instance of the <see cref="InMemoryClientStore"/> class, containing the specified <see cref="Client"/> instances.
        /// </summary>
        /// <param name="clients">The <see cref="Client"/> instances to register.</param>
        public InMemoryClientStore(params Client[] clients) {
            if (clients == null) clients = Array.Empty<Client>();
            _entries = new List<Client>(clients);
        }
        
        /// <summary>
        /// Registers a client, and its settings to perform signature verification.
        /// </summary>
        /// <param name="client">The entry that represents a known client.</param>
        public Task Register(Client client) {
            if (client == null) throw new ArgumentNullException(nameof(client));
            
            if (_entries.Contains(client)) throw new InvalidOperationException($"A {nameof(Client)} with id '{client.Id}' is already registered in the server store.");
            
            _entries.Add(client);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the registered client that corresponds to the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the registered client to get.</param>
        /// <returns>The registered client that corresponds to the specified identifier.</returns>
        public Task<Client> Get(KeyId id) {
            if (id == KeyId.Empty) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
            
            var match = _entries.FirstOrDefault(_ => _.Id == id);

            if (match == null) throw new SignatureVerificationException($"No {nameof(Client)}s with id '{id}' are registered in the server store.");

            return Task.FromResult(match);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            foreach (var entry in _entries) {
                entry?.Dispose();
            }
            _entries.Clear();
        }
    }
}