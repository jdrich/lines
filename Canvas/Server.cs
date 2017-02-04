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
using Microsoft.Extensions.Logging;

namespace Rooms.Canvas
{
    public class Server
    {
        protected static Random seed = new Random();
        
        protected List<Room> rooms;
        
        public List<Room> Rooms { get { return rooms; } }
        
        public Server() {
            rooms = new List<Room>() {
                new Room(),
                new Room(),
                new Room(),
                new Room(),
                new Room(),
                new Room(),
                new Room(),
                new Room(),
                new Room(),
                new Room()
            };
        }
        
        public static void Map(IApplicationBuilder app) {
            app.UseWebSockets();
            app.Use((new Server()).Listen);
        }
        
        public async Task Listen(HttpContext http, Func<Task> next)
        {
            if (!http.WebSockets.IsWebSocketRequest) {
                await http.Response.WriteAsync("Not a WebSocket request.");
                
                return;
            }
    
            var socket = await http.WebSockets.AcceptWebSocketAsync();
            
            if(socket != null && socket.State == WebSocketState.Open) {
                var player = new Player(this, socket, randomRoom());
                
                var waits = new List<Task>() {
                    player.Send(),
                    player.Receive()
                };
            
                await Task.WhenAll(waits);
            }
        }
        
        public Room randomRoom() {
            return rooms[seed.Next(rooms.Count())];
        }
        
        public bool hasRoom(int roomIndex) {
            return roomIndex < rooms.Count();
        }
        
        public Room getRoom(int roomIndex) {
            return rooms[roomIndex];
        }
    }
}
