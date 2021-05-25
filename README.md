# RTCAudioRecording
this is a simple console app that uses [MixedReality-WebRTC](https://github.com/microsoft/MixedReality-WebRTC) to capture and record audio stream from an RTC service
the code is generic and can be modified to fit your own business logic and need.

## Generic steps
### Get ICE servers
first, get ICE servers and their credentials
### Initialize peer connection
after initializing peer connection, create an audio transceiver and choose the transceiver direction (Send, Receive or both), in this case, it's Receive.
then create a data channel
### Create a web socket with the signaling URL and connect
important note: wss protocol require secure connection, in my case, setting signaling.SslConfiguration.EnabledSslProtocols to TLS 1.2 worked
the event handler OnOpen fires when a socket is created and connected, and when it fires you can call peerConnection.CreateOffer() to trigger LocalSdpReadytoSend event handler
in this delegate you can write your own offer message with the received sdp and then send it to the WebSocket, the WebSocket responds, and this triggers the web socket event handler OnMessage
if it does not get triggers, make sure that the payload sent in LocalSdpReadytoSend is correct.
in the event handler OnMessage you can handle the received answer according to your business logic, but mainly you should create an answer message and set the remote description to the answer message.
### Receive audio
peer connection event handler AudioTrackAdded is triggered when an audio track is added, in this case it's the remote audio track
track.AudioFrameReady is delegate that's triggers when audio data is received
and track.OutputToDevice(true) is a method that outputs the received audio to the device's speakers.





