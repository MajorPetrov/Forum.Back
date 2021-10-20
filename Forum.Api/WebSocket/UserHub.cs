using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ForumJV.Data.Services;
using ForumJV.Extensions;

namespace ForumJV.WebSocket
{
    public class UserHub : Hub
    {
        private readonly IForum _forumService;
        private readonly IPost _postService;

        public UserHub(IForum forumService, IPost postService)
        {
            _forumService = forumService;
            _postService = postService;

            InitializeForums().Wait();
            InitializePosts();
        }

        /// <summary>
        /// S'active automatiquement quand un utilisateur quitte un forum ou un sujet.
        /// Détermine dans un premier temps le forum sur lequel le client était connecté et envoie aux autres clients du forum
        /// le compteur mis à jour.
        /// Détermine ensuite les sujets actifs et envoie aux utilisateurs connectés sur le forum en question l'identifiant du sujet mis à jour et son compteur de co
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var activePosts = UserHandler.PostConnected.Values.Where(userPosts => userPosts.Count > 0);
            var ipAddress = Context.GetHttpContext().GetRemoteIPAddress().ToString();

            // Traitement des forums
            var forumId = UserHandler.ForumConnected.FirstOrDefault(users => users.Value.ContainsKey(Context.ConnectionId)).Key;

            if (forumId > 0)
            {
                UserHandler.ForumConnected[forumId].Remove(Context.ConnectionId);

                var differentIps = UserHandler.ForumConnected[forumId].Values.Distinct().Count();

                await Clients.Clients(UserHandler.ForumConnected[forumId].Keys.ToList()).SendAsync("TotalConnectedUsers", differentIps);
            }

            // Traitement des posts
            var postId = UserHandler.PostConnected.FirstOrDefault(dict => dict.Value.ContainsKey(Context.ConnectionId)).Key;

