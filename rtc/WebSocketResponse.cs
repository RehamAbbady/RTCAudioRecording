using System;
using System.Collections.Generic;
using System.Text;

namespace RTCAudioRecording
{
    public class WebsocketResponse
    {
        public string Type { get; set; }
        public int TransId { get; set; }
        public WebsocketResponseData Data { get; set; }

    }
    public class WebsocketResponseData
    {
        public string Sdp { get; set; }
        public string SubscriberId { get; set; }
        public string StreamId { get; set; }
        public long StreamViewId { get; set; }


    }
}
