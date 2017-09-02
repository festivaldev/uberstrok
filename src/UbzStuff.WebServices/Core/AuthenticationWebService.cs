﻿using log4net;
using System;
using System.Collections.Generic;
using UbzStuff.Core.Common;
using UbzStuff.Core.Views;

namespace UbzStuff.WebServices.Core
{
    public class AuthenticationWebService : BaseAuthenticationWebService
    {
        private readonly static ILog Log = LogManager.GetLogger(typeof(AuthenticationWebService));

        public AuthenticationWebService(WebServiceContext ctx) : base(ctx)
        {
            // Space
        }

        public override AccountCompletionResultView OnCompleteAccount(int cmid, string name, ChannelType channelType, string locale, string machineId)
        {
            var member = Users.GetMember(cmid);
            if (member == null)
            {
                Log.Error("An unidentified CMID was passed.");
                return null;
            }

            var itemsAttributed = new Dictionary<int, int>();
            for (int i = 0; i < member.MemberItems.Count; i++) // The client seem to ignore the value of the dictionary.
                itemsAttributed.Add(member.MemberItems[i], 0);

            /*
             * result:
             * 1 -> Success
             * 2 -> GetNonDuplicateNames?
             * 3 -> Load menu for show?
             * 4 -> Invalid characters in name
             * 5 -> Account banned
             */
            var view = new AccountCompletionResultView(
                1,
                itemsAttributed,
                null
            );
            return view;
        }

        public override MemberAuthenticationResultView OnLoginSteam(string steamId, string authToken, string machineId)
        {
            Log.Info($"Logging in member {steamId}:{machineId}");

            // Figure out if the account has been linked.
            var linked = true;

            // Load the user from the database using its steamId.
            var newMember = false;
            var member = Users.Db.LoadMember(steamId);
            if (member == null)
            {
                Log.Info($"Member entry {steamId} does not exists, creating new entry");

                // Create a new member if its not in the db.
                newMember = true;
                member = Users.NewMember();

                // Link the Steam ID to the CMID.
                linked = Users.Db.Link(steamId, member);
            }

            var result = MemberAuthenticationResult.Ok;
            if (!linked)
                result = MemberAuthenticationResult.InvalidEsns;

            var newAuthToken = Users.LogInUser(member);
            var view = new MemberAuthenticationResultView
            {
                MemberAuthenticationResult = result,
                AuthToken = newAuthToken,
                IsAccountComplete = !newMember,
                ServerTime = DateTime.Now,

                MemberView = member,
                PlayerStatisticsView = new PlayerStatisticsView
                {
                    Cmid = member.PublicProfile.Cmid,
                    PersonalRecord = new PlayerPersonalRecordStatisticsView(),
                    WeaponStatistics = new PlayerWeaponStatisticsView()
                },
            };
            return view;
        }
    }
}
