using System;
using System.Collections.Generic;
using System.Linq;

namespace Rooms.Canvas {
    public class Room {
        protected List<Message> messages = new List<Message>();

        protected List<Player> players = new List<Player>();
        
        public IList<Message> Messages { get { return messages.AsReadOnly(); } }
        
        public void Clear(Message message)
        {
            messages = new List<Message>();
            AddMessage(message);
        }
        
        public void Draw(Message message) {
            AddMessage(message);
        }
        
        public void Add(Player player) {
            players.Add(player);
        }
        
        public void Remove(Player player) {
            players.Remove(player);
        }
        
        public void AddMessage(Message message) {
            messages.Add(message);
            players.ForEach(p => p.Add(message));
        }
    }
}