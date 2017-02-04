using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rooms.Canvas {
    public class Message {
        public string raw;
        public string player;
        public string action;
        public string rawBody;
        public List<string> body;
        
        public Message(string message) {
            this.raw = message;
            
            var chunks = message.Split('>');
            
            if(chunks.Length == 3) {
                player = chunks[0];
                action = chunks[1];
                rawBody = chunks[2];
                body = chunks[2].Split('|').ToList();
            }
        }
        
        public override string ToString() {
            StringBuilder message = new StringBuilder();
            message.Append(player);
            message.Append(">");
            message.Append(action);
            message.Append(">");
            message.Append(rawBody);
            
            return message.ToString();
        }
        
        public ArraySegment<byte> ToSegment() {
            var bytes = Encoding.UTF8.GetBytes(ToString());
            
            return new ArraySegment<byte>(bytes, 0, bytes.Length);
        }
    }
}