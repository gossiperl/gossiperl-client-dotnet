# .NET Gossiperl client

.NET [gossiperl](http://gossiperl.com) client library.

## Installation

Download the sources and build using your tooling. This project has been tested using Mono 2.0.1.

    apt-get install -y install mono-complete nunit
    mozroots --import --sync
    
    git clone https://github.com/gossiperl/gossiperl-client-dotnet.git
    cd gossiperl-client-dotnet/
    git tags -l
    git checkout v0.1
    
    mkdir -p .nuget && cd .nuget/
    wget https://nuget.org/nuget.exe
    # is there a better way of doing this?
    wget http://headsigned.com/download/running-nuget-command-line-on-linux/Microsoft.Build.zip
    unzip Microsoft.Build.zip
    
    mkdir -p ../packages
    
    mono --runtime=v4.0 NuGet.exe install ../exec/packages.config -OutputDirectory ../packages
    mono --runtime=v4.0 NuGet.exe install ../lib/packages.config -OutputDirectory ../packages
    mono --runtime=v4.0 NuGet.exe install ../tests/packages.config -OutputDirectory ../packages
    
    cd ..
    # Build the solution:
    xbuild gossiperl-client-dotnet.sln

## Running

Add reference to the `gossiperl-client-lib.dll`, afterwards:

    using Gossiperl.Client;
    Supervisor supervisor = new Supervisor();

## Connecting to an overlay

    OverlayConfiguration config = new OverlayConfiguration () {
      ClientName = "test-dotnet-client",
      ClientPort = 54321,
      ClientSecret = "dotnet-client-secret",
      OverlayName = "gossiperl_overlay_remote",
      OverlayPort = 6666,
      SymmetricKey = "v3JElaRswYgxOt4b"
    };
    Supervisor supervisor = new Supervisor ();
    supervisor.Connect (config);

The instance of supervisor emits events, to subscribe to those:

    supervisor.Connected += new Supervisor.ConnectHandler(OverlayWorker worker);

Available events:


- `Connected(OverlayWorker worker);`
- `Disconnected(OverlayWorker worker);`
- `Event(OverlayWorker worker, string eventType, object member, long heartbeat);`
- `SubscribeAck(OverlayWorker worker, List<string> events);`
- `UnsubscribeAck(OverlayWorker worker, List<string> events);`
- `ForwardAck(OverlayWorker worker, string replyId);`
- `Forwarded(OverlayWorker worker, string digestType, byte[] binaryEnvelope, string envelopeId);`
- `Failed(OverlayWorker worker, Gossiperl.Client.Exceptions.GossiperlClientException error);`

## Subscribing / unsubscribing

Subscribing:

    supervisor.Subscribe( string overlayName, List<string> events );

Unsubscribing:

    supervisor.Unsubscribe( string overlayName, List<string> events );

## Disconnecting from and overlay

    supervisor.Disconnect( string overlayName );

## Additional operations

### Checking current client state

    Gossiperl.Client.Status supervisor.CurrentStatus( string overlayName );

### Get the list of current subscriptions

    List<string> supervisor.Subscriptions( string overlayName );

### Sending arbitrary digests

    using Gossiperl.Client.Serialization.CustomDigestField;
    using System.Collections.Generic;
    
    string overlayName = "gossiper_overlay_remote";
    List<CustomDigestField> digestData = new List<CustomDigestField>();
    digestData.Add(new CustomDigestField("field_name", "some value for the field", "string", (short)1));
    digestData.Add(new CustomDigestField("integer_field", 1234, "i32", (short)2));
    supervisor.Send( overlayName );

Where `<type>` is one of the supported serializable types:

- `string`: `string`
- `bool`: `bool`
- `byte`: `sbyte`
- `double`: `double`
- `i16`: `short`
- `i32`: `int`
- `i64`: `long`

Other Thrift types are not supported. `CustomDigestField`s constructor is:

    CustomDigestField(string fieldName, object value, string type, short fieldOrder)

Where `fieldOrder` is a Thrift field ID.

### Reading custom digests

To read a custom digest, assuming that this is the binary envelope data received via `Forwarded` event, it can be read in the following manner:

    supervisor.Read("expectedDigestType", binaryData, digestInfo);

Where binary data is `byte[]` buffer of the notification and `digestInfo` has the same format as `digestData` as in the example above.

## Running tests

    nunit-console tests/gossiperl-client-tests.csproj

Tests assume an overlay with the details specified in the `tests/ProcessTest.cs` running.

## License

The MIT License (MIT)

Copyright (c) 2014 Radoslaw Gruchalski <radek@gruchalski.com>

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
