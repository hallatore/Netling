Netling is a load tester client for easy web testing. It is extremely fast while using little CPU or memory.

## Requirements
.NET 4.7.1
.NET Core 2.1

## Usage

The base source is meant to support most scenarios. You can use the wpf client, console client or integrate netling.core into your custom solution.

Need custom headers, data, etc? Fork and tweak it to your needs! :)

### SocketWorker
This is the default worker. It uses raw sockets and is very fast.

PS: SocketWorker requires keep-alive. Connection: Close will result in errors.

### HttpClientWorker
This worker uses HttpClient and is easier to tweak.

## Screenshots

![Client](http://i.imgur.com/uNwaVTu.png)

![Result window](http://i.imgur.com/hpTbHsq.png)

![Console application](http://i.imgur.com/8gbPkxK.png)

## License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
