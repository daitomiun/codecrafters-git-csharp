using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

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
    String shaParam = args[2];
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
    deflateStream.Flush();
} else if (command == "hash-object")
{
    // Copy file to hash and compress it to a blob of bytes with the name of blob
    String fileToCopy = Directory.GetFiles("./", "*.txt")[0];

    if (File.Exists(fileToCopy))
    {
        Byte[] fileByteContents = File.ReadAllBytes(fileToCopy);
        Byte[] header =Encoding.UTF8.GetBytes($"blob {fileByteContents.Length}\0");

        Byte[] combineBytes = [..header, ..fileByteContents];

        // We hash the file according to git's stantard
        // https://stackoverflow.com/questions/7225313/how-does-git-compute-file-hashes
        String hashFromFile = Convert.ToHexString(SHA1.HashData(combineBytes)).ToLower();
        String hashDir = hashFromFile.Substring(0, 2);
        String hashFile = hashFromFile.Substring(2);
        
        Directory.CreateDirectory($".git/objects/{hashDir}/");
        var compressedFilePath = Path.Combine($".git/objects/{hashDir}/", hashFile);
        var compressedFile = new FileStream(compressedFilePath, FileMode.Create, FileAccess.Write);
        using var zLibStream = new ZLibStream(compressedFile, CompressionMode.Compress);
        zLibStream.Write(combineBytes);
        
        Console.WriteLine(hashFromFile);
    }
    else
    {
        Console.WriteLine("File not found");
    }
} else if (command == "ls-tree")
{
    String gitObjectsPath = ".git/objects/";
    String shaParam = args[2];

    var getDirs = Directory.GetDirectories(gitObjectsPath);

    foreach (var dir in getDirs)
    {
        var searchPath = shaParam.Substring(0, 2);
        var splitDir = dir.Split("/").Last();
        if (searchPath.Equals(splitDir))
        {
            var getFile = Directory.GetFiles($"{dir}/")[0];
            var compressedFile = new FileStream(getFile, FileMode.Open, FileAccess.ReadWrite);
            using var zLibStream = new ZLibStream(compressedFile, CompressionMode.Decompress);
            using var reader = new StreamReader(zLibStream);

            var contents = reader.ReadToEnd();

            var treeObjectLines = contents.Split(" ");
            var listObjectNames = new List<string>();

            for (var i = 2; i < treeObjectLines.Length; i++)
            {
                // Used to split null characters to make a correct division of every object
                var name = treeObjectLines[i].Split("\0")[0];
                listObjectNames.Add(name);
            }

            foreach (var name in listObjectNames.OrderBy(x => x))
            {
                Console.WriteLine(name);
            }
        }
    }

    
} else
{
    throw new ArgumentException($"Unknown command {command}");
}
