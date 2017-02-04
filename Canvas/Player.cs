using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;

namespace Rooms.Canvas {
    public class Player {
        protected static Regex messageRegex = new Regex(@"\#[0-9a-f]{6}\|\d+\|\d+\|(0|1)");

        protected string playerId;
        protected Server server;
        protected Room room;
        protected WebSocket socket;
        protected List<Message> messages;
        
        public Player(Server server, WebSocket socket, Room room) {
            this.server = server;
            this.socket = socket;
            
            messages = new List<Message>();
            
            this.room = room;
        }
        
        public void Load(string newPlayerId) {
            playerId = newPlayerId;
            
            moveTo(room);
        }
        
        public void Add(Message message) {
            if(message.player != playerId) {
                messages.Add(message);
            }
        }
        
        public void moveTo(Room newRoom) {
            if(playerId != null) {
                if(room != null) {
                    room.Remove(this);
                }
                
                room = newRoom;
                room.Add(this);
                
                var moved = new Message(playerId + ">moved>" + server.Rooms.IndexOf(room));
                
                messages.Add(moved);
                
                messages.AddRange(room.Messages);
            }
        }
        
        async public Task Receive() {
            await Task.Yield();
            
            var buffer = new byte[1024];
            var receive = new ArraySegment<byte>(buffer);
        
            while (socket.State == WebSocketState.Open)
            {
                var result = await this.socket.ReceiveAsync(receive, CancellationToken.None);
                
                if(result.MessageType != WebSocketMessageType.Close) {
                    var recd = receive.Array;
                    Array.Resize(ref recd, result.Count);
                    Handle(Encoding.UTF8.GetString(recd));
                } else {
                    room.Remove(this);
                }
            }
        }
        
        async public Task Send() {
            await Task.Yield();
            
            var buffer = new byte[1024];
            var send = new ArraySegment<byte>(buffer);
                
            while (socket.State == WebSocketState.Open)
            {
                while(messages.Count() > 0) {
                    var message = messages[0];
                    messages.RemoveAt(0);
                    
                    buffer = Encoding.UTF8.GetBytes(message.raw);
                    send = new ArraySegment<byte>(buffer, 0, buffer.Length);
                    
                    await socket.SendAsync(send, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
        
        protected void Handle(string data) 
        {
            var message = new Message(data);
            
            string drawMessage = message.action;

            switch (message.action.ToLower()) {
                case "clear":
                    room.Clear(message);
                    
                    break;
                case "load":
                    Load(message.player);
                
                    break;
                case "move":
                    int roomNo;
                
                    if(!Int32.TryParse(message.rawBody, out roomNo)) {
                        break;
                    }
                
                    if(server.hasRoom(roomNo)) {
                        moveTo(server.getRoom(roomNo));
                    }
                
                    break;
                case "draw":
                    if(messageRegex.IsMatch(message.rawBody.ToLower())) {
                        room.Draw(message);
                    }
                    
                    break;
            }    
        }
    }
}