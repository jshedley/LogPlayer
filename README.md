# LogPlayer

LogPlayer, effectively stores an audit log of an object, and allows that audit log to be replayed to reconstruct the object.

For Example, take this simple class:
```
 public class SimpleItem
 {
    public int AnIntegerValue { get; set; }
 }
```

If the property *AnIntegerValue* changes from value 1 to 3, the log would have two entries. It would replay the logs in chronological order to reconstruct the object, ending with the last log entry, resulting in *AnIntegerValue* being the value 3.

An audit log entry looks as follows:
```
public class ChangeLog<I> where I:struct
{
    public Guid? ChangeLogId { get; set; }
    public I? ObjectId { get; set; }
    public string FullTypeName { get; set; }
    public string PropertySystemType { get; set; }
    public string Property { get; set; }
    public string Value { get; set; }
    public int? ChangedBy { get; set; }
    public DateTime ChangedUtc { get; set; } = DateTime.UtcNow;
}
```
The above information allows the implementation to know the order logs to be replayed in, and more importantly gives meta data associated with that object, which is used during the implementation for retrival.

## Storage
Any storage of the data can be implemented. The code uploaded currently contains a MicrosoftSQL implementation, which is build from the interface ```IStore```, which is found within the implementation.

The ```IStore``` has a method called ```Provision``` which will create the storage container for holding the logs for the given Object(s). The implementation of the MicrosoftSQL (```MicrosoftSqlStore```) in its constructor allows for the Table and Schema to be specified. The ```Provision``` method will use those variables to create a table capabale of holding the Audit Logs. It is possible to store multiple objects logs in the table, and this is done internally by the implementation, but it effectively seperates them by storing the ```FullTypeName``` of the object being stored in the AuditLog.

### Encryption
The In ```InMemoryStore``` implementation of the IStore, stores its in memeory changes in Memory. The ```ChangeLog<I>.Value``` is the value that is encrypted. Only at the point of returning the values, are they decrypted and used by the consumer. The in memory store is protected using the ```RijndaelManaged``` cryptogrophy libary, and a random IV and Key are generated. The Key is also protected using the ```ProtectedMemory.Protect``` static method provided by *Microsoft* and only is the byte array of the key decrypted when data is requested, and is kept within the private scope of the data request. The ```MemoryProtectionScope``` is set to ```SameProcess```.

## Performance
The performance is subject to the hardware running the tests on, the below stats are basis:
> Processor: Intel64 Family 6 Model 142 Stepping 10 GenuineIntel ~1910 Mhz
> O/S: 10.0.16299 Build 16299
> Memory :  16,309 MB
> HDD: SAMSUNG MZFLW512HMJP-000MV - 512GB

### SQL Store
| Test | Execution Time |
| ------------ | ------------ |
| Inserting 1000 into store | 34ms |
| Inserting a single into store | 4ms |
| Getting all for type | 39ms |
| Getting single item | 2ms |
### InMemoryStore - No Base Store
| Test | Execution Time |
| ------------ | ------------ |
| Inserting 1000 into store | 3ms |
| Inserting a single into store | 0ms |
| Getting all for type | 15ms |
| Getting single item | 1ms |
### InMemoryStore - With SQL Base Store
| Test | Execution Time |
| ------------ | ------------ |
| Inserting 1000 into store | 40ms |
| Inserting a single into store | 5ms |
| Getting all for type | 68ms |
| Getting single item | 5ms |