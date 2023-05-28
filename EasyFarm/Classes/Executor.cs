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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using EasyFarm.Parsing;
using EasyFarm.States;
using EasyFarm.UserSettings;
using EliteMMO.API;
using MemoryAPI;
using MemoryAPI.Navigation;
using NLog.Targets;
using static EliteMMO.API.EliteAPI;
using StatusEffect = MemoryAPI.StatusEffect;

namespace EasyFarm.Classes
{   
    public class Executor
    {
        private readonly IMemoryAPI _fface;
        private const int RANGED_TOTAL_TIME = 10;
        private Regex rChatCannotSee = new Regex("You cannot see");
        private Regex rChatTooFar = new Regex("too far away.");
        private Regex rChatOOR = new Regex("out of range.");
        private Regex rChatRangeInterrupted = new Regex("interrupt your aim.");
        private Regex rChatRangeNoWeapon = new Regex("You do not have an appropriate ranged weapon");
        private Regex rChatTooSoon = new Regex("You must wait longer to perform that action.");
        private Regex rChatCommandError = new Regex("A command error occurred.");
        private Regex rChatUnableToUseAbil = new Regex("Unable to use job ability.");

        public String LastCommand { get; set; }

        public Executor(IMemoryAPI fface)
        {
            _fface = fface;
        }

        public void UseActions(IEnumerable<BattleAbility> actions)
        {
            if (actions == null) throw new ArgumentNullException(nameof(actions));

            foreach (var action in actions.ToList())
            {
                if (!ActionFilters.BuffingFilter(_fface, action))
                {
                    continue;
                }

                if (!CastSpell(action)) continue;

                action.Usages++;
                action.LastCast = DateTime.Now.AddSeconds(action.Recast);

                TimeWaiter.Pause(Config.Instance.GlobalCooldown);
            }
        }

        public void UseBuffingActions(IEnumerable<BattleAbility> actions)
        {
            if (actions == null) throw new ArgumentNullException(nameof(actions));

            var castables = actions.ToList();

            while (castables.Count > 0)
            {
                foreach (var action in castables.ToList())
                {
                    if (!ActionFilters.BuffingFilter(_fface, action))
                    {
                        castables.Remove(action);
                        continue;
                    }

                    if (!CastSpell(action)) continue;

                    castables.Remove(action);
                    action.Usages++;
                    action.LastCast = DateTime.Now.AddSeconds(action.Recast);

                    TimeWaiter.Pause(Config.Instance.GlobalCooldown);
                }
            }
        }   

        public bool UseTargetedActions(EasyFarm.Context.IGameContext context, IEnumerable<BattleAbility> actions, IUnit target, bool OnlyOne = false)
        {
            if (actions == null) throw new ArgumentNullException(nameof(actions));
            if (target == null) throw new ArgumentNullException(nameof(target));

            //Apply priority
            var sorted = actions.OrderByDescending(x => x.Priority);
            var success = false;

            foreach (var action in sorted)
            {
                var isInRange = MoveIntoActionRange(context, target, action);

                _fface.Navigator.FaceHeading(target.Position);
                Player.SetTarget(_fface, target);

                if (isInRange)
                {

                    _fface.Navigator.Reset();
                    _fface.Follow.Reset();
                    TimeWaiter.Pause(100);

                    if (ResourceHelper.IsSpell(action.AbilityType))
                    {
                        success |= CastSpell(action);
                    }
                    else if (action.AbilityType == Parsing.AbilityType.Range)
                    {
                        success |= CastRanged(action, target);
                    }
                    else
                    {
                        success |= CastAbility(action, target);
                    }

                    action.Usages++;
                    action.LastCast = DateTime.Now.AddSeconds(action.Recast);

                    TimeWaiter.Pause(Config.Instance.GlobalCooldown);
                } else if (action.AllowApproach) {
                    //partial success; we are gap closing
                    success = true;
                }

                //If only one successful action should be taken, then cancel out
                if (OnlyOne && success)
                    return success;
            }
            return success;
        }

