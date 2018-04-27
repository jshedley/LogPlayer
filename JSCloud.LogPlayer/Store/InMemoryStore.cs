using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JSCloud.LogPlayer.Types;

namespace JSCloud.LogPlayer.Store
{
    public class InMemoryStore<I> : IStore<I>
        where I : struct, IComparable<I>
    {
        private byte[] encryptionKey;
        private byte[] iv;
        private readonly IStore<I> _baseStore;
        private readonly ConcurrentDictionary<string, ConcurrentBag<ChangeLog<I>>> _items;

        public InMemoryStore(IStore<I> BaseStore)
        {
            _baseStore = BaseStore;
            _items = new ConcurrentDictionary<string, ConcurrentBag<ChangeLog<I>>>();
            generateEncryptionKey();
        }

        private void generateEncryptionKey()
        {
            using (var rngCsp = new RijndaelManaged())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GenerateKey();
                encryptionKey = rngCsp.Key;

                rngCsp.GenerateIV();
                iv = rngCsp.IV;
            }
            ProtectedMemory.Protect(encryptionKey, MemoryProtectionScope.SameProcess);
        }

        public async Task<ICollection<ChangeLog<I>>> GetChangesAsync(I? objectId, string fullTypeName)
        {
            if (_items.ContainsKey(fullTypeName))
            {
                var items = _items[fullTypeName];
                return await Task.Run(() =>
                {
                    var foundItems = items.Where(x => (x.ObjectId.Equals(objectId) || !objectId.HasValue)
                                  && x.FullTypeName == fullTypeName).Select(x => new ChangeLog<I>(x)).ToList();

                    var key = getEncryptionKey();
                    foundItems.ForEach(x =>
                        {
                            x.Value = decryptValue(x.Value, key);
                        });
                    key = null;

                    return foundItems;
                });
            }
            return new LinkedList<ChangeLog<I>>();
        }

        private string encryptValue(string value, byte[] key)
        {
            if(string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(value);

            byte[] cipherTextBytes;
            var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
            var encryptor = symmetricKey.CreateEncryptor(key, iv);

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    cipherTextBytes = memoryStream.ToArray();
                    cryptoStream.Close();
                }
                memoryStream.Close();
            }
            return Convert.ToBase64String(cipherTextBytes);
        }

        private string decryptValue(string value, byte[] key)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            byte[] cipherTextBytes = Convert.FromBase64String(value.ToString());
            var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.None };

            var decryptor = symmetricKey.CreateDecryptor(key, iv);
            using (var memoryStream = new MemoryStream(cipherTextBytes))
            {
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    byte[] plainTextBytes = new byte[cipherTextBytes.Length];

                    int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                    memoryStream.Close();
                    cryptoStream.Close();
                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
                }
            }
        }

        private byte[] getEncryptionKey()
        {
            byte[] key = new byte[encryptionKey.Length];
            Array.Copy(encryptionKey, key, encryptionKey.Length);
            ProtectedMemory.Unprotect(key, MemoryProtectionScope.SameProcess);
            return key;
        }

        public async Task Provision()
        {
            if (_baseStore != null)
            {
                await _baseStore.Provision();
                var allItems = await _baseStore.GetChangesAsync(null, null);
                var key = getEncryptionKey();
                allItems.AsParallel().ForAll(x =>
                {
                    if (!_items.ContainsKey(x.FullTypeName))
                    {
                        _items.TryAdd(x.FullTypeName, new ConcurrentBag<ChangeLog<I>>());
                    }
                    x.Value = encryptValue(x.Value, key);
                    _items[x.FullTypeName].Add(x);
                });
                key = null;
            }
        }

        public async Task<ChangeLog<I>> StoreAsync(ChangeLog<I> changeLog)
        {
            var key = getEncryptionKey();
            if (_baseStore != null)
            {
                changeLog = await _baseStore.StoreAsync(changeLog);
            }
            else
            {
                changeLog.ChangeLogId = Guid.NewGuid();
            }
            if (!_items.ContainsKey(changeLog.FullTypeName))
            {
                _items.TryAdd(changeLog.FullTypeName, new ConcurrentBag<ChangeLog<I>>());
            }

            var returnChangeLog = new ChangeLog<I>(changeLog);

            returnChangeLog.Value = encryptValue(changeLog.Value, key);
            key = null;
            _items[changeLog.FullTypeName].Add(returnChangeLog);
            return changeLog;
        }

        public async Task<ICollection<ChangeLog<I>>> StoreAsync(ICollection<ChangeLog<I>> changeLogs)
        {
            var key = getEncryptionKey();

            if (changeLogs.Count == 0)
            {
                return changeLogs;
            }

            if (_baseStore != null)
            {
                changeLogs = await _baseStore.StoreAsync(changeLogs);
            }
            else
            {
                for (int i = 0; i < changeLogs.Count; i++)
                {
                    changeLogs.ElementAt(i).ChangeLogId = Guid.NewGuid();
                }
            }
            if (!_items.ContainsKey(changeLogs.ElementAt(0).FullTypeName))
            {
                _items.TryAdd(changeLogs.ElementAt(0).FullTypeName, new ConcurrentBag<ChangeLog<I>>());
            }

            var returnChangeLogs = changeLogs.Select(x => new ChangeLog<I>(x)).ToList();

            returnChangeLogs.AsParallel().ForAll(x =>
            {
                x.Value = encryptValue(x.Value, key);
                _items[x.FullTypeName].Add(x);
            });
            return changeLogs;
        }
    }
}
