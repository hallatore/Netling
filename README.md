Netling is a load tester client for easy web testing. It is extremely fast while using little CPU or memory.

## Requirements
.NET 5

## Usage

The base source is meant to support most scenarios. You can use the WPF client, console client or integrate Netling.Core into your custom solution.

Need custom headers, data, etc? Fork and tweak it to your needs! :)

### SocketWorker
This is the default worker. It uses raw sockets and is very fast.

PS: SocketWorker requires keep-alive. Connection: Close will result in errors.

### HttpClientWorker
This worker uses HttpClient and is easier to tweak.

## Screenshots

![Client](https://i.imgur.com/m8GQn94.png)

![Result window](https://i.imgur.com/xpxz22y.png)

![Console application](https://i.imgur.com/Quh4EWM.png)
