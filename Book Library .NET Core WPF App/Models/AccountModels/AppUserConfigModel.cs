﻿namespace Book_Library_.NET_Core_WPF_App.Models.AccountModels
{
    internal class AppUserConfigModel
    {
        internal AppUserConfigModel(string login, string password, int accountId)
        {
            Login = login;
            Password = password;
            AccountId = accountId;
        }

        internal string Login { get; set; }

        internal string Password { get; set; }

        internal int AccountId { get; set; }
    }
}