        private bool MoveIntoActionRange(EasyFarm.Context.IGameContext context, IUnit target, BattleAbility action)
        {
            if (target.Distance > action.Distance)
            {
                if (action.AllowApproach)
                {
                    if (target.Distance > context.Config.RouteNavMeshTolerance)
                    {
                        var path = context.NavMesh.FindPathBetween(context.API.Player.Position, context.Target.Position);
                        if (path.Count > 0)
                        {
                            if (path.Count > 1)
                            {
                                _fface.Navigator.DistanceTolerance = context.Config.RouteTolerance;
                            }
                            else
                            {
                                _fface.Navigator.DistanceTolerance = action.Distance;
                            }

                            while (path.Count > 0 && path.Peek().Distance(context.API.Player.Position) <= _fface.Navigator.DistanceTolerance)
                            {
                                path.Dequeue();
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
                        } else
                        {
                            Route.NavigateTo(context, target.Position);
                        }
                    } else
                    {
                        Route.NavigateTo(context, target.Position);
                    }
                }
                return false;
            }

            return true;
        }        

        private bool EnsureCast(string command)
        {            
            var previous = _fface.Player.CastPercentEx;
            var startTime = DateTime.Now;
            var interval = startTime.AddSeconds(3);

            while (DateTime.Now < interval)
            {
                while(Player.Instance.IsMoving)
                {
                    Player.StopRunning(_fface);
                }

                if (_fface.Player.Status == Status.Healing)
                {
                    Player.Stand(_fface);
                }

                if (_fface.Player.StatusEffects.Contains(StatusEffect.Chainspell))
                {
                    _fface.Windower.SendString(command);
                    return true;
                }                             

                if (Math.Abs(previous - _fface.Player.CastPercentEx) > .5) return true;
                SendCommand(command);
                TimeWaiter.Pause(500);
            }

            return false;
        }

        private bool MonitorCast()
        {
            var prior = _fface.Player.CastPercentEx;

            var stopwatch = new Stopwatch();

            while (stopwatch.Elapsed.TotalSeconds < 2)
            {
                if (Math.Abs(prior - _fface.Player.CastPercentEx) < .5)
                {
                    if (!stopwatch.IsRunning) stopwatch.Start();
                }
                else
                {
                    stopwatch.Reset();
                }

                prior = _fface.Player.CastPercentEx;

                TimeWaiter.Pause(100);
            }

            return Math.Abs(prior - 100) < .5;
        }

        private bool CastAbility(BattleAbility ability, IUnit target)
        {
            var startTime = DateTime.Now;
            var interval = startTime.AddSeconds(4);

            SendCommand(ability.Command);
            TimeWaiter.Pause(100);

            while (DateTime.Now < interval)
            {
                //When the action timer begins, assume the ability animation is playing
                if (_fface.Player.ActionTimer1 > 0)
                    return true;

                //Got out claimed!
                if (target.IsClaimed && !target.PartyClaim)
                    return false;

                //Died
                if (target.IsDead)
                    return false;

                //Inspect chat to determine if there are any errors
                var entries = _fface.Chat.ChatEntries
                    .ToList()
                    .Where(x => x.Timestamp > startTime).ToList();
                foreach (var entry in entries)
                {
                    if (rChatCommandError.IsMatch(entry.Text))
                        return false;
                    if (rChatOOR.IsMatch(entry.Text))
                        return false;
                    if (rChatUnableToUseAbil.IsMatch(entry.Text))
                        return false;
                }

                //Match the chat api polling rate.
                TimeWaiter.Pause(100);
            }
            return false;
        }

        private void SendCommand(String command)
        {
            LastCommand = command;
            _fface.Windower.SendString(command);
        }

        private bool CastSpell(BattleAbility ability)
        {
            if (EnsureCast(ability.Command)) return MonitorCast();
            return false;
        }

        private bool CastRanged(BattleAbility ability, IUnit target)
        {
            return EnsureRanged(ability, target);
        }

        private bool EnsureRanged(BattleAbility ability, IUnit target)
        {
            var command = ability.Command;
            var startTime = DateTime.Now;
            var interval = startTime.AddSeconds(RANGED_TOTAL_TIME);
            var docommand = true;
            DateTime startCommand = DateTime.Now;
            var attempts = 0;

            //Stop the player and wait until movement is atually halted
            while (Player.Instance.IsMoving)
                Player.StopRunning(_fface);

            //A successful ranged attack should take no longer than this amount of time
            while (DateTime.Now < interval)
            {
                if (docommand)
                {
                    _fface.Navigator.FaceHeading(target.Position);
                    TimeWaiter.Pause(100);
                    startCommand = DateTime.Now;
                    SendCommand(command);
                    docommand = false;
                }

                //Action timer 1 is briefly set to 1 when the shooting phase is over and the projectile is fired.
                if (_fface.Player.ActionTimer1 > 0)
                    return true;

                //Got out claimed!
                if (target.IsClaimed && !target.PartyClaim)
                    return false;

                //Died during the aim
                if (target.IsDead)
                    return false;

                //Inspect chat to determine if there are any errors
                var entries = _fface.Chat.ChatEntries
                    .ToList()
                    .Where(x => x.Timestamp > startCommand).ToList();
                foreach (var entry in entries)
                {
                    //Retry up to 3 times, if a cannot see message is received.
                    if (rChatCannotSee.IsMatch(entry.Text))
                    {
                        docommand = true;
                        if (++attempts > 3)
                            return false;
                        TimeWaiter.Pause(300);
                        continue;
                    }
                    //Fail out immediately if the target is too far away.
                    if (rChatTooFar.IsMatch(entry.Text))
                        return false;
                    //Fail out immediately if the player moves and the aim is interrupted.
                    if (rChatRangeInterrupted.IsMatch(entry.Text))
                        return false;
                    //Fail out immediately if the player doesnt have a ranged weapon equipped.
                    if (rChatRangeNoWeapon.IsMatch(entry.Text))
                        return false;
                    //Fail out immediately if range attacks are still on global cooldown.
                    if (rChatTooSoon.IsMatch(entry.Text))
                        return false;
                    //Fail out immediately if a command error is present. This indicates no target.
                    if (rChatCommandError.IsMatch(entry.Text))
                        return false; 
                }

                //Match the chat api polling rate.
                TimeWaiter.Pause(100);
            }
            return false;
        }
    }
}