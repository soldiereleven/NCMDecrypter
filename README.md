# Netease Copyright Music Decrypter.
This is a package taht support convert Netease Copyright Music format to the flac or mp3 format which is friendly to your media player. It also read the metadata from the source file and fix it, using taglib.
## todos
- Lyrics fix
- Completely async
- more options
## Usage
First, you should create a Decrypter using a filae path or `IEnumerable<byte>` or a `MemoryStream`. 
```csharp
public Decrypter(string filePath)
public Decrypter(IEnumerable<byte> data)
public Decrypter(MemoryStream ms)
```
So you can create like followings
```csharp
using Network11.NCMDecrypter;
var decrypter = new Decrypter("PATH/TO/YOUR/NCM/SOURCE/FILE");
```
then, create a option class
```csharp
DecryptParam param = new DecryptParam() { OutputName=Path.GetFileNameWithoutExtension(path),OutputPath=Path.GetDirectoryName(path) };
```
OptputName can be a file name with extension or just a name without extension. Anyway, we will delete the extensions and add the proper one. The OutputPath must be a valid directory.


Finally, just call the execute function
```csharp
await decrypter.Execute(param);
```
Of course you can use Task to run more than one process at one time. It will be helpful when you are dealing with many files but can also be challenging to your RAM :)
