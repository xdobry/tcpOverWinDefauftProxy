REM SET YOUR PATH TO YOUR .NET Framework
REM FOR EXAMPLE. CHECK YOUR SYSTEM FOR YOUR .NET VERSION
set PATH=%PATH%;%windir%\Microsoft.NET\Framework\v4.0.30319\
csc -reference:System.Net.Http.dll -reference:netstandard.dll tcpproxy.cs