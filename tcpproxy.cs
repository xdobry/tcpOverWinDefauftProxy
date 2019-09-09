/*
    This program tcpproxy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

    Written by xdobry 2019
    see: https://github.com/xdobry/tcpOverWinDefauftProxy
 */
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Threading;  

public class StateObject {  
    public Socket workSocket = null;  
    public Socket proxySocket = null;
    public const int BufferSize = 1024;  
    public byte[] buffer = new byte[BufferSize];  
    public byte[] outbuffer = new byte[BufferSize];
    public byte[] proxyBuffer = new byte[BufferSize];  
    public byte[] proxyOutbuffer = new byte[BufferSize];
} 

class Programm {
    static string targetHost;
    static string targetPort;
    static bool isDebug = false;

    public static ManualResetEvent allDone = new ManualResetEvent(false);  

    static void Main(string[] args) {
        Console.WriteLine("tcpproxy is GPL Software written by xdobry 2019 (see https://github.com/xdobry/tcpOverWinDefauftProxy)");
        if  (args.Length!=3) {
            Console.WriteLine("Arguments Count Error Exiting! Expect arguments targetHostname targetPort listenPort");    
            Environment.Exit(1);
        }
        targetHost = args[0];
        targetPort = args[1];
        string listenPort = args[2];

        IPAddress ipAddress = (Dns.Resolve(IPAddress.Any.ToString())).AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Int32.Parse(listenPort));  
  
        // Create a TCP/IP socket.  
        Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp );  
        byte[] bytes = new Byte[1024];  

        try {  
            listener.Bind(localEndPoint);  
            listener.Listen(10);  

            // Start listening for connections.  
            Console.WriteLine("Waiting for a connection on: "+listenPort+ " adress: "+ipAddress);  
            while (true) {  
                allDone.Reset();  
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);  
                // Wait until a connection is made before continuing.  
                allDone.WaitOne(); 
            }  
  
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        }  
  
    }

    public static void AcceptCallback(IAsyncResult ar) {  
        // Signal the main thread to continue.  
        allDone.Set(); 
        Console.WriteLine("client connected");
  
        // Get the socket that handles the client request.  
        Socket listener = (Socket) ar.AsyncState;  
        Socket handler = listener.EndAccept(ar);  
  
        // Create the state object.  
        StateObject state = new StateObject();  
        state.workSocket = handler; 
        state.proxySocket = createSocket(targetHost, targetPort);
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        state.proxySocket.BeginReceive(state.proxyBuffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallbackProxy), state);
    }

    public static void ReadCallback(IAsyncResult ar) {  
        StateObject state = (StateObject) ar.AsyncState;  
        Socket handler = state.workSocket;  
  
        // Read data from the client socket.
        int bytesRead = 0;
        try {
            bytesRead = handler.EndReceive(ar);  
        } catch (ObjectDisposedException) {
            Console.WriteLine("proxy connection already closed");
            return;
        }
  
        if (bytesRead > 0) {  
            if (isDebug) {
                Console.WriteLine("Got {0} bytes from client.", bytesRead);
            }
            // Console.WriteLine(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
            Buffer.BlockCopy(state.buffer, 0, state.outbuffer, 0, bytesRead);
            state.proxySocket.BeginSend(state.outbuffer, 0 , bytesRead, 0, new AsyncCallback(SendCallback), state.proxySocket);
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  new AsyncCallback(ReadCallback), state);  
        }  else {
            Console.WriteLine("connection closed by client");
            if (handler.Connected) {
                handler.Shutdown(SocketShutdown.Both);  
                handler.Close();  
            }
            if (state.proxySocket.Connected) {
                state.proxySocket.Shutdown(SocketShutdown.Both);
                state.proxySocket.Close();
            }
        }
    } 

    public static void ReadCallbackProxy(IAsyncResult ar) {  
        StateObject state = (StateObject) ar.AsyncState;  
        Socket handler = state.proxySocket;
        int bytesRead = 0;
        try {
            bytesRead = handler.EndReceive(ar);  
        } catch (ObjectDisposedException) {
            Console.WriteLine("proxy connection already closed");
            return;
        }
  
        if (bytesRead > 0) {  
            if (isDebug) {
                Console.WriteLine("Got {0} bytes from proxy.", bytesRead);
            }
            // Console.WriteLine(Encoding.ASCII.GetString(state.proxyBuffer, 0, bytesRead));
            Buffer.BlockCopy(state.proxyBuffer, 0, state.proxyOutbuffer, 0, bytesRead);
            state.workSocket.BeginSend(state.proxyOutbuffer, 0 , bytesRead, 0, new AsyncCallback(SendCallbackProxy), state.workSocket);
            handler.BeginReceive(state.proxyBuffer, 0, StateObject.BufferSize, 0,  new AsyncCallback(ReadCallbackProxy), state);  
        }  else {
            Console.WriteLine("connection closed by proxy");
            if (handler.Connected) {
                handler.Shutdown(SocketShutdown.Both);  
                handler.Close();  
            }
            if (state.workSocket.Connected) {
                state.workSocket.Shutdown(SocketShutdown.Both);
                state.workSocket.Close();
            }
        }
    } 


    private static void SendCallback(IAsyncResult ar) {  
        try {  
            Socket handler = (Socket) ar.AsyncState;  
            int bytesSent = handler.EndSend(ar);  
            if (isDebug) {
                Console.WriteLine("Sent {0} bytes to proxy.", bytesSent);  
            }
        } catch (ObjectDisposedException) {
            Console.WriteLine("client connection already closed");
            return;
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        }  
    }   

    private static void SendCallbackProxy(IAsyncResult ar) {  
        try {  
            Socket handler = (Socket) ar.AsyncState;  
            int bytesSent = handler.EndSend(ar);  
            if (isDebug) {
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);  
            }
        } catch (ObjectDisposedException) {
            Console.WriteLine("proxy connection already closed");
            return;
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        }  
    }



    public static Socket createSocket(String targetHost, String targetPort) {
        WebProxy myProxy= (WebProxy) WebProxy.GetDefaultProxy();
        Uri proxyUri = myProxy.GetProxy(new Uri("https://en.wikipedia.org/wiki/Main_Page"));
        Console.WriteLine("connecting by default proxy {0}",proxyUri);
        myProxy.Credentials = CredentialCache.DefaultCredentials;

        var request = WebRequest.Create("http://" + targetHost + ":" + targetPort);
        request.Proxy = myProxy;
        request.Method = "CONNECT";

        var response = request.GetResponse();

        var responseStream = response.GetResponseStream();
        Debug.Assert(responseStream != null);

        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        var rsType = responseStream.GetType();
        var connectionProperty = rsType.GetProperty("Connection", Flags);

        var connection = connectionProperty.GetValue(responseStream, null);
        var connectionType = connection.GetType();
        var networkStreamProperty = connectionType.GetProperty("NetworkStream", Flags);

        var networkStream = networkStreamProperty.GetValue(connection, null);
        var nsType = networkStream.GetType();
        var socketProperty = nsType.GetProperty("Socket", Flags);
        Socket socket = (Socket)socketProperty.GetValue(networkStream, null);
        Console.WriteLine("proxy tunnel established");
        return socket;
    }



}