using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace WiccanRedeServer
{
    interface IDisplay
    {
        void WriteLine(String message);
        void LogLine(String message);
    }

    interface ICommand
    {
        void ExecuteCommand(String command);
    }

    interface INetInput
    {
        int ConnectedUsersCount();
        ServerCore.Data GetInputData();
        bool AreInputData();
    }

    interface INetOutput
    {
        void AddOutputData(ServerCore.Data outputData);
    }

    interface IStartable
    {
        void Start();
        void Stop();
        bool IsStoped();
    }

    interface IAccountsActionExecutor
    {
        bool ExecuteAction(ref ServerCore.Data inputdata);
        String[] GetLoggedUsers();
        String[] GetLoggedUsers(String without);
        String[] GetLoggedUsers(String without, out int index);
        IWorldActionExecutor GetWorld(ServerCore.Data data);
    }

    interface IWorldActionExecutor
    {
        bool ExecuteAction(ref ServerCore.Data inputdata);
        World.Vector3 GetPosition();
        int GetID();
    }
}
