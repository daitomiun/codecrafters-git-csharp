using System;
using System.IO;
using System.IO.Compression;

if (args.Length < 1)
{
    Console.WriteLine("Please provide a command.");
    return;
}

string command = args[0];

if (command == "init")
{
    Directory.CreateDirectory(".git");
    Directory.CreateDirectory(".git/objects");
    Directory.CreateDirectory(".git/refs");
    File.WriteAllText(".git/HEAD", "ref: refs/heads/main\n");
    Console.WriteLine("Initialized git directory");
} else if (command == "cat-file")
{
    // Assign the params and directories
    string shaParam = args[2];
    String hashedDir = shaParam[..2];
    String file = shaParam[2..];
    
    String path = $".git/objects/{hashedDir}/{file}";
    var blob = File.ReadAllBytes(path);
    
    // Example of stream: Stack overflow --> https://stackoverflow.com/questions/507747/can-you-explain-the-concept-of-streams
    // 1. create a memoryStream to get the source of the blob
    //  We create a Memory stream to saving the blob and stores it in memory, also it cannot be writable
    // https://learn.microsoft.com/en-us/dotnet/api/System.IO.MemoryStream?view=net-9.0
    using var memoryStream = new MemoryStream(blob, false);
    
    // 2. "Seek" or  get the bytes with an offset of 2 as the header starts from 0 to 1
    memoryStream.Seek(2, SeekOrigin.Begin);
            
    // 3. It deflates the stream to decompress it
    // When it deplates the streams it has modes to decompress or compress the data from the memory stream assigned
    // info here: https://learn.microsoft.com/en-us/dotnet/api/System.IO.Compression.DeflateStream?view=net-9.00
    using var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
    using var reader = new StreamReader(deflateStream);
            
    // After this it reads until the end to then write it without a line jump
    var content = reader.ReadToEnd();
    Console.Write(content.Split('\x00')[1]);
}
else
{
    throw new ArgumentException($"Unknown command {command}");
}
