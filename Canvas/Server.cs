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

namespace Lines.Canvas
{
    public class Server
    {
        protected static List<string> messages = new List<string>();

        protected static Dictionary<string, int> playerLastIndex = new Dictionary<string, int>();

        protected static Regex messageRegex = new Regex(@"\#[0-9a-f]{6}\|\d+\|\d+\|(0|1)");

        protected static List<WebSocket> connections = new List<WebSocket>();
        
        protected WebSocket socket;
        
        protected Server(WebSocket socket) {
            this.socket = socket;
            
            connections.Add(socket);
        }
        
        static async Task Acceptor(HttpContext http, Func<Task> next)
        {
            if (!http.WebSockets.IsWebSocketRequest) {
                await http.Response.WriteAsync("Not a WebSocket request.");
                
                return;
            }
    
            var socket = await http.WebSockets.AcceptWebSocketAsync();
            
            if(socket != null && socket.State == WebSocketState.Open) {
                var server = new Server(socket);
            
                await server.HandleSocket();
            }
        }

        public static void Map(IApplicationBuilder app) {
            app.UseWebSockets();
            app.Use(Acceptor);
        }
        
        async public Task HandleSocket() {
            var buffer = new byte[1024];
            var receive = new ArraySegment<byte>(buffer);
        
            while (this.socket.State == WebSocketState.Open)
            {
                var incoming = await this.socket.ReceiveAsync(receive, CancellationToken.None);
                
                var recd = receive.Array;
                Array.Resize(ref recd, incoming.Count);
                Receive(Encoding.UTF8.GetString(recd));
            }
        }
        
        async protected Task Send(string message) {
            var bytes = Encoding.UTF8.GetBytes(message);
            var outgoing = new ArraySegment<byte>(bytes, 0, bytes.Length);
            
            await this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
        async protected Task Broadcast(string message) {
            var bytes = Encoding.UTF8.GetBytes(message);
            var outgoing = new ArraySegment<byte>(bytes, 0, bytes.Length);
            
            var sends = connections.Select(socket => { 
                if(socket.State == WebSocketState.Open) {
                    return this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
                } else {
                    return Task.FromResult(0);
                }
            });
            
            await Task.WhenAll(sends);
        }
                
        async protected void Clear(string player)
        {
            messages = new List<string>();
            playerLastIndex = new Dictionary<string, int>();

            await Broadcast(player + '>' + "clear");
        }

        async protected void Receive(string data) 
        {
            string[] chunks = data.Split('>');
            
            if(chunks.Count() < 2) {
                return;
            }
            
            string player = chunks[0];
            string drawMessage = chunks[1];

            if(drawMessage == "clear")
            {
                Clear(player);

                return;
            }

            if(drawMessage == "load")
            {
                Load(player); 

                return; 
            }
                    
            if(messageRegex.IsMatch(drawMessage.ToLower()))
            {
                messages.Add(data);

                string color = drawMessage.Split('|').First();
                                 
                await Broadcast(data);

                Parallel.ForEach(playerLastIndex.Values, v => { v++; });
            }
        }

        async protected void Load(String player)
        {
            if (!playerLastIndex.Keys.Contains(player))
            {
                playerLastIndex[player] = 0;

                while (playerLastIndex[player] < messages.Count())
                {
                    await Send(messages[playerLastIndex[player]]);

                    playerLastIndex[player]++;
                }
            }
        }
    }
}
