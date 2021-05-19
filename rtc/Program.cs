
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json;
using RTCAudioRecording;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace TestNetCoreConsole
{
     
    class Program
    {
    

        static async Task Main()
        {
            try
            {
                Console.WriteLine("Starting...");
                //create and Initialize capture object to record audio
                var waveFormat = new WaveFormat(44100, 32, 2, AudioEncoding.MpegLayer3);
                WasapiCapture capture = new WasapiCapture(true, AudioClientShareMode.Shared, 100, waveFormat);

                //initialize the selected device for recording
                capture.Initialize();
                //fill ice servers here 
                List<string> urls = new List<string>();


                using var pc = new PeerConnection();

                var config = new PeerConnectionConfiguration
                {
                    IceServers = new List<IceServer> {
                        new IceServer { Urls = urls,
                          
                        }
                    }
                    ,
                    BundlePolicy = BundlePolicy.MaxBundle
                };

                await pc.InitializeAsync(config);

                Console.WriteLine("Peer connection initialized.");


                //create audio transceiver
                Transceiver transceiver = pc.AddTransceiver(MediaKind.Audio);
                transceiver.DesiredDirection = Transceiver.Direction.ReceiveOnly;
                Console.WriteLine("Create audio transceiver ...");

                DataChannel chanel = await pc.AddDataChannelAsync("Data", true, true, cancellationToken: default);
                string url = "";
                WebSocketSharp.WebSocket signaling = new WebSocket(url);
                signaling.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

                signaling.OnMessage += async (sender, message) =>
                {

                    try
                    {
                        //response messages may differ from service provider to another, adjust WebsocketResponse object accordingly 
                        var messageObject = JsonConvert.DeserializeObject<WebsocketResponse>(message.Data);
                        var mess = new SdpMessage { Content = messageObject.Data.Sdp, Type = SdpMessage.StringToType("answer") };

                        if (!string.IsNullOrEmpty(mess.Content))
                        {
                            Console.WriteLine("Sdpmessage: {0}, Type: {1}", mess.Content, mess.Type);
                            await pc.SetRemoteDescriptionAsync(mess);
                            if (mess.Type == SdpMessageType.Answer)
                            {
                                bool res = pc.CreateAnswer();
                                Console.WriteLine("Answer created? {0}", res);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                };


                signaling.OnError += (sender, e) =>
                {
                    Console.WriteLine(e.Message, e.Exception);
                };
                signaling.OnOpen += (sender, e) =>
                {
                    pc.CreateOffer();
                    Console.WriteLine("open");
                };
                signaling.Connect();

                transceiver.Associated += (tranciever) =>
                {
                    Console.WriteLine("Transivier: {0}, {1}", tranciever.Name, tranciever.StreamIDs);
                };



                pc.LocalSdpReadytoSend += (SdpMessage message) =>
                {
                    Console.WriteLine(message.Content);

                    //modify the offer message according to your need 

                    var data = new
                    {
                        streamId = "",
                        sdp = message.Content
                    };
                    var payload = JsonConvert.SerializeObject(new
                    {
                        type = "cmd",
                        transId = 0,
                        name = "view",
                        data = data
                    });
                    Console.WriteLine("Sdp offer to send: " + payload);

                    signaling.Send(payload);
                };


                pc.RenegotiationNeeded += () =>
                {
                    Console.WriteLine("Regotiation needed");
                };

                //when a remote audio track is added, start recording 
                pc.AudioTrackAdded += (RemoteAudioTrack track) =>
                {


                    //create a wavewriter to write the data to
                    WaveWriter w = new WaveWriter("audio.mp3", capture.WaveFormat);

                    //setup an eventhandler to receive the recorded data
                    capture.DataAvailable += (s, e) =>
                    {
                        //save the recorded audio
                        w.Write(e.Data, e.Offset, e.ByteCount);
                    };

                    //start recording
                    capture.Start();
                    //this should output the sound 
                    track.OutputToDevice(true);

                    //track.AudioFrameReady += (AudioFrame frame) =>
                    //{
                    //you can print anything here if you want to make sure that's you're recieving audio 

                    //};

                };


                pc.Connected += () =>
                {
                    Console.WriteLine("Connected");
                    Console.WriteLine(pc.DataChannels.Count);


                };
                pc.IceStateChanged += (IceConnectionState newState) =>
                {
                    Console.WriteLine($"ICE state: {newState}");
                };

                Console.WriteLine("Press enter to stop");
                Console.ReadLine();

                //stop recording
                capture.Stop();
                pc.Close();
                signaling.Close();
                Console.WriteLine("Program termined.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


    }
}
