// ///////////////////////////////////////////////////////////////////
// This file is a part of EasyFarm for Final Fantasy XI
// Copyright (C) 2013 Mykezero
//  
// EasyFarm is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// EasyFarm is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// If not, see <http://www.gnu.org/licenses/>.
// ///////////////////////////////////////////////////////////////////
using EasyFarm.Classes;
using EasyFarm.Context;
using MemoryAPI;
using MemoryAPI.Navigation;
using Player = EasyFarm.Classes.Player;

namespace EasyFarm.States
{
    /// <summary>
    ///     Moves to target enemies.
    /// </summary>
    public class FollowState : BaseState
    {
        private IUnit FollowedPlayer;

        public override void Enter(IGameContext context)
        {
            // Stand up from resting. 
            if (context.Player.Status == Status.Healing) Player.Stand(context.API);

            // Disengage an invalid target. 
            if (context.Player.Status == Status.Fighting) Player.Disengage(context.API);
        }

        public override bool Check(IGameContext context)
        {
            // Do not follow during fighting. 
            if (context.IsFighting) return false;

            // Do not follow when resting. 
            if (new RestState().Check(context)) return false;

            // Avoid following empty units. 
            if (string.IsNullOrWhiteSpace(context.Config.FollowedPlayer)) return false;

            // Names are only valid if it contains at least 3 characters
            if (context.Config.FollowedPlayer.Length < 3) return false;

            // Get the player specified in user settings.
            if (FollowedPlayer == null || FollowedPlayer.Name != context.Config.FollowedPlayer)
                FollowedPlayer = context.Memory.UnitService.GetUnitByName(context.Config.FollowedPlayer);

            // If no player is nearby, return. 
            if (FollowedPlayer == null) return false;

            // Run the follow. This also responsible for stopping navigation
            return true;
        }

        public override void Run(IGameContext context)
        {
            if (FollowedPlayer == null)
                return;

            if (FollowedPlayer.Distance < context.Config.FollowDistance)
            {
                // Sanity check
                if (Player.Instance.IsMoving)
                {
                    context.API.Navigator.Reset();
                    context.API.Follow.Reset();
                }
            }
            else
            {
                // Follow the player. 
                context.API.Navigator.DistanceTolerance = context.Config.FollowDistance;
                var path = context.NavMesh.FindPathBetween(context.API.Player.Position, FollowedPlayer.Position);
                if (path.Count > 0)
                {
                    while (path.Count > 0 && path.Peek().Distance(context.API.Player.Position) <= context.Config.RouteTolerance)
                    {
                        path.Dequeue();
                    }
                    if (path.Count > 1)
                    {
                        context.API.Navigator.DistanceTolerance = context.Config.RouteTolerance;
                    }

                    if (path.Count > 0)
                    {
                        Route.NavigateTo(context, path.Peek());
                    }
                    else
                    {
                        context.API.Navigator.Reset();
                        context.API.Follow.Reset();
                    }
                }
            }
        }
    }
}