            if (postId > 0)
            {
                UserHandler.PostConnected[postId].Remove(Context.ConnectionId);

                // S'il n'y a plus personne de co sur le sujet, autant virer le dico
                if (!UserHandler.PostConnected[postId].Any())
                    UserHandler.PostConnected.Remove(postId);

                var differentIps = UserHandler.PostConnected.GetValueOrDefault(postId);

                await Clients.Clients(UserHandler.ForumConnected[forumId].Keys.ToList()).SendAsync("UpdateCounterPost", postId, differentIps == null ? 0 : UserHandler.PostConnected[postId].Values.Distinct().Count());
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Méthode à appeler quand un utilisateur ouvre un forum.
        /// Si l'adresse IP de l'utilisateur se trouve déjà dans la collection, le compteur n'est envoyé qu'au client en question.
        /// Dans le cas contraire, le compteur est envoyé à tous les utilisateurs connectés sur le forum.
        /// </summary>
        /// <param name="forumId">L'identifiant du forum</param>
        /// <returns></returns>
        public async Task UserEnterForum(int forumId)
        {
            bool isNewIp;
            var userIpAddress = Context.GetHttpContext().GetRemoteIPAddress().ToString();

            if (UserHandler.ForumConnected[forumId].ContainsValue(userIpAddress))
                isNewIp = false;
            else
                isNewIp = true;

            UserHandler.ForumConnected[forumId].Add(Context.ConnectionId, userIpAddress);

            var amoutForumIpAddresses = UserHandler.ForumConnected[forumId].Values.Distinct().Count();
            var postIpAddresses = new Dictionary<int, int>();

            foreach (var postUsers in UserHandler.PostConnected)
            {
                var differentIps = postUsers.Value.Values.Distinct().Count();

                if (differentIps > 0)
                    postIpAddresses.Add(postUsers.Key, differentIps);
            }

            var allPostUsers = JsonConvert.SerializeObject(postIpAddresses); // Objet JSON contenant les associations des identifiants de sujet et du nombre de co sur chacun d'eux

            await Clients.Client(Context.ConnectionId).SendAsync("TotalConnectedUsers", amoutForumIpAddresses, allPostUsers);

            if (isNewIp)
                await Clients.Clients(UserHandler.ForumConnected[forumId].Keys.ToList()).SendAsync("TotalConnectedUsers", amoutForumIpAddresses, allPostUsers);
        }

        /// <summary>
        /// Méthode à appeler quand un utilisateur quitte un forum (fermeture de l'onglet, par exemple).
        /// Le Hub envoie à tous les utilisateurs connectés au forum en question son identifiant, ainsi que le nouveau nombre de connectés sur ce dernier.
        /// </summary>
        /// <param name="forumId">L'identifiant du forum</param>
        /// <returns></returns>
        public async Task UserLeaveForum(int forumId)
        {
            var ipAddress = Context.GetHttpContext().GetRemoteIPAddress().ToString();

            UserHandler.ForumConnected[forumId].Remove(Context.ConnectionId);

            if (!UserHandler.ForumConnected[forumId].ContainsValue(ipAddress))
                await Clients.Clients(UserHandler.ForumConnected[forumId].Keys.ToList()).SendAsync("TotalConnectedUsers", UserHandler.ForumConnected[forumId].Values.Distinct().Count());

            if (!UserHandler.ForumConnected[forumId].Any())
                UserHandler.ForumConnected.Remove(forumId);
        }

        /// <summary>
        /// Méthode à appeler quand un utilisateur ouvre un sujet.
        /// Si l'adresse IP de l'utilisateur se trouve déjà dans la collection, elle n'est pas mise à jour et rien n'est renvoyé.
        /// Dans le cas contraire, l'association de l'identifiant de connection de l'utilisateur et de son adresse IP est ajoutée à la collection,
        /// et le Hub envoie à tous les utilisateurs connectés au sujet en question l'identifiant du sujet, ainsi que le nouveau nombre de connectés sur ce dernier.
        /// </summary>
        /// <param name="forumId">L'identifiant du forum sur lequel le sujet se trouve</param>
        /// <param name="postId">L'identifiant du sujet que l'utilisateur a ouvert</param>
        /// <returns></returns>
        public async Task UserEnterPost(int forumId, int postId)
        {
            var userIpAddress = Context.GetHttpContext().GetRemoteIPAddress().ToString();

            // S'il le sujet n'existe dans le dictionnaire, c'est qu'il n'y a personne de co
            if (!UserHandler.PostConnected.ContainsKey(postId))
                UserHandler.PostConnected.Add(postId, new Dictionary<string, string>());

            if (UserHandler.PostConnected[postId].ContainsValue(userIpAddress))
                return;

            UserHandler.PostConnected[postId].Add(Context.ConnectionId, userIpAddress);

            var ipAddresses = UserHandler.PostConnected[postId].Values.Distinct();

            await Clients.Clients(UserHandler.ForumConnected[forumId].Keys.ToList()).SendAsync("UpdateCounterPost", postId, ipAddresses.Count());
        }

        /// <summary>
        /// Méthode à appeler quand un utilisateur quitte un sujet (fermeture de l'onglet, par exemple).
        /// Le Hub envoie à tous les utilisateurs connectés au sujet en question l'identifiant du sujet, ainsi que le nouveau nombre de connectés sur ce dernier.
        /// </summary>
        /// <param name="forumId">L'identifiant du forum sur lequel le sujet se trouve</param>
        /// <param name="postId">L'identifiant du sujet que l'utilisateur a quitté</param>
        /// <returns></returns>
        public async Task UserLeavePost(int forumId, int postId)
        {
            var ipAddress = Context.GetHttpContext().GetRemoteIPAddress().ToString();

            UserHandler.PostConnected[postId].Remove(Context.ConnectionId);

            if (!UserHandler.PostConnected[postId].ContainsValue(ipAddress))
                await Clients.Clients(UserHandler.ForumConnected[forumId].Keys.ToList()).SendAsync("UpdateCounterPost", postId, UserHandler.PostConnected[postId].Values.Distinct().Count());

            // S'il n'y a plus personne de co sur le sujet, autant virer le dico
            if (!UserHandler.PostConnected[postId].Any())
                UserHandler.PostConnected.Remove(postId);
        }

        private async Task InitializeForums()
        {
            var forumNumber = await _forumService.Count();

            if (UserHandler.ForumConnected != null && UserHandler.ForumConnected.Count == forumNumber)
                return;

            UserHandler.ForumConnected = new Dictionary<int, Dictionary<string, string>>();

            for (int i = 1; i <= forumNumber; i++)
                UserHandler.ForumConnected.Add(i, new Dictionary<string, string>());
        }

        private void InitializePosts()
        {
            if (UserHandler.PostConnected == null)
                UserHandler.PostConnected = new Dictionary<int, Dictionary<string, string>>();
        }
    }
}