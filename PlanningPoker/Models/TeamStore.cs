using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNet.SignalR;

namespace PlanningPoker.Controllers
{
    public class TeamStore
    {
        // use Lazy as singleton pattern for thread safety
        private static Lazy<TeamStore> _instance = new Lazy<TeamStore>();

        private ConcurrentDictionary<string, Team> _teams;

        public TeamStore()
        {
            _teams = new ConcurrentDictionary<string, Team>();
        }

        private Team GetTeamByConnectionId(string connectionId)
        {
            return _teams.Values.SingleOrDefault(t => t.Players.Any(p => p.Key == connectionId));
        }

        public void NewTeam(string teamName, string playerName, int duration, bool participating, string connectionId)
        {
            if (_teams.ContainsKey(teamName))
                throw new Exception("Team name already in use");

            var hub = GlobalHost.ConnectionManager.GetHubContext<TeamHub>().Clients;
            Team team = new Team(teamName, duration, hub);

            var mode = participating ? ClientMode.ParticipatingHost : ClientMode.Host;
            team.AddClient(playerName, connectionId, mode);

            _teams[teamName] = team;
        }

        public void NewPlayer(string teamName, string playerName, string connectionId)
        {
            _teams[teamName].AddClient(playerName, connectionId, ClientMode.Player);
        }

        public void NewViewer(string teamName, string playerName, string connectionId)
        {
            _teams[teamName].AddClient(playerName, connectionId, ClientMode.Viewer);
        }

        public void SubmitScore(string score, string connectionId)
        {
            var team = GetTeamByConnectionId(connectionId);
            if (team != null)
                team.SubmitCardScore(score, connectionId);
        }

        public void NewRound(string connectionId)
        {
            var team = GetTeamByConnectionId(connectionId);
            if (team != null)
                team.Reset();
        }

        public void RemovePlayer(string connectionId)
        {
            var team = GetTeamByConnectionId(connectionId);
            if (team == null)
                return;

            team.RemovePlayer(connectionId);

            if (!team.Players.Any())
                _teams.TryRemove(team.Name, out team);
        }
        
        public void Start(string connectionId)
        {
            var team = GetTeamByConnectionId(connectionId);
            if (team != null)
                team.Start();
        }

        public void Stop(string connectionId)
        {
            var team = GetTeamByConnectionId(connectionId);
            if (team != null)
                team.Stop();
        }

        public void Pause(string connectionId)
        {
            var team = GetTeamByConnectionId(connectionId);
            if (team != null)
                team.Pause();
        }

        public static TeamStore Instance
        {
            get { return _instance.Value; }
        }
    }
}
