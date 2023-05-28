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
using System.Linq;
using EasyFarm.Classes;
using EasyFarm.Context;
using EasyFarm.UserSettings;
using MemoryAPI;
using MemoryAPI.Navigation;
using Player = EasyFarm.Classes.Player;

namespace EasyFarm.States
{
    /// <summary>
    ///     Moves to target enemies.
    /// </summary>
    public class ApproachState : BaseState
    {
        const double BACKUP_THRESHOLD = 0.25;
        public override bool Check(IGameContext context)
        {
            if (new RestState().Check(context)) return false;

            // Make sure we don't need trusts
            if (new SummonTrustsState().Check(context)) return false;

            // Target dead or null.
            if (!context.Target.IsValid) return false;

            // We should approach mobs that have aggroed or have been pulled. 
            if (context.Target.Status.Equals(Status.Fighting)) return true;

            // Get usable abilities. 
            var usable = context.Config.BattleLists["Pull"].Actions
                .Where(x => ActionFilters.BuffingFilter(context.API, x));

            // Approach when there are no pulling moves available. 
            if (!usable.Any()) return true;

            // Approach mobs if their distance is close and approach is enabled.
            if (context.Config.IsApproachEnabled)
                return
                    (context.Target.isLocked && context.Config.PullFallback == PullFallbackType.Approach)
                    || context.Target.Distance < 8;
            return false;
        }

        public override void Run(IGameContext context)
        {
            // Target mob if not currently targeted. 
            Player.SetTarget(context.API, context.Target);

            // Has the user decided that we should approach targets?
            if (context.Config.IsApproachEnabled)
            {
                //If the player is fighting right on top of the target, then hold the down key to backup. This relies on target lock.
                double wpDist = context.Target.Distance;
                if (wpDist <= BACKUP_THRESHOLD && context.Player.Status == Status.Fighting)
                {
                    context.API.Windower.SendKeyDown(EliteMMO.API.Keys.NUMPAD2);
                    context.Memory.IsBackingUp = true;
                    return;
                } else if (context.Memory.IsBackingUp)
                {
                    context.API.Windower.SendKeyUp(EliteMMO.API.Keys.NUMPAD2);
                    context.Memory.IsBackingUp = false;
                }

                // Move to target if out of melee range.
                if (wpDist < context.Config.MeleeDistance)
                {
                    context.API.Navigator.FaceHeading(context.Target.Position);
                    context.API.Navigator.Reset();
                    context.API.Follow.Reset();

                    // Has the user decided we should engage in battle. 
                    if (context.Config.IsEngageEnabled)
                        if (!context.API.Player.Status.Equals(Status.Fighting) && context.Target.Distance < 25)
                            context.API.Windower.SendString(Constants.AttackTarget);

                    return;
                }
                else if (wpDist > context.Config.RouteNavMeshTolerance + context.Config.MeleeDistance)
                {
                    var path = context.NavMesh.FindPathBetween(context.API.Player.Position, context.Target.Position);
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
                        else
                        {
                            context.API.Navigator.DistanceTolerance = context.Config.MeleeDistance;
                        }

                        if (path.Count > 0)
                        {
                            Route.NavigateTo(context, path.Peek());
                            return;
                        }
                    }
                }
                Route.NavigateTo(context, context.Target.Position);
            }
            else
            {
                // Face mob. 
                context.API.Navigator.FaceHeading(context.Target.Position);

                // Has the user decided we should engage in battle. 
                if (context.Config.IsEngageEnabled)
                    if (!context.API.Player.Status.Equals(Status.Fighting) && context.Target.Distance < 25)
                        context.API.Windower.SendString(Constants.AttackTarget);
            }
        }
    }
}