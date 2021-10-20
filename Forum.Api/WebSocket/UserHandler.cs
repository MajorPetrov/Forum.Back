using System.Collections.Generic;

namespace Forum.WebSocket
{
    public static class UserHandler
    {
        /// <summary>
        /// Le dictionnaire contient l'identifiant du forum comme clé et en valeur un dictionnaire d'utilisateurs connectés.
        /// Un utilisateur est représenté par l'association de son ConnectionId et de son IpAddress.
        /// </summary>
        /// <value></value>
        public static Dictionary<int, Dictionary<string, string>> ForumConnected { get; set; }

        /// <summary>
        /// Le dictionnaire contient l'identifiant du sujet comme clé et en valeur un dictionnaire d'utilisateurs connectés.
        /// Un utilisateur est représenté par l'association de son ConnectionId et de son IpAddress.
        /// </summary>
        /// <value></value>
        public static Dictionary<int, Dictionary<string, string>> PostConnected { get; set; }
    }
}