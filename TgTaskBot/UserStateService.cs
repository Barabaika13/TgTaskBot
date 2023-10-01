using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgTaskBot
{
    public enum UserState
    {
        CreatingTask, TaskList, CompletingTask, DeletingTask, NoState
    }

    public class UserStateService
    {
        private readonly Dictionary<long, UserState> _userState = new();

        public void SetState(long userId, UserState state)
        {
            _userState[userId] = state;
        }

        public UserState GetState(long userId)
        {
            if (_userState.TryGetValue(userId, out var state))
            {
                return state;
            }
            else
            {
                return UserState.NoState;
            }
        }
    }
}
