# Tcp Socket Over Windinws Defauft HTTP Proxy

small Windows .net program that allow to tunel tcp connection (port forwading) over windows default http proxy. 
It supports windows build in proxy authentification as kerberos or NTLMv2.

# Usage

tcpproxy targetHost tagetPort localPort

# Example

If you want to connect the ssh on target host myhost.com on port 22 but your network does not allow this connection and the putty does not support the proxy authentification setting and you always get 407 proxy error.
Check if your windows browser is working. It means your default system proxy is up and running.

run

tclproxy myhost.com 22 2222

This will open local port on 2222 and forward all incomming connections to myhost.com:22 over the http proxy. It uses the proxy Socket forwaring by using CONNECT request.

ssh -p 2222 127.0.0.1

# Internals

The programm is written in .NET it uses the possiblity that windows pass the proxy setting from standard browser (IE) to all .NET programs. It also handles the transparently the proxy authentification methods (as NTLM or Kerberos) that sometime is not supported by unix derived programs or older one (like ssh, putty, git, ....)